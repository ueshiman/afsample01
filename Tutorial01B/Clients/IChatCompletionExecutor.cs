using OpenAI.Chat;
using SampleOpenAIApp.Clients;

namespace Tutorial01B.Clients;

public interface IChatCompletionExecutor
{
    Task<ChatResult> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}