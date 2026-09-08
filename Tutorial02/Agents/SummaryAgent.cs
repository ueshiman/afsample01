using Microsoft.Extensions.AI;
using Tutorial02.Clients;

namespace Tutorial02.Agents;

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
            new ChatMessage(ChatRole.System, "You are a helpful assistant that summarizes text concisely in Japanese."),
            new ChatMessage(ChatRole.User, input)
        ];

        var result = await _executor.CompleteAsync(messages, cancellationToken);
        return result.Text;
    }
}
