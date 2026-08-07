using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

var options = ClientOptions.FromEnvironment();
var inputFilePath = args.Length > 0 ? args[0] : "inputs.txt";

if (!File.Exists(inputFilePath))
{
    Console.Error.WriteLine($"入力ファイルが見つかりません: {Path.GetFullPath(inputFilePath)}");
    return;
}

var lines = File.ReadAllLines(inputFilePath, Encoding.UTF8)
    .Select(x => x.Trim())
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .ToArray();

if (lines.Length == 0)
{
    Console.Error.WriteLine("入力ファイルに送信対象の行がありません。");
    return;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var bus = new MessageBus();
var callbackServerTask = RunCallbackServerAsync(options.CallbackUrl, bus, cts.Token);
var senderTask = SendInputsAsync(lines, options, bus, cts.Token);
var rendererTask = RenderLoopAsync(bus, cts.Token);

await senderTask;
await Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ContinueWith(_ => Task.CompletedTask);
cts.Cancel();

await Task.WhenAll(callbackServerTask, rendererTask);
return;

static async Task SendInputsAsync(
    string[] lines,
    ClientOptions options,
    MessageBus bus,
    CancellationToken cancellationToken)
{
    using var client = new HttpClient
    {
        BaseAddress = new Uri(options.BaseUrl)
    };

    foreach (var line in lines)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestId = $"REQ-{DateTimeOffset.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..28];
        var request = new ExecuteOrchestratorRequest(
            line,
            null,
            options.CallbackUrl,
            requestId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/orchestrator/execute")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.TryAddWithoutValidation(options.ApiKeyHeader, options.ApiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        bus.EnqueueInput($"[{requestId}] {line}");
        if (!response.IsSuccessStatusCode)
        {
            bus.EnqueueCallback($"送信失敗 {response.StatusCode}: {body}");
        }

        await Task.Delay(200, cancellationToken);
    }
}

static async Task RunCallbackServerAsync(string callbackUrl, MessageBus bus, CancellationToken cancellationToken)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls(callbackUrl);
    var app = builder.Build();

    app.MapPost("/", async (HttpContext context) =>
    {
        string raw;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            raw = await reader.ReadToEndAsync(cancellationToken);
        }

        var message = TryFormatCallback(raw);
        bus.EnqueueCallback(message);
        return Results.Ok();
    });

    app.MapGet("/", () => Results.Ok("callback receiver ready"));

    await app.StartAsync(cancellationToken);
    await app.WaitForShutdownAsync(cancellationToken);
}

static async Task RenderLoopAsync(MessageBus bus, CancellationToken cancellationToken)
{
    Console.OutputEncoding = Encoding.UTF8;
    while (!cancellationToken.IsCancellationRequested)
    {
        var drained = false;
        while (bus.TryDequeueInput(out var input))
        {
            drained = true;
            Console.ResetColor();
            Console.WriteLine(input);
        }

        while (bus.TryDequeueCallback(out var callback))
        {
            drained = true;
            var width = Math.Max(40, Console.WindowWidth - 2);
            var shown = callback.Length > width ? callback[..width] : callback;
            var pad = Math.Max(0, width - shown.Length);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{new string(' ', pad)}{shown}");
            Console.ResetColor();
        }

        if (!drained)
        {
            await Task.Delay(100, cancellationToken);
        }
    }
}

static string TryFormatCallback(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return "(empty callback)";
    }

    try
    {
        using var json = JsonDocument.Parse(raw);
        return JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = false });
    }
    catch
    {
        return raw;
    }
}

sealed record ExecuteOrchestratorRequest(string Input, Guid? SessionId, string CallbackUrl, string RequestId);

sealed class MessageBus
{
    private readonly ConcurrentQueue<string> _inputQueue = new();
    private readonly ConcurrentQueue<string> _callbackQueue = new();

    public void EnqueueInput(string value) => _inputQueue.Enqueue(value);
    public void EnqueueCallback(string value) => _callbackQueue.Enqueue(value);
    public bool TryDequeueInput(out string value) => _inputQueue.TryDequeue(out value!);
    public bool TryDequeueCallback(out string value) => _callbackQueue.TryDequeue(out value!);
}

sealed class ClientOptions
{
    public string BaseUrl { get; init; } = "http://localhost:12670/";
    public string ApiKeyHeader { get; init; } = "x-api-key";
    public string ApiKey { get; init; } = "local-dev-key";
    public string CallbackUrl { get; init; } = "http://localhost:1305";

    public static ClientOptions FromEnvironment()
    {
        return new ClientOptions
        {
            BaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("ORCHESTRATOR_BASE_URL") ?? "http://localhost:12670/"),
            ApiKeyHeader = Environment.GetEnvironmentVariable("ORCHESTRATOR_API_KEY_HEADER") ?? "x-api-key",
            ApiKey = Environment.GetEnvironmentVariable("ORCHESTRATOR_API_KEY") ?? "local-dev-key",
            CallbackUrl = Environment.GetEnvironmentVariable("ORCHESTRATOR_CALLBACK_URL") ?? "http://localhost:1305"
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
