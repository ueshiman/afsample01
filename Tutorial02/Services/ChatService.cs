using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SampleOpenAIApp.Clients;
using Tutorial02.Clients;
using Tutorial02.Services;

namespace Tutorial02.Services;

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
            new ChatMessage(ChatRole.System, "You are a helpful assistant that talks like a pirate in Japanese."),
            new ChatMessage(ChatRole.User, "Hi, can you help me?"),
            new ChatMessage(ChatRole.Assistant, "Arrr! もちろんでござる…じゃなくて海賊風に手伝うぜ！"),
            new ChatMessage(ChatRole.User, "What's the best way to train a parrot?")
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