using System.Text.Json.Serialization;

namespace ConversationSuggestionService.Configuration;

public sealed class AgentServiceDefinition
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1";

    [JsonPropertyName("service")]
    public ServiceDefinition Service { get; set; } = new();

    [JsonPropertyName("providers")]
    public List<ProviderDefinition> Providers { get; set; } = new();

    [JsonPropertyName("callbacks")]
    public List<CallbackDefinition> Callbacks { get; set; } = new();

    [JsonPropertyName("execution")]
    public ExecutionDefinition Execution { get; set; } = new();

    [JsonPropertyName("agents")]
    public List<AgentDefinition> Agents { get; set; } = new();
}

public sealed class ServiceDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("defaultLocale")]
    public string DefaultLocale { get; set; } = "ja-JP";

    [JsonPropertyName("defaultTimeoutSeconds")]
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

public sealed class ProviderDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("authentication")]
    public AuthenticationDefinition Authentication { get; set; } = new();

    [JsonPropertyName("defaults")]
    public ProviderDefaultsDefinition Defaults { get; set; } = new();

    [JsonPropertyName("logging")]
    public string? Logging { get; set; }
}

public sealed class AuthenticationDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("apiKeyEnvVar")]
    public string? ApiKeyEnvVar { get; set; }
}

public sealed class ProviderDefaultsDefinition
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}

public sealed class CallbackDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("includeConversation")]
    public bool IncludeConversation { get; set; }

    [JsonPropertyName("includeAgentMetadata")]
    public bool IncludeAgentMetadata { get; set; }
}

public sealed class ExecutionDefinition
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "parallel";

    [JsonPropertyName("returnMode")]
    public string ReturnMode { get; set; } = "perAgent";

    [JsonPropertyName("maxDegreeOfParallelism")]
    public int MaxDegreeOfParallelism { get; set; } = 3;
}

public sealed class AgentDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("providerRef")]
    public string ProviderRef { get; set; } = string.Empty;

    [JsonPropertyName("deployment")]
    public string Deployment { get; set; } = string.Empty;

    [JsonPropertyName("callbackRef")]
    public string CallbackRef { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }

    [JsonPropertyName("prompt")]
    public PromptDefinition Prompt { get; set; } = new();

    [JsonPropertyName("input")]
    public InputDefinition Input { get; set; } = new();

    [JsonPropertyName("output")]
    public OutputDefinition Output { get; set; } = new();

    [JsonPropertyName("settings")]
    public AgentSettingsDefinition Settings { get; set; } = new();
}

public sealed class PromptDefinition
{
    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;
}

public sealed class InputDefinition
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "conversation";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "plainText";

    [JsonPropertyName("maxTurns")]
    public int MaxTurns { get; set; } = 20;
}

public sealed class OutputDefinition
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";

    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }
}

public sealed class AgentSettingsDefinition
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}