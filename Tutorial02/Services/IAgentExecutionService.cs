namespace ConversationSuggestionService.Services;

public interface IAgentExecutionService
{
    Task<AgentExecutionResult> ExecuteAsync(
        string conversation,
        CancellationToken cancellationToken = default);
}