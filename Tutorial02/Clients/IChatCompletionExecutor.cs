using Microsoft.Extensions.AI;
using SampleOpenAIApp.Clients;

namespace Tutorial02.Clients;

public interface IChatCompletionExecutor
{
    Task<ChatResult> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}