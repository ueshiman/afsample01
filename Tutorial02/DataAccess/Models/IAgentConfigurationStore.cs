namespace ConversationSuggestionService.Configuration;

public interface IAgentConfigurationStore
{
    AgentConfigurationSnapshot Current { get; }
    void Set(AgentConfigurationSnapshot snapshot);
}