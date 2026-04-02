namespace Cline01.Models;

public class AgentDefinition
{
    public required string AgentId { get; set; }
    public required string AgentName { get; set; }
    public bool Enabled { get; set; }
    public required string SystemPrompt { get; set; }
    public required ModelConfiguration Model { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
    public string OutputFormat { get; set; } = "json";
}

public class ModelConfiguration
{
    public required string Deployment { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 1000;
}
