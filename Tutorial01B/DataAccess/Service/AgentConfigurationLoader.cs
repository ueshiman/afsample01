using System.Text.Json;

namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<AgentConfigurationSnapshot> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);

        var definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, SerializerOptions)
                         ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

        AgentConfigurationValidator.Validate(definition);

        return AgentConfigurationSnapshotFactory.Create(definition);
    }

    public AgentConfigurationSnapshot Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = File.ReadAllText(filePath);

        var definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, SerializerOptions)
                         ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

        AgentConfigurationValidator.Validate(definition);

        return AgentConfigurationSnapshotFactory.Create(definition);
    }
}