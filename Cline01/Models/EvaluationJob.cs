namespace Cline01.Models;

public class EvaluationJob
{
    public required string JobId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required string SentenceText { get; set; }
    public required string SpeakerRole { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime EnqueuedAt { get; set; }
}
