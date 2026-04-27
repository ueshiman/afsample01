namespace ConversationSuggestionService.Configuration;

public static class AgentConfigurationSnapshotFactory
{
    public static AgentConfigurationSnapshot Create(AgentServiceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var providers = definition.Providers
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var callbacks = definition.Callbacks
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var agents = definition.Agents
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var enabledAgentsOrdered = definition.Agents
            .Where(x => x.Enabled)
            .OrderByDescending(x => x.Priority)
            .ToList();

        return new AgentConfigurationSnapshot
        {
            Raw = definition,
            Providers = providers,
            Callbacks = callbacks,
            Agents = agents,
            EnabledAgentsOrdered = enabledAgentsOrdered
        };
    }
}