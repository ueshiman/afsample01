using OpenAI.Chat;

namespace Tutorial01B.Models
{
    public class AgentEntity
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required AgentServiceModel ServiceModel { get; set; }
        public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public DateTimeOffset LastActiveAt { get; set; }
        public DateTimeOffset Touch() { LastActiveAt = DateTimeOffset.UtcNow; return LastActiveAt; }    
    }
}
