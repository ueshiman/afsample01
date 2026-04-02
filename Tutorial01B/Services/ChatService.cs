using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Tutorial01B.Clients;

namespace Tutorial01B.Services;

public sealed class ChatService : IChatService
{
    private readonly IChatCompletionExecutor _executor;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatCompletionExecutor executor,
        ILogger<ChatService> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task RunSampleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessage> messages =
        [
            new SystemChatMessage("You are a helpful assistant that talks like a pirate in Japanese."),
            new UserChatMessage("Hi, can you help me?"),
            new AssistantChatMessage("Arrr! もちろんでござる…じゃなくて海賊風に手伝うぜ！"),
            new UserChatMessage("What's the best way to train a parrot?")
        ];

        ChatCompletion completion = await _executor.CompleteAsync(messages, cancellationToken);

        _logger.LogInformation("Model={Model}", completion.Model);
        _logger.LogInformation("Chat Role={Role}", completion.Role);

        foreach (ChatMessageContentPart contentPart in completion.Content)
        {
            if (!string.IsNullOrWhiteSpace(contentPart.Text))
            {
                Console.WriteLine("Message:");
                Console.WriteLine(contentPart.Text);
            }
        }

        _logger.LogInformation("Finish Reason={FinishReason}", completion.FinishReason);
    }
}