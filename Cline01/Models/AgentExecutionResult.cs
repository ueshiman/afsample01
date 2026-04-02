namespace Cline01.Models;

public class AgentExecutionResult
{
    public required string AgentId { get; set; }
    public required string AgentName { get; set; }
    public required string Status { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
