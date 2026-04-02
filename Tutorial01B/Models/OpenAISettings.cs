namespace Tutorial01B.Models;

public sealed class OpenAISettings
{
    public const string SectionName = "OpenAI";

    public string DeploymentName { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}