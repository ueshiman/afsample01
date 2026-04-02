using Cline01.Models;
using Cline01.Services.Interfaces;

namespace Cline01.Services;

public class AgentDefinitionValidator : IAgentDefinitionValidator
{
    private const int MaxEnabledAgents = 10;
    private readonly ILogger<AgentDefinitionValidator> _logger;

    public AgentDefinitionValidator(ILogger<AgentDefinitionValidator> logger)
    {
        _logger = logger;
    }

    public (bool IsValid, string? ErrorMessage) Validate(AgentDefinitionsRoot root)
    {
        _logger.LogInformation("Validating agent definitions");

        if (root.Agents == null || root.Agents.Count == 0)
        {
            return (false, "No agents defined");
        }

        // Check for duplicate AgentIds
        var agentIds = root.Agents.Select(a => a.AgentId).ToList();
        var duplicates = agentIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        
        if (duplicates.Any())
        {
            return (false, $"Duplicate agent IDs found: {string.Join(", ", duplicates)}");
        }

        // Check enabled agents count
        var enabledAgents = root.Agents.Where(a => a.Enabled).ToList();
        if (enabledAgents.Count > MaxEnabledAgents)
        {
            return (false, $"Too many enabled agents: {enabledAgents.Count}. Maximum is {MaxEnabledAgents}");
        }

        // Validate required fields for each agent
        foreach (var agent in root.Agents)
        {
            if (string.IsNullOrWhiteSpace(agent.AgentId))
            {
                return (false, "Agent ID is required");
            }

            if (string.IsNullOrWhiteSpace(agent.AgentName))
            {
                return (false, $"Agent name is required for agent {agent.AgentId}");
            }

            if (string.IsNullOrWhiteSpace(agent.SystemPrompt))
            {
                return (false, $"System prompt is required for agent {agent.AgentId}");
            }

            if (agent.Model == null || string.IsNullOrWhiteSpace(agent.Model.Deployment))
            {
                return (false, $"Model deployment is required for agent {agent.AgentId}");
            }
        }

        _logger.LogInformation("Agent definitions validation passed. {EnabledCount} enabled agents", enabledAgents.Count);
        return (true, null);
    }
}
