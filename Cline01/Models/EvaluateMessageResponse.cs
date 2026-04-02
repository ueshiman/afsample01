namespace Cline01.Models;

public class EvaluateMessageResponse
{
    public bool Accepted { get; set; }
    public required string JobId { get; set; }
    public required string Status { get; set; }
}
