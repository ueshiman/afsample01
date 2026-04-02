using Cline01.Models;
using Cline01.Services.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Options;
using Azure.AI.OpenAI;
using Azure;

namespace Cline01.Services;

public class AgentExecutor : IAgentExecutor
{
    private readonly ILogger<AgentExecutor> _logger;
    private readonly OpenAISettings _openAISettings;

    public AgentExecutor(
        ILogger<AgentExecutor> logger,
        IOptions<OpenAISettings> openAISettings)
    {
        _logger = logger;
        _openAISettings = openAISettings.Value;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentDefinition agent, 
        string sentenceText, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing agent {AgentId} ({AgentName})", agent.AgentId, agent.AgentName);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(agent.TimeoutSeconds));

            // Build Semantic Kernel
            var builder = Kernel.CreateBuilder();
            
            if (!string.IsNullOrWhiteSpace(_openAISettings.AzureEndpoint))
            {
                // Azure OpenAI
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: agent.Model.Deployment,
                    endpoint: _openAISettings.AzureEndpoint,
                    apiKey: _openAISettings.ApiKey);
            }
            else
            {
                // OpenAI
                builder.AddOpenAIChatCompletion(
                    modelId: agent.Model.Deployment,
                    apiKey: _openAISettings.ApiKey);
            }

            var kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(agent.SystemPrompt);
            chatHistory.AddUserMessage(sentenceText);

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = agent.Model.Temperature,
                MaxTokens = agent.Model.MaxTokens
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cts.Token);

            var resultText = response.Content ?? string.Empty;
            object? parsedResult = null;

            if (agent.OutputFormat.ToLowerInvariant() == "json" && !string.IsNullOrWhiteSpace(resultText))
            {
                try
                {
                    parsedResult = System.Text.Json.JsonSerializer.Deserialize<object>(resultText);
                }
                catch (Exception jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to parse agent {AgentId} response as JSON", agent.AgentId);
                    parsedResult = new { rawText = resultText };
                }
            }
            else
            {
                parsedResult = new { text = resultText };
            }

            _logger.LogInformation("Agent {AgentId} completed successfully", agent.AgentId);

            return new AgentExecutionResult
            {
                AgentId = agent.AgentId,
                AgentName = agent.AgentName,
                Status = "Completed",
                Result = parsedResult
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent {AgentId} execution timed out after {Timeout}s", 
                agent.AgentId, agent.TimeoutSeconds);
            
            return new AgentExecutionResult
            {
                AgentId = agent.AgentId,
                AgentName = agent.AgentName,
                Status = "Timeout",
                ErrorMessage = $"Execution timed out after {agent.TimeoutSeconds} seconds"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent {AgentId}", agent.AgentId);
            
            return new AgentExecutionResult
            {
                AgentId = agent.AgentId,
                AgentName = agent.AgentName,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string AzureEndpoint { get; set; } = string.Empty;
}
