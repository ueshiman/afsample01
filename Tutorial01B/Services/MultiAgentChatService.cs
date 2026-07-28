using Microsoft.Extensions.Logging;

namespace Tutorial01B.Services;

public sealed class MultiAgentChatService : IChatService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly ILogger<MultiAgentChatService> _logger;

    public MultiAgentChatService(
        AgentOrchestrator orchestrator,
        ILogger<MultiAgentChatService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task RunSampleAsync(CancellationToken cancellationToken = default)
    {
        const string userMessage = "What's the best way to train a parrot?";

        _logger.LogInformation("Running MultiAgentChatService with message: {Message}", userMessage);

        var results = await _orchestrator.HandleAsync(new Uri("https://sample.com"), userMessage, Guid.NewGuid(), cancellationToken);

            Console.WriteLine($"=== {results} ===");
            Console.WriteLine(userMessage);
            Console.WriteLine();
    }
}
