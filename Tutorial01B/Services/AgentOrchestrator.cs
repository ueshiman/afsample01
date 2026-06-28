using System.ClientModel;
using System.Collections.Concurrent;
using ConversationSuggestionService.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Tutorial01B.Models;

namespace Tutorial01B.Services;

public sealed class AgentOrchestrator
{
    private readonly AgentConfigurationLoader _configurationLoader;
    private readonly OpenAISettings _openAiSettings;
    private readonly string _settingsFilePath;

    public AgentOrchestrator(
        AgentConfigurationLoader configurationLoader,
        IOptions<OpenAISettings> openAiSettings,
        IWebHostEnvironment environment)
    {
        _configurationLoader = configurationLoader;
        _openAiSettings = openAiSettings.Value;
        _settingsFilePath = Path.Combine(environment.ContentRootPath, "agentsettings.json");
    }

    public Task<IReadOnlyDictionary<string, string>> ExecuteAsync(
        string input,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(input, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> HandleAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _configurationLoader.LoadAsync(_settingsFilePath, cancellationToken);
        var mode = snapshot.Raw.Execution.Mode;

        return string.Equals(mode, "parallel", StringComparison.OrdinalIgnoreCase)
            ? await ExecuteParallelAsync(snapshot, message, cancellationToken)
            : await ExecuteSequentialAsync(snapshot, message, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> ExecuteSequentialAsync(
        AgentConfigurationSnapshot snapshot,
        string message,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var agent in snapshot.EnabledAgentsOrdered)
        {
            results[agent.Name] = await ExecuteAgentAsync(snapshot, agent, message, cancellationToken);
        }

        return results;
    }

    private async Task<IReadOnlyDictionary<string, string>> ExecuteParallelAsync(
        AgentConfigurationSnapshot snapshot,
        string message,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var semaphore = new SemaphoreSlim(snapshot.Raw.Execution.MaxDegreeOfParallelism);

        var tasks = snapshot.EnabledAgentsOrdered.Select(async agent =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                results[agent.Name] = await ExecuteAgentAsync(snapshot, agent, message, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return new Dictionary<string, string>(results, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> ExecuteAgentAsync(
        AgentConfigurationSnapshot snapshot,
        AgentDefinition agent,
        string message,
        CancellationToken cancellationToken)
    {
        var provider = snapshot.Providers[agent.ProviderRef];
        var endpoint = ResolveEndpoint(provider.Endpoint);

        var chatClient = new ChatClient(
            credential: new ApiKeyCredential(_openAiSettings.ApiKey),
            model: agent.Deployment,
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });

        IReadOnlyList<ChatMessage> messages =
        [
            new SystemChatMessage(agent.Prompt.System),
            new UserChatMessage(message)
        ];

        var timeoutSeconds = agent.TimeoutSeconds ?? snapshot.Raw.Service.DefaultTimeoutSeconds;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, cancellationToken: linkedCts.Token);

        return string.Join(
            Environment.NewLine,
            completion.Content
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .Select(x => x.Text));
    }

    private string ResolveEndpoint(string configuredEndpoint)
    {
        return string.IsNullOrWhiteSpace(configuredEndpoint)
            || configuredEndpoint.Contains("your-resource", StringComparison.OrdinalIgnoreCase)
            ? _openAiSettings.Endpoint
            : configuredEndpoint;
    }
}
