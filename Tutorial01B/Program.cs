using ConversationSuggestionService.Configuration;
using ConversationSuggestionService.Services;
using Tutorial01B.Agents;
using Tutorial01B.DataAccess.Models;
using Tutorial01B.DataAccess.Service;
//using Tutorial01B.Extensions;
using Tutorial01B.Models;
using Tutorial01B.Services;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
// IHttpClientFactoryをDIへ登録
builder.Services.AddHttpClient();

//builder.Services.AddOpenAIChatModule(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddScoped<IAgentExecutionService, AgentExecutionService>();
builder.Services.AddScoped<IAgentConfigurationWatcher, AgentConfigurationWatcher>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddScoped<IAgentConfigurationSnapshotFactory, AgentConfigurationSnapshotFactory>();
builder.Services.AddScoped<IAgentConfigurationStore, AgentConfigurationStore>();
builder.Services.AddScoped<IAgentServiceMapper, AgentServiceMapper>();
builder.Services.AddScoped<IAgentEntityFactory, AgentEntityFactory>();
builder.Services.AddScoped<IAgentStore, AgentStore>();
builder.Services.AddScoped<IAgentGroupChatRunner, AgentGroupChatRunner>();
builder.Services.AddScoped<IAgentConfigurationLoader,AgentConfigurationLoader>();
builder.Services.AddScoped<AgentOrchestrator>();
builder.Services.AddScoped<IChatService, MultiAgentChatService>();
builder.Services.AddScoped<IAgentConfigurationFile, AgentConfigurationFile>();


var app = builder.Build();

app.MapControllers();

app.MapGet("/", () => Results.Text("Tutorial01B Web API is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Start the agent configuration watcher
// var watcher = app.Services.GetRequiredService<IAgentConfigurationWatcher>();

//var app = builder.Build();

// IAgentConfigurationWatcherはScopedなので、スコープ内から取得する
using var watcherScope = app.Services.CreateScope();

var watcher = watcherScope.ServiceProvider
    .GetRequiredService<IAgentConfigurationWatcher>();

watcher.Start();

await app.RunAsync();

//watcher.Start();

app.Run();
