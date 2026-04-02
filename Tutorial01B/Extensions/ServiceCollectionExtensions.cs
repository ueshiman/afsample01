using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

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
        services.AddSingleton<IChatService, ChatService>();

        return services;
    }
}