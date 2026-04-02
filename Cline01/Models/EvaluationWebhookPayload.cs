namespace Cline01.Models;

public class EvaluationWebhookPayload
{
    public required string EventType { get; set; }
    public required string JobId { get; set; }
    public required string ConversationId { get; set; }
    public required string MessageId { get; set; }
    public required List<AgentExecutionResult> AgentResults { get; set; }
    public ConversationResult? ConversationResult { get; set; }
}
