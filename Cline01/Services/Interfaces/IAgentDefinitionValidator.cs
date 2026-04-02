using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IAgentDefinitionValidator
{
    (bool IsValid, string? ErrorMessage) Validate(AgentDefinitionsRoot root);
}
