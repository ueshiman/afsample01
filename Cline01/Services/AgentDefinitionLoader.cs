using Cline01.Models;
using Cline01.Services.Interfaces;
using System.Text.Json;

namespace Cline01.Services;

public class AgentDefinitionLoader : IAgentDefinitionLoader
{
    private readonly ILogger<AgentDefinitionLoader> _logger;

    public AgentDefinitionLoader(ILogger<AgentDefinitionLoader> logger)
    {
        _logger = logger;
    }

    public async Task<AgentDefinitionsRoot> LoadAsync(string filePath)
    {
        _logger.LogInformation("Loading agent definitions from {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Agent definition file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Agent definition file not found: {filePath}");
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<AgentDefinitionsRoot>(json, options);
            
            if (root == null)
            {
                throw new InvalidOperationException("Failed to deserialize agent definitions");
            }

            _logger.LogInformation("Successfully loaded {Count} agent definitions", root.Agents.Count);
            return root;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in agent definition file");
            throw new InvalidOperationException("Invalid JSON format in agent definition file", ex);
        }
    }
}
