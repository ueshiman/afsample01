using Cline01.Models;
using Cline01.Services.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Cline01.Services;

public class AgentConversationCoordinator : IAgentConversationCoordinator
{
    private readonly ILogger<AgentConversationCoordinator> _logger;
    private readonly OpenAISettings _openAISettings;

    public AgentConversationCoordinator(
        ILogger<AgentConversationCoordinator> logger,
        IOptions<OpenAISettings> openAISettings)
    {
        _logger = logger;
        _openAISettings = openAISettings.Value;
    }

    public async Task<ConversationResult?> CoordinateAsync(
        List<AgentExecutionResult> agentResults, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if agent conversation coordination is needed");

        // Simple heuristic: if multiple agents have different assessments, coordinate
        var completedResults = agentResults.Where(r => r.Status == "Completed" && r.Result != null).ToList();
        
        if (completedResults.Count < 2)
        {
            _logger.LogInformation("Not enough completed results for coordination");
            return null;
        }

        // Check if results have different status/assessment values
        var needsCoordination = HasDifferentAssessments(completedResults);

        if (!needsCoordination)
        {
            _logger.LogInformation("Agent results are consistent, no coordination needed");
            return null;
        }

        _logger.LogInformation("Different assessments detected, coordinating agent conversation");

        try
        {
            var builder = Kernel.CreateBuilder();
            
            if (!string.IsNullOrWhiteSpace(_openAISettings.AzureEndpoint))
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: "gpt-4o-mini",
                    endpoint: _openAISettings.AzureEndpoint,
                    apiKey: _openAISettings.ApiKey);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: "gpt-4o-mini",
                    apiKey: _openAISettings.ApiKey);
            }

            var kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(@"You are a coordinator AI that synthesizes results from multiple evaluation agents. 
Analyze their assessments and provide a unified summary with a final assessment. Return JSON format with 'summary' and 'details' fields.");

            var resultsJson = JsonSerializer.Serialize(completedResults, new JsonSerializerOptions { WriteIndented = true });
            chatHistory.AddUserMessage($"Coordinate these agent results:\n{resultsJson}");

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                new OpenAIPromptExecutionSettings 
                { 
                    Temperature = 0.3,
                    MaxTokens = 500
                },
                kernel,
                cancellationToken);

            var responseText = response.Content ?? "{}";
            var coordinationResult = JsonSerializer.Deserialize<Dictionary<string, object>>(responseText);

            return new ConversationResult
            {
                Summary = coordinationResult?.GetValueOrDefault("summary")?.ToString() 
                    ?? "エージェント間の会話結果として調整が必要と判断されました。",
                Details = coordinationResult?.GetValueOrDefault("details")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent conversation coordination");
            return null;
        }
    }

    private bool HasDifferentAssessments(List<AgentExecutionResult> results)
    {
        var assessments = new HashSet<string>();

        foreach (var result in results)
        {
            try
            {
                var json = JsonSerializer.Serialize(result.Result);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (dict != null)
                {
                    if (dict.TryGetValue("status", out var status))
                    {
                        assessments.Add(status.ToString());
                    }
                    else if (dict.TryGetValue("assessment", out var assessment))
                    {
                        assessments.Add(assessment.ToString());
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return assessments.Count > 1;
    }
}
