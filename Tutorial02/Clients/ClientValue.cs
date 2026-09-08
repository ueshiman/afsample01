using OpenAI.Chat;

namespace Tutorial02.Clients
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
