using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tutorial01B.Extensions;
using Tutorial01B.Services;

#pragma warning disable OPENAI001

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenAIChatModule(builder.Configuration);

using IHost host = builder.Build();

IChatService chatService = host.Services.GetRequiredService<IChatService>();
await chatService.RunSampleAsync();
