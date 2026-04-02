using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SampleOpenAIApp.Clients;
using Tutorial01B.Clients;
using Tutorial01B.Services;

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

        ChatResult result = await _executor.CompleteAsync(messages, cancellationToken);

        _logger.LogInformation("Model={Model}", result.Model);
        _logger.LogInformation("Chat Role={Role}", result.Role);

        if (!string.IsNullOrWhiteSpace(result.Text))
        {
            Console.WriteLine("Message:");
            Console.WriteLine(result.Text);
        }

        _logger.LogInformation("Finish Reason={FinishReason}", result.FinishReason);
    }
}