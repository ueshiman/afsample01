using OpenAI.Chat;
using Tutorial01B.Clients;

namespace Tutorial01B.Clients;

public sealed class OpenAIChatCompletionExecutor : IChatCompletionExecutor
{
    private readonly ChatClient _chatClient;

    public OpenAIChatCompletionExecutor(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<ChatCompletion> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        return await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);
    }
}