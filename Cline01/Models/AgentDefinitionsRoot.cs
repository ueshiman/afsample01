namespace Cline01.Models;

public class AgentDefinitionsRoot
{
    public required string Version { get; set; }
    public required List<AgentDefinition> Agents { get; set; }
}
