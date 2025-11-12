using System.Threading;
using System.Threading.Tasks;

namespace Backend.LLMService
{
    // 简单回声 provider，作为缺省或占位实现
    public class EchoProvider : ILLMProvider
    {
        public Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }
}