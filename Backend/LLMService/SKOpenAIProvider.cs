using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

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
            this.modelId = modelId;
            this.apiKey = apiKey;
        }


        public SKOpenAIProvider(string modelId, string apiKey, Uri endpoint)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("OPENAI_API_KEY is required", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("OPENAI_MODEL is required", nameof(modelId));

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
    }
}