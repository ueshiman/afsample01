using Tutorial01B.Models;

namespace ConversationSuggestionService.Configuration;

public interface IAgentConfigurationSnapshotFactory
{
    AgentConfigurationSnapshot Create(AgentServiceDefinition definition);
}