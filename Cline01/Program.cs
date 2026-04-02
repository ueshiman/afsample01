using Cline01.Services;
using Cline01.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OpenAI settings
builder.Services.Configure<OpenAISettings>(
    builder.Configuration.GetSection("OpenAI"));

// Configure Webhook settings
builder.Services.Configure<WebhookSettings>(
    builder.Configuration.GetSection("Webhook"));

// Register HttpClient for Webhook
builder.Services.AddHttpClient("Webhook", client =>
{
    var webhookSettings = builder.Configuration.GetSection("Webhook");
    var timeoutSeconds = webhookSettings.GetValue<int>("TimeoutSeconds", 30);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Register application services
builder.Services.AddSingleton<IEvaluationJobQueue, EvaluationJobQueue>();
builder.Services.AddSingleton<IAgentDefinitionLoader, AgentDefinitionLoader>();
builder.Services.AddSingleton<IAgentDefinitionValidator, AgentDefinitionValidator>();
builder.Services.AddScoped<IAgentExecutor, AgentExecutor>();
builder.Services.AddScoped<IAgentConversationCoordinator, AgentConversationCoordinator>();
builder.Services.AddScoped<IWebhookNotifier, WebhookNotifier>();

// Register background service
builder.Services.AddHostedService<EvaluationBackgroundService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("Multi-Agent Evaluation Service starting...");
app.Run();
