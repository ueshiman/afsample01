using Microsoft.Extensions.AI;

namespace Tutorial02.Models
{
    public class AgentEntity
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required AgentServiceModel ServiceModel { get; set; }
        public List<ChatMessage> ChatMessages { get; set; } = [];
        public DateTimeOffset LastActiveAt { get; set; }
        public DateTimeOffset Touch() { LastActiveAt = DateTimeOffset.UtcNow; return LastActiveAt; }    
    }
}
