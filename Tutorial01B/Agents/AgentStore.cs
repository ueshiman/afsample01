using System.Linq.Expressions;
using Tutorial01B.Models;

namespace Tutorial01B.Agents
{
    public class AgentStore : IAgentStore
    {
        private static readonly Dictionary<Guid, AgentEntity> Agents = new();
        private readonly IAgentEntityFactory _agentEntityFactory;
        private readonly ILogger<AgentStore> _logger;
        private readonly IConfiguration _configuration;

        public AgentStore(ILogger<AgentStore> logger, IAgentEntityFactory agentEntityFactory, IConfiguration configuration)
        {
            _logger = logger;
            _agentEntityFactory = agentEntityFactory;
            _configuration = configuration;
        }

        public Guid CreateAgent()
        {
            var newAgent = _agentEntityFactory.CreateAgent();
            Agents.Add(newAgent.Id, newAgent);
            return newAgent.Id;
        }

        public AgentEntity GetAgent(Guid? agentId)
        {
            // Implementation for retrieving an agent by its ID
            try
            {
                if (agentId.HasValue && Agents.TryGetValue(agentId.Value, out var agent))
                {
                    agent.ServiceModel.Touch();
                    Garbage();
                    return agent;
                }

                // If the agent does not exist, create a new one and add it to the store
                AgentEntity newAgent = _agentEntityFactory.CreateAgent();

                Agents.Add(newAgent.Id, newAgent);

                return newAgent;
            }
            catch
            {
                _logger.LogError("An error occurred while retrieving the agent.");
                throw;
            }
        }

        public TimeSpan AgentEntityExpiration
        {
            get
            {
                var expirationSetting = _configuration["TimeOut:AgentEntityLifeMinute"];
                if (int.TryParse(expirationSetting, out var expirationMinutes))
                {
                    return TimeSpan.FromMinutes(expirationMinutes);
                }

                _logger.LogWarning("Invalid AgentEntityExpiration configuration. Using default of 1 hour.");
                return TimeSpan.FromHours(1);
            }
        }

        public int Garbage(DateTimeOffset cutoff)
        {
            var keysToRemove = Agents.Where(kvp => kvp.Value.LastActiveAt < cutoff)
                                      .Select(kvp => kvp.Key)
                                      .ToList();

            foreach (var key in keysToRemove)
            {
                RemoveAgent(key);
            }

            return keysToRemove.Count;
        }

        public int Garbage()
        {
            return Garbage(DateTimeOffset.UtcNow - AgentEntityExpiration);
        }


        public bool RemoveAgent(Guid agentId)
        {
            return Agents.Remove(agentId);
        } 
    }
}
