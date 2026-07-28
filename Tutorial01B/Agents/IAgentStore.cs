using Tutorial01B.Models;

namespace Tutorial01B.Agents;

public interface IAgentStore
{
    Guid CreateAgent();

    AgentEntity GetAgent(Guid? agentId);
    TimeSpan AgentEntityExpiration { get; }
    int Garbage(DateTimeOffset cutoff);
    int Garbage();
    bool RemoveAgent(Guid agentId);
}