using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SampleOpenAIApp.Clients;
using Tutorial01B.Agents;
using Tutorial01B.Clients;
using Tutorial01B.DataAccess.Service;
using Tutorial01B.Models;
using Tutorial01B.Services;

namespace Tutorial01B.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// OpenAI Chat モジュールをサービスコレクションに追加する拡張メソッド。
    /// OpenAISettings を appsettings.json の OpenAI セクションにバインドし、
    /// 必須項目のバリデーションや ChatClient の DI 登録を行う。
    /// </summary>
    public static IServiceCollection AddOpenAIChatModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // OpenAISettings を appsettings.json の OpenAI セクションにバインド
        services
            .AddOptions<OpenAISettings>()
            .Bind(configuration.GetSection(OpenAISettings.SectionName))
            .PostConfigure(settings =>
            {
                // 環境変数があれば優先して設定
                settings.ApiKey =
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                    ?? settings.ApiKey;

                settings.Endpoint =
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                    ?? settings.Endpoint;
            })
            // 必須項目のバリデーション
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DeploymentName),
                "OpenAI:DeploymentName is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Endpoint),
                "OpenAI:Endpoint is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
                "AZURE_OPENAI_API_KEY or OpenAI:ApiKey is required.")
            .ValidateOnStart();

        // ChatClient の DI 登録
        services.AddSingleton(sp =>
        {
            OpenAISettings settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;

            return new ChatClient(credential: new ApiKeyCredential(settings.ApiKey), model: settings.DeploymentName, options: new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) });
        });

        services.AddSingleton<IChatCompletionExecutor, OpenAIChatCompletionExecutor>();
        services.AddSingleton<ChatService>();
        services.AddHttpClient();

        // マルチエージェントシステムの DI 登録
        services.AddMultiAgentSystem(configuration);

        return services;
    }

    /// <summary>
    /// マルチエージェントシステムをサービスコレクションに追加する拡張メソッド。
    /// AgentSettings の Enabled 設定に応じてエージェントを DI 登録する。
    /// </summary>
    public static IServiceCollection AddMultiAgentSystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // AgentSettings を appsettings.json から取得
        var agentSettings = configuration
            .GetSection(AgentSettings.SectionName)
            .Get<AgentSettings>() ?? new AgentSettings();

        // 有効なエージェントか判定
        bool IsEnabled(string agentName) =>
            agentSettings.Enabled.Count == 0 ||
            agentSettings.Enabled.Contains(agentName, StringComparer.OrdinalIgnoreCase);

        // SummaryAgent の DI 登録
        if (IsEnabled(nameof(SummaryAgent)))
            services.AddSingleton<IAgent, SummaryAgent>();

        // TranslationAgent の DI 登録
        if (IsEnabled(nameof(TranslationAgent)))
            services.AddSingleton<IAgent, TranslationAgent>();

        // 動的設定ロード関連の DI 登録
        services.AddSingleton<AgentConfigurationLoader>();

        // オーケストレータとサービスの DI 登録
        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<IChatService, MultiAgentChatService>();

        return services;
    }
}