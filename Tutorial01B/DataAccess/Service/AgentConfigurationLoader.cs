using ConversationSuggestionService.Configuration;
using System.Text.Json;

namespace Tutorial01B.DataAccess.Service;

public sealed class AgentConfigurationLoader : IAgentConfigurationLoader
{
    private readonly IAgentConfigurationSnapshotFactory _agentConfigurationSnapshotFactory;

    public AgentConfigurationLoader(IAgentConfigurationSnapshotFactory agentConfigurationSnapshotFactory)
    {
        _agentConfigurationSnapshotFactory = agentConfigurationSnapshotFactory;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<AgentConfigurationSnapshot> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string json = await File.ReadAllTextAsync(filePath, cancellationToken);

        AgentServiceDefinition definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, SerializerOptions)
                         ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

        AgentConfigurationValidator.Validate(definition);

        return _agentConfigurationSnapshotFactory.Create(definition);
    }

    public AgentConfigurationSnapshot Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = File.ReadAllText(filePath);

        var definition = JsonSerializer.Deserialize<AgentServiceDefinition>(json, SerializerOptions)
                         ?? throw new InvalidOperationException("設定ファイルを読み込めませんでした。");

        AgentConfigurationValidator.Validate(definition);

        return _agentConfigurationSnapshotFactory.Create(definition);
    }
}