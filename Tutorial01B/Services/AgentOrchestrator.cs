using Tutorial01B.Agents;

namespace Tutorial01B.Services;

public sealed class AgentOrchestrator
{
    private readonly IEnumerable<IAgent> _agents;

    public AgentOrchestrator(IEnumerable<IAgent> agents)
    {
        _agents = agents;
    }

    public async Task<IReadOnlyDictionary<string, string>> HandleAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();

        foreach (var agent in _agents)
        {
            results[agent.Name] = await agent.ReplyAsync(message, cancellationToken);
        }

        return results;
    }
}
