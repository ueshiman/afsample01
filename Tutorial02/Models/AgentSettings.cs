namespace Tutorial02.Models;

public sealed class AgentSettings
{
    public const string SectionName = "Agents";

    public IList<string> Enabled { get; set; } = [];
}
