using ConversationSuggestionService.Configuration;

namespace ConversationSuggestionService.Services;

public sealed class AgentExecutionService : IAgentExecutionService
{
    private readonly IAgentConfigurationStore _configurationStore;

    public AgentExecutionService(IAgentConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore;
    }

    public Task<AgentExecutionResult> ExecuteAsync(string conversation, CancellationToken cancellationToken = default)
    {
        var current = _configurationStore.Current;

        var items = current.EnabledAgentsOrdered
            .Select(kvp => kvp.Value)
            .Select(groups => groups
            .Select(agent => new AgentExecutionItem
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                ProviderRef = agent.ProviderRef,
                Deployment = agent.Deployment
            }).ToList())
            .ToList();

        var result = new AgentExecutionResult
        {
            Conversation = conversation,
            Items = items
        };

        return Task.FromResult(result);
    }
}

public sealed class AgentExecutionResult
{
    public string Conversation { get; set; } = string.Empty;

    public List<List<AgentExecutionItem>> Items { get; set; } = new();
}

public sealed class AgentExecutionItem
{
    public string AgentId { get; set; } = string.Empty;

    public string AgentName { get; set; } = string.Empty;

    public string ProviderRef { get; set; } = string.Empty;

    public string Deployment { get; set; } = string.Empty;
}