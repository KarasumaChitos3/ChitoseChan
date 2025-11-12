using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Net.Http;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Backend.LLMService
{
    public class SKOpenAIProvider : ILLMProvider
    {
        private readonly IChatCompletionService _chat;
        private readonly Kernel _kernel;
        private string modelId;
        private string apiKey;

        public SKOpenAIProvider(string modelId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("OPENAI_API_KEY is required", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("OPENAI_MODEL is required", nameof(modelId));

            this.modelId = modelId;
            this.apiKey = apiKey;

            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IChatCompletionService>(sp =>
                new OpenAIChatCompletionService(modelId, apiKey));
            _kernel = builder.Build();
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
        }


        public SKOpenAIProvider(string modelId, string apiKey, Uri endpoint)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("OPENAI_API_KEY is required", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("OPENAI_MODEL is required", nameof(modelId));
            if (endpoint == null)
                throw new ArgumentException("OPENAI_ENDPOINT is required", nameof(endpoint));
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IChatCompletionService>(sp =>
                new OpenAIChatCompletionService(modelId, apiKey, httpClient: new HttpClient { BaseAddress = endpoint }));
            _kernel = builder.Build();
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default)
        {
            var history = new ChatHistory();
            history.AddUserMessage(input);

            var messages = await _chat.GetChatMessageContentsAsync(
                history,
                executionSettings: null,
                kernel: _kernel,
                cancellationToken: cancellationToken
            );

            var content = messages.FirstOrDefault()?.Content;
            return content?.ToString() ?? string.Empty;
        }

        // 通过 stdio 注册 MCP 工具为 SK 插件
        public static async Task<KernelPlugin> AddMCPTool(
            Kernel kernel,
            string pluginName,
            string command,
            params string[] arguments)
        {
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = pluginName,
                Command = command,
                Arguments = arguments ?? Array.Empty<string>(),
            });
            return await CreateAndRegisterMcpPluginAsync(kernel, pluginName, transport);
        }

        // 通过 StreamClientTransport（远程输入/输出流）注册 MCP 工具
        public static async Task<KernelPlugin> AddMCPTool(
            Kernel kernel,
            string pluginName,
            System.IO.Stream serverInput,
            System.IO.Stream serverOutput,
            ILoggerFactory? loggerFactory = null)
        {
            var transport = new StreamClientTransport(serverInput, serverOutput, loggerFactory);
            return await CreateAndRegisterMcpPluginAsync(kernel, pluginName, transport);
        }

        private static async Task<KernelPlugin> CreateAndRegisterMcpPluginAsync(
            Kernel kernel,
            string pluginName,
            IClientTransport transport)
        {
            var mcpClient = await McpClient.CreateAsync(transport);
            var tools = await mcpClient.ListToolsAsync();

            var functions = tools.Select(t =>
                KernelFunctionFactory.CreateFromMethod(
                    (string argsJson) => CallToolAsync(mcpClient, t.Name, argsJson),
                    functionName: t.Name,
                    description: t.Description ?? "MCP tool"
                ));

            var plugin = KernelPluginFactory.CreateFromFunctions(pluginName, functions);
            kernel.Plugins.Add(plugin);
            return plugin;
        }

        private static async Task<string> CallToolAsync(McpClient client, string toolName, string argsJson)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson)
                       ?? new Dictionary<string, object?>();
            var result = await client.CallToolAsync(toolName, dict);
            return JsonSerializer.Serialize(result);
        }
    }
}