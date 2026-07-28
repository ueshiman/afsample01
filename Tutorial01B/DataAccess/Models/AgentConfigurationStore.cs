using Tutorial01B.DataAccess.Service;

namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationStore : IAgentConfigurationStore
{
    private readonly IAgentConfigurationLoader _agentConfigurationLoader;
    private readonly IAgentConfigurationFile _agentConfigurationFile;
    private AgentConfigurationSnapshot? _current;

    public AgentConfigurationStore(IAgentConfigurationLoader agentConfigurationLoader, IAgentConfigurationFile agentConfigurationFile)
    {
        _agentConfigurationLoader = agentConfigurationLoader;
        _agentConfigurationFile = agentConfigurationFile;
    }

    public AgentConfigurationSnapshot Current => _current ?? _agentConfigurationLoader.Load(_agentConfigurationFile.Path);

    public void Set(AgentConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        //Interlocked.Exchange(ref _current, snapshot);
        _current = snapshot;
    }
}