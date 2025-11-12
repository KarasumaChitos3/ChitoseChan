using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.LLMService
{
    public class LLMService
    {
        private readonly ILLMProvider _provider;

        public LLMService(ILLMProvider provider)
        {
            _provider = provider;
        }

        public Task<string> AskAsync(string text, CancellationToken cancellationToken = default)
            => _provider.GenerateAsync(text, cancellationToken);

        public static LLMService CreateDefaultFromEnv()
        {
            // 目前支持 OpenAI，若环境变量不存在则回退到 Echo
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var modelId = Environment.GetEnvironmentVariable("OPENAI_MODEL")
                          ?? Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL")
                          ?? "";
            var endpointStr = Environment.GetEnvironmentVariable("OPENAI_API_ENDPOINT")
                               ?? Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")
                               ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
                               ?? "https://api.openai.com/v1";
            var endpoint = new Uri(endpointStr);

            if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelId) && endpoint != null)
            {
                var provider = new SKOpenAIProvider(modelId, apiKey, endpoint);
                return new LLMService(provider);
            }
            return new LLMService(new EchoProvider());
        }
    }
}