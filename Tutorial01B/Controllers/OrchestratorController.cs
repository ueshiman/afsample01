using System.Net.Http.Json;
using ConversationSuggestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Tutorial01B.Agents;
using Tutorial01B.Services;

namespace Tutorial01B.Controllers;

[ApiController]
[Route("api/orchestrator")]
public sealed class OrchestratorController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAgentExecutionService _agentExecutionService;

    private readonly IAgentStore _agentStore;
    private readonly ILogger<OrchestratorController> _logger;

    public OrchestratorController(
        AgentOrchestrator orchestrator,
        IHttpClientFactory httpClientFactory,
        ILogger<OrchestratorController> logger, IAgentExecutionService agentExecutionService, IAgentStore agentStore)
    {
        _orchestrator = orchestrator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _agentExecutionService = agentExecutionService;
        _agentStore = agentStore;
    }

    [HttpPost("execute")]
    public IActionResult Execute([FromBody] ExecuteOrchestratorRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Input))
        {
            return BadRequest(new { error = "input is required." });
        }

        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            return BadRequest(new { error = "callbackUrl is required." });
        }

        if (!Uri.TryCreate(request.CallbackUrl, UriKind.Absolute, out var callbackUri)
            || (callbackUri.Scheme != Uri.UriSchemeHttp && callbackUri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { error = "callbackUrl must be absolute http/https URL." });
        }

        var requestId = request.RequestId;

        request.SessionId ??= _agentStore.CreateAgent();
        
        _ = Task.Run(() => ExecuteAndSendCallbackAsync(requestId, request));

        return Accepted(new ExecuteAcceptedResponse
        {
            RequestId = requestId,
            SessionId = request.SessionId,
            Status = "accepted"
        });
    }

    private async Task ExecuteAndSendCallbackAsync(string requestId, ExecuteOrchestratorRequest request)
    {
        try
        {
            var result = await _orchestrator.ExecuteAsync(new System.Uri(request.CallbackUrl), request.Input, request.SessionId, CancellationToken.None);

            //var payload = new ExecuteCallbackPayload
            //{
            //    RequestId = requestId,
            //    SessionId = request.SessionId,
            //    Input = request.Input,
            //    Status = "completed",
            //    Results = result
            //};

            //await SendCallbackAsync(callbackUri, payload, requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "orchestrator execution failed. requestId={RequestId}", requestId);

            //var payload = new ExecuteCallbackPayload
            //{
            //    RequestId = requestId,
            //    SessionId = request.SessionId,
            //    Input = request.Input,
            //    Status = "failed",
            //    Error = ex.Message,
            //    Results = new Dictionary<string, string>()
            //};

            //await SendCallbackAsync(callbackUri, payload, requestId);
        }
    }

    //private async Task SendCallbackAsync(Uri callbackUri, ExecuteCallbackPayload payload, string requestId)
    //{
    //    try
    //    {
    //        var client = _httpClientFactory.CreateClient();
    //        using var response = await client.PostAsJsonAsync(callbackUri, payload);

    //        if (!response.IsSuccessStatusCode)
    //        {
    //            _logger.LogWarning(
    //                "callback failed. requestId={RequestId}, statusCode={StatusCode}",
    //                requestId,
    //                (int)response.StatusCode);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "callback post failed. requestId={RequestId}", requestId);
    //    }
    //}

    public sealed class ExecuteOrchestratorRequest
    {
        public string Input { get; set; } = string.Empty;

        public Guid? SessionId { get; set; }

        public string CallbackUrl { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;
    }

    public sealed class ExecuteAcceptedResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public Guid? SessionId { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public sealed class ExecuteCallbackPayload
    {
        public string RequestId { get; set; } = string.Empty;

        public string? SessionId { get; set; }

        public string Input { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Error { get; set; }

        public IReadOnlyDictionary<string, string> Results { get; set; } = new Dictionary<string, string>();
    }
}
