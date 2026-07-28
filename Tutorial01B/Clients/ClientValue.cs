using OpenAI.Chat;

namespace Tutorial01B.Clients
{
    public class ClientValue
    {
        private readonly Dictionary<string, ChatClient> Clients = new Dictionary<string, ChatClient>();

        public ChatClient ChatClientValue { get; set; }

        public ClientValue()
        {
        }
    }
}
