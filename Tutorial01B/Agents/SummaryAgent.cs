using OpenAI.Chat;
using Tutorial01B.Clients;

namespace Tutorial01B.Agents;

public sealed class SummaryAgent : IAgent
{
    private readonly IChatCompletionExecutor _executor;

    public SummaryAgent(IChatCompletionExecutor executor)
    {
        _executor = executor;
    }

    public string Name => "SummaryAgent";

    public async Task<string> ReplyAsync(string input, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> messages =
        [
            new SystemChatMessage("You are a helpful assistant that summarizes text concisely in Japanese."),
            new UserChatMessage(input)
        ];

        var result = await _executor.CompleteAsync(messages, cancellationToken);
        return result.Text;
    }
}
