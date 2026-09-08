namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationSnapshot
{
    public required AgentServiceDefinition Raw { get; init; }

    public required IReadOnlyDictionary<string, ProviderDefinition> Providers { get; init; }

    public required IReadOnlyDictionary<string, CallbackDefinition> Callbacks { get; init; }

    public required IReadOnlyDictionary<string, AgentGroupDefinition> Agents { get; init; }

    public required IReadOnlyDictionary<string, List<AgentDefinition>> EnabledAgentsOrdered { get; init; }
}