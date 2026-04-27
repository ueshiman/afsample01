namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationSnapshot
{
    public required AgentServiceDefinition Raw { get; init; }

    public required IReadOnlyDictionary<string, ProviderDefinition> Providers { get; init; }

    public required IReadOnlyDictionary<string, CallbackDefinition> Callbacks { get; init; }

    public required IReadOnlyDictionary<string, AgentDefinition> Agents { get; init; }

    public required IReadOnlyList<AgentDefinition> EnabledAgentsOrdered { get; init; }
}