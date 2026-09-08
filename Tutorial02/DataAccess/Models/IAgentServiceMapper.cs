using ConversationSuggestionService.Configuration;
using Tutorial02.Models;

namespace Tutorial02.DataAccess.Models;

public interface IAgentServiceMapper
{
    AgentServiceModel MppFrom(AgentServiceDefinition agentServiceDefinition);
}