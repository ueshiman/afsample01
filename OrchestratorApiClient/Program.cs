using System.Net.Http.Json;
using System.Text.Json;

var options = ClientOptions.FromEnvironment();
var input = args.Length > 0 ? string.Join(' ', args) : Prompt("input", "Hello from OrchestratorApiClient");
var sessionId = Environment.GetEnvironmentVariable("ORCHESTRATOR_SESSION_ID") ?? Guid.NewGuid().ToString("N");

using var client = new HttpClient
{
    BaseAddress = new Uri(options.BaseUrl)
};

using var request = new HttpRequestMessage(HttpMethod.Post, "api/orchestrator/execute")
{
    Content = JsonContent.Create(new ExecuteOrchestratorRequest(input, sessionId))
};

request.Headers.TryAddWithoutValidation(options.ApiKeyHeader, options.ApiKey);

Console.WriteLine($"POST {new Uri(client.BaseAddress!, "api/orchestrator/execute")}");
Console.WriteLine($"{options.ApiKeyHeader}: {MaskApiKey(options.ApiKey)}");

using var response = await client.SendAsync(request);
var raw = await response.Content.ReadAsStringAsync();

Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
Console.WriteLine("Body:");
Console.WriteLine(FormatJsonOrRaw(raw));

return;

static string Prompt(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}

static string MaskApiKey(string apiKey)
{
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return "(empty)";
    }

    if (apiKey.Length <= 4)
    {
        return "****";
    }

    return $"{new string('*', apiKey.Length - 4)}{apiKey[^4..]}";
}

static string FormatJsonOrRaw(string content)
{
    try
    {
        using var json = JsonDocument.Parse(content);
        return JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }
    catch
    {
        return content;
    }
}

sealed record ExecuteOrchestratorRequest(string Input, string SessionId);

sealed class ClientOptions
{
    public string BaseUrl { get; init; } = "http://localhost:12670/";
    public string ApiKeyHeader { get; init; } = "x-api-key";
    public string ApiKey { get; init; } = "local-dev-key";

    public static ClientOptions FromEnvironment()
    {
        return new ClientOptions
        {
            BaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("ORCHESTRATOR_BASE_URL") ?? "http://localhost:12670/"),
            ApiKeyHeader = Environment.GetEnvironmentVariable("ORCHESTRATOR_API_KEY_HEADER") ?? "x-api-key",
            ApiKey = Environment.GetEnvironmentVariable("ORCHESTRATOR_API_KEY") ?? "local-dev-key"
        };
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "http://localhost:12670/";
        }

        return baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
    }
}
