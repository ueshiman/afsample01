using ConversationSuggestionService.Configuration;

namespace Tutorial01B.Clients
{
    public class ClientStore
    {
        private readonly IAgentConfigurationStore _agentConfigurationStore;

        public ClientStore(IAgentConfigurationStore agentConfigurationStore)
        {
            _agentConfigurationStore = agentConfigurationStore;
        }

        public ClientValue GetClients(Guid clientId)
        {
            // Implementation for retrieving clients by their ID
            if (_clients.TryGetValue(clientId, out var client))
            {
                return client;
            }



            ClientValue newClientValue = new();

            _clients.Add(clientId, newClientValue);

            return newClientValue;
        }

        private readonly Dictionary<Guid, ClientValue> _clients = [];

    }
}
