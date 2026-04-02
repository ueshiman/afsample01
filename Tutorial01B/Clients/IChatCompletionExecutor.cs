using OpenAI.Chat;

namespace Tutorial01B.Clients;

public interface IChatCompletionExecutor
{
    Task<ChatCompletion> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}