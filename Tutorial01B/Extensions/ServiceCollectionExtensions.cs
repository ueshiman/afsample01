using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SampleOpenAIApp.Clients;
using Tutorial01B.Agents;
using Tutorial01B.Clients;
using Tutorial01B.Models;
using Tutorial01B.Services;

namespace Tutorial01B.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAIChatModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OpenAISettings>()
            .Bind(configuration.GetSection(OpenAISettings.SectionName))
            .PostConfigure(settings =>
            {
                settings.ApiKey =
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                    ?? settings.ApiKey;

                settings.Endpoint =
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                    ?? settings.Endpoint;
            })
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DeploymentName),
                "OpenAI:DeploymentName is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Endpoint),
                "OpenAI:Endpoint is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
                "AZURE_OPENAI_API_KEY or OpenAI:ApiKey is required.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            OpenAISettings settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;

            return new ChatClient(
                credential: new ApiKeyCredential(settings.ApiKey),
                model: settings.DeploymentName,
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(settings.Endpoint)
                });
        });

        services.AddSingleton<IChatCompletionExecutor, OpenAIChatCompletionExecutor>();
        services.AddSingleton<ChatService>();

        services.AddMultiAgentSystem(configuration);

        return services;
    }

    public static IServiceCollection AddMultiAgentSystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var agentSettings = configuration
            .GetSection(AgentSettings.SectionName)
            .Get<AgentSettings>() ?? new AgentSettings();

        bool IsEnabled(string agentName) =>
            agentSettings.Enabled.Count == 0 ||
            agentSettings.Enabled.Contains(agentName, StringComparer.OrdinalIgnoreCase);

        if (IsEnabled(nameof(SummaryAgent)))
            services.AddSingleton<IAgent, SummaryAgent>();

        if (IsEnabled(nameof(TranslationAgent)))
            services.AddSingleton<IAgent, TranslationAgent>();

        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<IChatService, MultiAgentChatService>();

        return services;
    }
}