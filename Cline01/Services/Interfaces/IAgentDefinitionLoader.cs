using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IAgentDefinitionLoader
{
    Task<AgentDefinitionsRoot> LoadAsync(string filePath);
}
