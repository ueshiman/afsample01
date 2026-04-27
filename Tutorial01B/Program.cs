using ConversationSuggestionService.Services;

using Tutorial01B.Extensions;
using Tutorial01B.Services;

#pragma warning disable OPENAI001

using System.Text.Json;
using ConversationSuggestionService.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

//var loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddSimpleConsole();
//});

//var loader = new AgentConfigurationLoader();
//var store = new AgentConfigurationStore();
//var logger = loggerFactory.CreateLogger<AgentConfigurationWatcher>();

//using var watcher = new AgentConfigurationWatcher(
//    filePath: "agentsettings.jsonc",
//    loader: loader,
//    store: store,
//    logger: logger);

//watcher.Start();

// どこからでも現在設定を参照
//var current = store.Current;

//var options = new JsonSerializerOptions
//{
//    PropertyNameCaseInsensitive = true,
//    ReadCommentHandling = JsonCommentHandling.Skip,
//    AllowTrailingCommas = true
//};

//var definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, options)
//                 ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

//var providerMap = definition.Providers
//    .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

//var callbackMap = definition.Callbacks
//    .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AgentConfigurationLoader>();
builder.Services.AddSingleton<AgentConfigurationStore>();
builder.Services.AddSingleton<AgentExecutionService>();
builder.Services.AddOpenAIChatModule(builder.Configuration);

var app = builder.Build();

var loader = app.Services.GetRequiredService<AgentConfigurationLoader>();
var store = app.Services.GetRequiredService<AgentConfigurationStore>();


builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

store.Set(loader.Load("agentsettings.json"));

app.MapGet("/", () => Results.Ok("Agent service is running."));

app.MapPost("/api/conversations", async (
    ConversationRequest request,
    AgentExecutionService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ExecuteAsync(request.Text, cancellationToken);
    return Results.Ok(result);
});

app.Run();

public sealed class ConversationRequest
{
    public string Text { get; set; } = string.Empty;
}

//IChatService chatService = host.Services.GetRequiredService<IChatService>();
//await chatService.RunSampleAsync();
