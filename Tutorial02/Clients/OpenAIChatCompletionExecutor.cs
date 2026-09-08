using Microsoft.Extensions.AI;
using Tutorial02.Clients;

namespace SampleOpenAIApp.Clients;

public sealed class OpenAIChatCompletionExecutor : IChatCompletionExecutor
{
    private readonly IChatClient _chatClient;

    public OpenAIChatCompletionExecutor(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<ChatResult> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ChatResponse completion = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return new ChatResult
        {
            Model = completion.ModelId ?? string.Empty,
            Role = completion.Messages.LastOrDefault()?.Role.ToString() ?? string.Empty,
            Text = completion.Text ?? string.Empty,
            FinishReason = completion.FinishReason?.ToString() ?? string.Empty
        };
    }
}