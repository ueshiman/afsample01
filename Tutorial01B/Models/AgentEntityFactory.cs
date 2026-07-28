using ConversationSuggestionService.Configuration;
using Tutorial01B.DataAccess.Models;

namespace Tutorial01B.Models
{
    public class AgentEntityFactory : IAgentEntityFactory
    {
        private readonly IAgentServiceMapper _agentServiceMapper;
        private readonly IAgentConfigurationStore _configurationStore;

        public AgentEntityFactory(IAgentServiceMapper agentServiceMapper, IAgentConfigurationStore configurationStore)
        {
            _agentServiceMapper = agentServiceMapper;
            _configurationStore = configurationStore;
        }

        public AgentEntity CreateAgent()
        {
            return new AgentEntity
            {
                ServiceModel = _agentServiceMapper.MppFrom(_configurationStore.Current.Raw),
                LastActiveAt = DateTimeOffset.UtcNow
            };
        }
    }
}
