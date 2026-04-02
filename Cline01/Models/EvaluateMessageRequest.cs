namespace Cline01.Models;

public class EvaluateMessageRequest
{
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required string SentenceText { get; set; }
    public required string SpeakerRole { get; set; }
    public required DateTime Timestamp { get; set; }
}
