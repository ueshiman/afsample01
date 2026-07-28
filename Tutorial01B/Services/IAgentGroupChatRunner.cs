using OpenAI.Chat;
using Tutorial01B.Models;

namespace Tutorial01B.Services;

public interface IAgentGroupChatRunner
{
    Task<AgentGroupChatResult> RunAsync(AgentGroupModel group, List<ChatMessage> message, int maxRounds = 1, int defaultTimeoutSeconds = 30, CancellationToken cancellationToken = default);
}