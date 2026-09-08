using Microsoft.Extensions.AI;
using Tutorial02.Models;

namespace Tutorial02.Services;

public interface IAgentGroupChatRunner
{
    Task<AgentGroupChatResult> RunAsync(AgentGroupModel group, List<ChatMessage> message, int maxRounds = 1, int defaultTimeoutSeconds = 30, CancellationToken cancellationToken = default);
}