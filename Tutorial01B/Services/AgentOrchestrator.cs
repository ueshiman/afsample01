using System.ClientModel;
using System.Collections.Concurrent;
using System.Text.Json;
using ConversationSuggestionService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using OpenAI;
using OpenAI.Chat;
using Tutorial01B.Agents;
using Tutorial01B.Models;

namespace Tutorial01B.Services;

public class AgentOrchestrator : IAgentOrchestrator
{
    //private readonly IAgentConfigurationLoader _configurationLoader;
    //private readonly OpenAISettings _openAiSettings;
    //private readonly IAgentConfigurationStore _configurationStore;
    //private static readonly string _settingsFilePath;

    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly IAgentStore _agentStore;
    private readonly IAgentGroupChatRunner _agentGroupChatRunner;

    private readonly HttpClient _httpClient = new();

    public AgentOrchestrator(ILogger<AgentOrchestrator> logger, IAgentStore agentStore, IAgentGroupChatRunner agentGroupChatRunner)
    {
        _logger = logger;
        _agentStore = agentStore;
        _agentGroupChatRunner = agentGroupChatRunner;
        //_configurationStore = configurationStore;
        //_openAiSettings = openAiSettings.Value;
        //_settingsFilePath = Path.Combine(environment.ContentRootPath, "agentsettings.json");
    }

    public Task<Guid> ExecuteAsync(Uri callback, string requestId, string input, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        return HandleAsync(callback, requestId, input, sessionId, cancellationToken);
    }

    public async Task<Guid> HandleAsync(Uri callback, string requestId, string message, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        //AgentConfigurationSnapshot snapshot = _configurationStore.Current;
        AgentEntity entity = _agentStore.GetAgent(sessionId);
        var mode = entity.ServiceModel.Execution.Mode;
        entity.ChatMessages.Add(new UserChatMessage(message));

        _logger.LogInformation(
            "orchestrator request received. requestId={RequestId}, sessionId={SessionId}, callback={Callback}, input={Input}",
            requestId,
            entity.Id,
            callback,
            message);

        foreach (AgentGroupModel agentGroup in entity.ServiceModel.Agents)
        {
            AgentGroupChatResult result;
            string? error = null;

            try
            {
                result = await _agentGroupChatRunner.RunAsync(agentGroup, entity.ChatMessages, 5, 10, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "エージェント '{AgentGroupId}' の実行中にエラーが発生しました。", agentGroup.Id);
                result = new AgentGroupChatResult(agentGroup.Id, []);
                error = exception.Message;
            }

            try
            {
                var callbackPayload = new
                {
                    RequestId = requestId,
                    SessionId = entity.Id,
                    Result = result,
                    Error = error
                };

                _logger.LogInformation(
                    "sending callback. requestId={RequestId}, payload={Payload}",
                    requestId,
                    JsonSerializer.Serialize(callbackPayload));

                using var response = await _httpClient.PostAsJsonAsync(
                    callback,
                    callbackPayload,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "callback completed. requestId={RequestId}, statusCode={StatusCode}",
                    requestId,
                    (int)response.StatusCode);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "callback の送信に失敗しました。requestId={RequestId}, agentGroupId={AgentGroupId}",
                    requestId,
                    agentGroup.Id);
            }
        }

        return entity.Id;
        //return string.Equals(mode, "parallel", StringComparison.OrdinalIgnoreCase) ? await ExecuteParallelAsync(entity, message, cancellationToken) : await ExecuteSequentialAsync(entity, message, cancellationToken);
    }

    //private async Task<IReadOnlyDictionary<string, string>> ExecuteSequentialAsync(AgentEntity entity, string message, CancellationToken cancellationToken)
    //{
    //    var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    //    foreach (AgentGroupModel agent in entity.ServiceModel.Agents)
    //    {
    //        results[agent.Id] = await ExecuteAgentAsync(entity, agent, message, cancellationToken);
    //    }

    //    return results;
    //}

    //private async Task<IReadOnlyDictionary<string, string>> ExecuteParallelAsync(AgentConfigurationSnapshot snapshot, string message, CancellationToken cancellationToken)
    //{
    //    var results = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    //    using var semaphore = new SemaphoreSlim(snapshot.Raw.Execution.MaxDegreeOfParallelism);

    //    var tasks = snapshot.EnabledAgentsOrdered.Select(async agent =>
    //    {
    //        await semaphore.WaitAsync(cancellationToken);
    //        try
    //        {
    //            results[agent.Id] = await ExecuteAgentAsync(snapshot, agent, message, cancellationToken);
    //        }
    //        finally
    //        {
    //            semaphore.Release();
    //        }
    //    });

    //    await Task.WhenAll(tasks);
    //    return new Dictionary<string, string>(results, StringComparer.OrdinalIgnoreCase);
    //}

    //private async Task<string> ExecuteAgentAsync(AgentConfigurationSnapshot snapshot, AgentGroupDefinition agentGroup, string message, CancellationToken cancellationToken)
    //{
    //    var provider = snapshot.Providers[agentGroup.ProviderRef];
    //    var endpoint = ResolveEndpoint(provider.Endpoint);

    //    var chatClient = new ChatClient(
    //        credential: new ApiKeyCredential(_openAiSettings.ApiKey),
    //        model: agentGroup.Deployment,
    //        options: new OpenAIClientOptions
    //        {
    //            Endpoint = new Uri(endpoint)
    //        });

    //    IReadOnlyList<ChatMessage> messages =
    //    [
    //        new SystemChatMessage(agentGroup.Prompt.System),
    //        new UserChatMessage(message)
    //    ];

    //    var timeoutSeconds = agentGroup.TimeoutSeconds ?? snapshot.Raw.Service.DefaultTimeoutSeconds;
    //    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    //    linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

    //    ChatCompletion completion = await chatClient.CompleteChatAsync(messages, cancellationToken: linkedCts.Token);

    //    return string.Join(
    //        Environment.NewLine,
    //        completion.Content
    //            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
    //            .Select(x => x.Text));
    //}

    //private string ResolveEndpoint(string configuredEndpoint)
    //{
    //    return string.IsNullOrWhiteSpace(configuredEndpoint)
    //        || configuredEndpoint.Contains("your-resource", StringComparison.OrdinalIgnoreCase)
    //        ? _openAiSettings.Endpoint
    //        : configuredEndpoint;
    //}
}
