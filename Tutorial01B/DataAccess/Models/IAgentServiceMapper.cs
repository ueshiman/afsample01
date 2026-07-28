using ConversationSuggestionService.Configuration;
using Tutorial01B.Models;

namespace Tutorial01B.DataAccess.Models;

public interface IAgentServiceMapper
{
    AgentServiceModel MppFrom(AgentServiceDefinition agentServiceDefinition);
}