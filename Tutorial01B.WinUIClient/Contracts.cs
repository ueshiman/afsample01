using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tutorial01B_WinUIClient;

public sealed record ExecuteOrchestratorRequest(
    [property: JsonPropertyName("input")] string Input,
    [property: JsonPropertyName("sessionId")] Guid? SessionId,
    [property: JsonPropertyName("callbackUrl")] string CallbackUrl,
    [property: JsonPropertyName("requestId")] string RequestId);

public sealed record ExecuteAcceptedResponse(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("sessionId")] Guid? SessionId,
    [property: JsonPropertyName("status")] string Status);

public sealed record ExecuteCallbackPayload(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("sessionId")] Guid? SessionId,
    [property: JsonPropertyName("result")] JsonElement Result,
    [property: JsonPropertyName("error")] string? Error);
