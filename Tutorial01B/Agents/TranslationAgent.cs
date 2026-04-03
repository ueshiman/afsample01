using OpenAI.Chat;
using Tutorial01B.Clients;

namespace Tutorial01B.Agents;

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
            new SystemChatMessage(
                "You are a translation assistant. Translate the given Japanese text into English. " +
                "If the input is already in English, translate it into Japanese instead."),
            new UserChatMessage(input)
        ];

        var result = await _executor.CompleteAsync(messages, cancellationToken);
        return result.Text;
    }
}
