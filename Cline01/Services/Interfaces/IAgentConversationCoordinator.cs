using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IAgentConversationCoordinator
{
    Task<ConversationResult?> CoordinateAsync(List<AgentExecutionResult> agentResults, CancellationToken cancellationToken = default);
}
