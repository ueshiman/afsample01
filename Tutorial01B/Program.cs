using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tutorial01B.Extensions;
using Tutorial01B.Services;

#pragma warning disable OPENAI001

using System.Text.Json;
using ConversationSuggestionService.Configuration;

var json = await File.ReadAllTextAsync("agentsettings.json");

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
};

var definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, options)
                 ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

var providerMap = definition.Providers
    .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

var callbackMap = definition.Callbacks
    .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenAIChatModule(builder.Configuration);

using IHost host = builder.Build();

IChatService chatService = host.Services.GetRequiredService<IChatService>();
await chatService.RunSampleAsync();
