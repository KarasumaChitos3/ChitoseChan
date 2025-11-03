using System.Threading;
using System.Threading.Tasks;

namespace Backend.LLMService
{
    public interface ILLMProvider
    {
        Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default);
    }
}