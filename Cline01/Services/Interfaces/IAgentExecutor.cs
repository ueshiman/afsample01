using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IAgentExecutor
{
    Task<AgentExecutionResult> ExecuteAsync(AgentDefinition agent, string sentenceText, CancellationToken cancellationToken = default);
}
