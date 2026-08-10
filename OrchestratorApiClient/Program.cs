using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

// 実行オプションを環境変数から読み込み
var options = ClientOptions.FromEnvironment();

// 表示設定（appsettings + 環境変数）
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var showInputLogs = config.GetValue("Client:ShowInputLogs", true);

// 第1引数に入力ファイルパス、未指定時は既定値を使用
var inputFilePath = args.Length > 0 ? args[0] : "inputs.txt";

// 入力ファイル存在チェック
if (!File.Exists(inputFilePath))
{
    Console.Error.WriteLine($"入力ファイルが見つかりません: {Path.GetFullPath(inputFilePath)}");
    return;
}

// 空行を除いた送信対象の入力行を読み込み
var lines = File.ReadAllLines(inputFilePath, Encoding.UTF8)
    .Select(x => x.Trim())
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .ToArray();

if (lines.Length == 0)
{
    Console.Error.WriteLine("入力ファイルに送信対象の行がありません。");
    return;
}

// Ctrl + C で安全に停止できるようにキャンセル機構を設定
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// 送信処理、コールバック受信サーバー、表示ループを並列起動
var bus = new MessageBus();
var callbackServerTask = RunCallbackServerAsync(options.CallbackUrl, bus, cts.Token);
var senderTask = SendInputsAsync(lines, options, bus, cts.Token);
var rendererTask = RenderLoopAsync(bus, showInputLogs, cts.Token);

// 送信完了後、コールバック受信待ちの猶予を確保してから終了
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

    Guid? sessionId = null;
    foreach (var line in lines)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // リクエストごとの追跡用IDを生成
        var requestId = $"REQ-{DateTimeOffset.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..28];
        var request = new ExecuteOrchestratorRequest(
            line,
            sessionId,
            options.CallbackUrl,
            requestId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/orchestrator/execute")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.TryAddWithoutValidation(options.ApiKeyHeader, options.ApiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // bodyがJSONなら、sessionIdを抽出して次回送信に利用
        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var root = JsonNode.Parse(body);
                var sessionIdNode = root?["sessionId"];
                if (sessionIdNode is not null && sessionIdNode is JsonValue value && value.TryGetValue<Guid>(out var parsedSessionId))
                {
                    sessionId = parsedSessionId;
                }
            }
            catch
            {
                // JSON解析失敗時は無視して次回送信へ
            }
        }

        // 入力送信ログを表示キューへ積む
        bus.EnqueueInput($"[{requestId}] {line}");
        if (!response.IsSuccessStatusCode)
        {
            bus.EnqueueCallback($"送信失敗 {response.StatusCode}: {body}");
        }

        // 短い間隔を空けて連続送信
        await Task.Delay(2000, cancellationToken);
    }
}

// コールバック受信用の最小Webサーバーを起動
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

        var decoded = DecodeCallbackPayload(raw, context.Request.ContentType);

        // 整形可否に関わらず同じ出力経路（callbackキュー）へ積む
        if (TryFormatCallback(decoded, out var message))
        {
            bus.EnqueueCallback(message);
        }
        else
        {
            Debug.WriteLine($"Failed to format callback: {message}");
        }

        return Results.Ok();
    });

    app.MapGet("/", () => Results.Ok("callback receiver ready"));

    await app.StartAsync(cancellationToken);
    await app.WaitForShutdownAsync(cancellationToken);
}

// 送信ログは通常表示、コールバックも同じコンソールへ表示
static async Task RenderLoopAsync(MessageBus bus, bool showInputLogs, CancellationToken cancellationToken)
{
    Console.OutputEncoding = Encoding.UTF8;
    while (!cancellationToken.IsCancellationRequested)
    {
        var drained = false;
        while (bus.TryDequeueInput(out var input))
        {
            drained = true;
            if (showInputLogs)
            {
                Console.ResetColor();
                Console.WriteLine(input);
            }
        }

        while (bus.TryDequeueCallback(out var callback))
        {
            drained = true;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(callback);
            Console.ResetColor();
        }

        if (!drained)
        {
            await Task.Delay(100, cancellationToken);
        }
    }
}

// callback本文のURLエンコードを必要時のみデコード
static string DecodeCallbackPayload(string raw, string? contentType)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return raw;
    }

    var trimmed = raw.TrimStart();
    if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
    {
        return raw;
    }

    if (!string.IsNullOrWhiteSpace(contentType) &&
        contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
    {
        var index = raw.IndexOf('=');
        return index >= 0 && index < raw.Length - 1
            ? WebUtility.UrlDecode(raw[(index + 1)..])
            : WebUtility.UrlDecode(raw);
    }

    if (raw.Contains('%') || raw.Contains('+'))
    {
        return WebUtility.UrlDecode(raw);
    }

    return raw;
}

// JSONなら1行に正規化し、必要な箇所をデコードして返す
static bool TryFormatCallback(string raw, out string formatted)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        formatted = "(empty callback)";
        return false;
    }

    try
    {
        var root = JsonNode.Parse(raw);

        // 例: { "results": [ { "content": "..." } ] }

        var resultsNode = root?["result"]?["results"]?[0]?["content"];

        var content = resultsNode?.GetValue<string>();

        if (!string.IsNullOrWhiteSpace(content))
        {
            formatted = content;
            return true;
        }

        formatted = "(results.content not found)";
        return false;
    }
    catch
    {
        formatted = raw;
        return false;
    }
}

static void DecodeNestedResultPayload(JsonObject root, string outerPropertyName)
{
    if (!root.TryGetPropertyValue(outerPropertyName, out var outerNode) || outerNode is null)
    {
        return;
    }

    JsonObject? innerObject = null;

    // 既に object の場合
    if (outerNode is JsonObject existingObject)
    {
        innerObject = existingObject;
    }
    // 文字列JSONの場合
    else if (outerNode is JsonValue outerValue && outerValue.TryGetValue<string>(out var outerText))
    {
        var decodedOuterText = DecodeMaybeUrlEncoded(outerText);

        if (TryParseJsonObject(decodedOuterText, out var parsedObject))
        {
            innerObject = parsedObject;
            root[outerPropertyName] = innerObject;
        }
        else
        {
            root[outerPropertyName] = decodedOuterText;
            return;
        }
    }

    if (innerObject is null)
    {
        return;
    }

    // 内側プロパティ名の揺れに対応
    DecodeInnerStringValue(innerObject, "result");
    DecodeInnerStringValue(innerObject, "reslut");
}

static void DecodeInnerStringValue(JsonObject obj, string propertyName)
{
    if (!obj.TryGetPropertyValue(propertyName, out var node) || node is not JsonValue value)
    {
        return;
    }

    if (!value.TryGetValue<string>(out var text))
    {
        return;
    }

    obj[propertyName] = DecodeMaybeUrlEncoded(text);
}

static bool TryParseJsonObject(string text, out JsonObject? parsed)
{
    parsed = null;
    try
    {
        parsed = JsonNode.Parse(text) as JsonObject;
        return parsed is not null;
    }
    catch
    {
        return false;
    }
}

static string DecodeMaybeUrlEncoded(string value)
{
    if (string.IsNullOrEmpty(value))
    {
        return value;
    }

    return (value.Contains('%') || value.Contains('+'))
        ? WebUtility.UrlDecode(value)
        : value;
}

sealed record ExecuteOrchestratorRequest(string Input, Guid? SessionId, string CallbackUrl, string RequestId);

// 送信ログとコールバックログを分離して保持する軽量キュー
sealed class MessageBus
{
    private readonly ConcurrentQueue<string> _inputQueue = new();
    private readonly ConcurrentQueue<string> _callbackQueue = new();

    public void EnqueueInput(string value) => _inputQueue.Enqueue(value);
    public void EnqueueCallback(string value) => _callbackQueue.Enqueue(value);
    public bool TryDequeueInput(out string value) => _inputQueue.TryDequeue(out value!);
    public bool TryDequeueCallback(out string value) => _callbackQueue.TryDequeue(out value!);
}

// 環境変数から接続先設定を組み立てるオプション
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

    // BaseUrl末尾のスラッシュを保証
    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "http://localhost:12670/";
        }

        return baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
    }
}
