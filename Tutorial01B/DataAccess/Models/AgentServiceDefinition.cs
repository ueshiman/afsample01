using System.Text.Json.Serialization;

namespace ConversationSuggestionService.Configuration;

/// <summary>
/// エージェント サービス全体の定義を表します。
/// 構成ファイルのルート要素として利用されます。
/// </summary>
public sealed class AgentServiceDefinition
{
    /// <summary>
    /// 構成定義のバージョンを取得または設定します。
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1";

    /// <summary>
    /// サービス共通設定を取得または設定します。
    /// </summary>
    [JsonPropertyName("service")]
    public ServiceDefinition Service { get; set; } = new();

    /// <summary>
    /// 利用可能なプロバイダー定義一覧を取得または設定します。
    /// </summary>
    [JsonPropertyName("providers")]
    public List<ProviderDefinition> Providers { get; set; } = new();

    /// <summary>
    /// 利用可能なコールバック定義一覧を取得または設定します。
    /// </summary>
    [JsonPropertyName("callbacks")]
    public List<CallbackDefinition> Callbacks { get; set; } = new();

    /// <summary>
    /// 実行モードに関する設定を取得または設定します。
    /// </summary>
    [JsonPropertyName("execution")]
    public ExecutionDefinition Execution { get; set; } = new();

    /// <summary>
    /// 実行対象エージェントの定義一覧を取得または設定します。
    /// </summary>
    [JsonPropertyName("agents")]
    public List<AgentGroupDefinition> Agents { get; set; } = new();
}



public sealed class AgentGroupDefinition
{
    /// <summary>
    /// プロバイダー識別子を取得または設定します。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 入力元を取得または設定します。
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 入力フォーマットを取得または設定します。
    /// </summary>
    [JsonPropertyName("plainText")]
    public string PlainText { get; set; } = string.Empty;

    /// <summary>
    /// 実行対象エージェントの定義一覧を取得または設定します。
    /// </summary>
    [JsonPropertyName("agentsGroup")]
    public List<AgentDefinition> AgentGroup { get; set; } = new ();
}

/// <summary>
/// サービス全体に適用される基本設定を表します。
/// </summary>
public sealed class ServiceDefinition
{
    /// <summary>
    /// サービス名を取得または設定します。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 既定ロケールを取得または設定します。
    /// </summary>
    [JsonPropertyName("defaultLocale")]
    public string DefaultLocale { get; set; } = "ja-JP";

    /// <summary>
    /// 既定タイムアウト秒数を取得または設定します。
    /// </summary>
    [JsonPropertyName("defaultTimeoutSeconds")]
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// LLM などの外部プロバイダー接続情報を表します。
/// 
/// </summary>
public sealed class ProviderDefinition
{
    /// <summary>
    /// プロバイダー識別子を取得または設定します。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// プロバイダー種別を取得または設定します。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 接続先エンドポイントを取得または設定します。
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// プロバイダー名を取得または設定します。
    /// </summary>
    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクト名を取得または設定します。
    /// </summary>
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 認証設定を取得または設定します。
    /// </summary>
    [JsonPropertyName("authentication")]
    public AuthenticationDefinition Authentication { get; set; } = new();

    /// <summary>
    /// プロバイダー既定値を取得または設定します。
    /// </summary>
    [JsonPropertyName("defaults")]
    public ProviderDefaultsDefinition Defaults { get; set; } = new();

    /// <summary>
    /// ログ出力ポリシーを取得または設定します。
    /// </summary>
    [JsonPropertyName("logging")]
    public string? Logging { get; set; }
}

/// <summary>
/// プロバイダー認証情報を表します。
/// </summary>
public sealed class AuthenticationDefinition
{
    /// <summary>
    /// 認証方式を取得または設定します。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// API キーを保持する環境変数名を取得または設定します。
    /// </summary>
    [JsonPropertyName("keyName")]
    public string? ApiKeyEnvVar { get; set; }
}

/// <summary>
/// プロバイダーの既定推論パラメーターを表します。
/// </summary>
public sealed class ProviderDefaultsDefinition
{
    /// <summary>
    /// 既定温度パラメーターを取得または設定します。
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// 既定最大出力トークン数を取得または設定します。
    /// </summary>
    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}

/// <summary>
/// エージェント実行後の通知先コールバックを表します。
/// </summary>
public sealed class CallbackDefinition
{
    /// <summary>
    /// コールバック識別子を取得または設定します。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// コールバック種別を取得または設定します。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// コールバック送信先 URL を取得または設定します。
    /// </summary>agents
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 会話内容を送信ペイロードに含めるかどうかを取得または設定します。
    /// </summary>
    [JsonPropertyName("includeConversation")]
    public bool IncludeConversation { get; set; }

    /// <summary>
    /// エージェント メタデータを送信ペイロードに含めるかどうかを取得または設定します。
    /// </summary>
    [JsonPropertyName("includeAgentMetadata")]
    public bool IncludeAgentMetadata { get; set; }
}

/// <summary>
/// エージェント群の実行制御設定を表します。
/// </summary>
public sealed class ExecutionDefinition
{
    /// <summary>
    /// 実行モードを取得または設定します。
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "parallel";

    /// <summary>
    /// 戻り値の集約モードを取得または設定します。
    /// </summary>
    [JsonPropertyName("returnMode")]
    public string ReturnMode { get; set; } = "perAgent";

    /// <summary>
    /// 並列実行時の最大同時実行数を取得または設定します。
    /// </summary>
    [JsonPropertyName("maxDegreeOfParallelism")]
    public int MaxDegreeOfParallelism { get; set; } = 3;
}

/// <summary>
/// 個別エージェントの実行定義を表します。
/// </summary>
public sealed class AgentDefinition
{
    /// <summary>
    /// エージェント識別子を取得または設定します。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 表示名を取得または設定します。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// エージェントが有効かどうかを取得または設定します。
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// エージェント種別を取得または設定します。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 使用するプロバイダー参照 ID を取得または設定します。
    /// </summary>
    [JsonPropertyName("providerRef")]
    public string ProviderRef { get; set; } = string.Empty;

    /// <summary>
    /// プロバイダー上のデプロイメント名を取得または設定します。
    /// </summary>
    [JsonPropertyName("deployment")]
    public string Deployment { get; set; } = string.Empty;

    /// <summary>
    /// 使用するコールバック参照 ID を取得または設定します。
    /// </summary>
    [JsonPropertyName("callbackRef")]
    public string CallbackRef { get; set; } = string.Empty;

    /// <summary>
    /// 実行優先度を取得または設定します。
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// 個別タイムアウト秒数を取得または設定します。
    /// null の場合はサービス既定値を使用します。
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// プロンプト定義を取得または設定します。
    /// </summary>
    [JsonPropertyName("prompt")]
    public PromptDefinition Prompt { get; set; } = new();

    /// <summary>
    /// 入力データ定義を取得または設定します。
    /// </summary>
    [JsonPropertyName("input")]
    public InputDefinition Input { get; set; } = new();

    /// <summary>
    /// 出力形式定義を取得または設定します。
    /// </summary>
    [JsonPropertyName("output")]
    public OutputDefinition Output { get; set; } = new();

    /// <summary>
    /// エージェント固有の推論設定を取得または設定します。
    /// </summary>
    [JsonPropertyName("settings")]
    public AgentSettingsDefinition Settings { get; set; } = new();
}

/// <summary>
/// システム プロンプト設定を表します。
/// </summary>
public sealed class PromptDefinition
{
    /// <summary>
    /// システム メッセージ本文を取得または設定します。
    /// </summary>
    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;
}

/// <summary>
/// エージェント入力の取得方式を表します。
/// </summary>
public sealed class InputDefinition
{
    /// <summary>
    /// 入力ソースを取得または設定します。
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "conversation";

    /// <summary>
    /// 入力フォーマットを取得または設定します。
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "plainText";

    /// <summary>
    /// 参照する最大ターン数を取得または設定します。
    /// </summary>
    [JsonPropertyName("maxTurns")]
    public int MaxTurns { get; set; } = 20;
}

/// <summary>
/// エージェント出力仕様を表します。
/// </summary>
public sealed class OutputDefinition
{
    /// <summary>
    /// 出力フォーマットを取得または設定します。
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";

    /// <summary>
    /// 出力検証に利用するスキーマ名を取得または設定します。
    /// </summary>
    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }
}

/// <summary>
/// エージェント固有の推論パラメーターを表します。
/// </summary>
public sealed class AgentSettingsDefinition
{
    /// <summary>
    /// 温度パラメーターを取得または設定します。
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// 最大出力トークン数を取得または設定します。
    /// </summary>
    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}