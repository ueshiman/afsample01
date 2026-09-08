using Tutorial02.Models;

namespace ConversationSuggestionService.Configuration;

public interface IAgentConfigurationSnapshotFactory
{
    AgentConfigurationSnapshot Create(AgentServiceDefinition definition);
}