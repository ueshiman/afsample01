using Cline01.Models;
using Cline01.Services.Interfaces;

namespace Cline01.Services;

public class EvaluationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEvaluationJobQueue _jobQueue;
    private readonly ILogger<EvaluationBackgroundService> _logger;

    public EvaluationBackgroundService(
        IServiceProvider serviceProvider,
        IEvaluationJobQueue jobQueue,
        ILogger<EvaluationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Evaluation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueAsync(stoppingToken);
                if (job == null)
                {
                    continue;
                }

                _logger.LogInformation("Processing job {JobId}", job.JobId);

                // Process in a separate task to avoid blocking the queue
                _ = Task.Run(async () => await ProcessJobAsync(job, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background service loop");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("Evaluation Background Service stopped");
    }

    private async Task ProcessJobAsync(EvaluationJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var agentLoader = scope.ServiceProvider.GetRequiredService<IAgentDefinitionLoader>();
        var agentValidator = scope.ServiceProvider.GetRequiredService<IAgentDefinitionValidator>();
        var agentExecutor = scope.ServiceProvider.GetRequiredService<IAgentExecutor>();
        var coordinator = scope.ServiceProvider.GetRequiredService<IAgentConversationCoordinator>();
        var webhookNotifier = scope.ServiceProvider.GetRequiredService<IWebhookNotifier>();

        try
        {
            _logger.LogInformation("Loading agent definitions for job {JobId}", job.JobId);
            
            // Load agent definitions
            var agentDefinitions = await agentLoader.LoadAsync("agents.json");
            
            // Validate
            var (isValid, errorMessage) = agentValidator.Validate(agentDefinitions);
            if (!isValid)
            {
                _logger.LogError("Agent definition validation failed: {Error}", errorMessage);
                await SendErrorWebhook(job, errorMessage!, webhookNotifier, cancellationToken);
                return;
            }

            // Get enabled agents
            var enabledAgents = agentDefinitions.Agents.Where(a => a.Enabled).ToList();
            _logger.LogInformation("Executing {Count} enabled agents for job {JobId}", enabledAgents.Count, job.JobId);

            // Execute all agents in parallel
            var executionTasks = enabledAgents.Select(agent => 
                agentExecutor.ExecuteAsync(agent, job.SentenceText, cancellationToken));
            
            var agentResults = (await Task.WhenAll(executionTasks)).ToList();

            _logger.LogInformation("All agents completed for job {JobId}. Completed: {Completed}, Failed: {Failed}, Timeout: {Timeout}",
                job.JobId,
                agentResults.Count(r => r.Status == "Completed"),
                agentResults.Count(r => r.Status == "Failed"),
                agentResults.Count(r => r.Status == "Timeout"));

            // Coordinate agent conversation if needed
            var conversationResult = await coordinator.CoordinateAsync(agentResults, cancellationToken);

            // Send webhook notification
            var payload = new EvaluationWebhookPayload
            {
                EventType = "evaluation.completed",
                JobId = job.JobId,
                ConversationId = job.ConversationId,
                MessageId = job.MessageId,
                AgentResults = agentResults,
                ConversationResult = conversationResult
            };

            await webhookNotifier.NotifyAsync(payload, cancellationToken);

            _logger.LogInformation("Job {JobId} processed successfully", job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", job.JobId);
            await SendErrorWebhook(job, ex.Message, webhookNotifier, cancellationToken);
        }
    }

    private async Task SendErrorWebhook(EvaluationJob job, string errorMessage, IWebhookNotifier webhookNotifier, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new EvaluationWebhookPayload
            {
                EventType = "evaluation.completed",
                JobId = job.JobId,
                ConversationId = job.ConversationId,
                MessageId = job.MessageId,
                AgentResults = new List<AgentExecutionResult>
                {
                    new AgentExecutionResult
                    {
                        AgentId = "system",
                        AgentName = "System",
                        Status = "Failed",
                        ErrorMessage = errorMessage
                    }
                }
            };

            await webhookNotifier.NotifyAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error webhook for job {JobId}", job.JobId);
        }
    }
}
