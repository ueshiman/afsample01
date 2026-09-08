using Microsoft.Extensions.AI;
using Tutorial02.Clients;

namespace Tutorial02.Agents;

public sealed class TranslationAgent : IAgent
{
    private readonly IChatCompletionExecutor _executor;

    public TranslationAgent(IChatCompletionExecutor executor)
    {
        _executor = executor;
    }

    public string Name => "TranslationAgent";

    public async Task<string> ReplyAsync(string input, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> messages =
        [
            new ChatMessage(
                ChatRole.System,
                "You are a translation assistant. Translate the given Japanese text into English. " +
                "If the input is already in English, translate it into Japanese instead."),
            new ChatMessage(ChatRole.User, input)
        ];

        var result = await _executor.CompleteAsync(messages, cancellationToken);
        return result.Text;
    }
}
