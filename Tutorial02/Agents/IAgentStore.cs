using Tutorial02.Models;

namespace Tutorial02.Agents;

public interface IAgentStore
{
    Guid CreateAgent();

    AgentEntity GetAgent(Guid? agentId);
    TimeSpan AgentEntityExpiration { get; }
    int Garbage(DateTimeOffset cutoff);
    int Garbage();
    bool RemoveAgent(Guid agentId);
}