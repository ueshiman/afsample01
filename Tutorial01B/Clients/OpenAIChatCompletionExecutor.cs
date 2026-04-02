using OpenAI.Chat;
using Tutorial01B.Clients;

namespace SampleOpenAIApp.Clients;

public sealed class OpenAIChatCompletionExecutor : IChatCompletionExecutor
{
    private readonly ChatClient _chatClient;

    public OpenAIChatCompletionExecutor(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<ChatResult> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

        string text = string.Join(
            Environment.NewLine,
            completion.Content
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .Select(x => x.Text));

        return new ChatResult
        {
            Model = completion.Model,
            Role = completion.Role.ToString(),
            Text = text,
            FinishReason = completion.FinishReason.ToString()
        };
    }
}