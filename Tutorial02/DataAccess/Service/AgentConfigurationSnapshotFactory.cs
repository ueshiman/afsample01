
namespace ConversationSuggestionService.Configuration;

/// <summary>
/// <see cref="AgentServiceDefinition"/> から高速参照用の
/// <see cref="AgentConfigurationSnapshot"/> を生成するファクトリ実装です。
/// </summary>
public class AgentConfigurationSnapshotFactory : IAgentConfigurationSnapshotFactory
{
    /// <summary>
    /// エージェント定義を、ID ベースの辞書と有効エージェントの優先度順リストへ正規化し、
    /// スナップショットとして返します。
    /// </summary>
    /// <param name="definition">プロバイダー、コールバック、エージェント定義を含む構成情報。</param>
    /// <returns>
    /// 元定義と、ID（大文字小文字を区別しない）で検索可能な辞書、
    /// および有効エージェントを優先度降順で並べた一覧を保持するスナップショット。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="definition"/> が <see langword="null"/> の場合。
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Providers / Callbacks / Agents のいずれかで ID が重複している場合。
    /// </exception>
    public AgentConfigurationSnapshot Create(AgentServiceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Dictionary<string, ProviderDefinition> providers = definition.Providers
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, CallbackDefinition> callbacks = definition.Callbacks
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, AgentGroupDefinition> agents = definition.Agents
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, List<AgentDefinition>> enabledAgentsOrdered = definition.Agents
            .Select(group => new KeyValuePair<string, List<AgentDefinition>>(group.Id, group.AgentGroup
                .Where(agent => agent.Enabled)
                .OrderByDescending(agent => agent.Priority)
                .ToList()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        return new AgentConfigurationSnapshot
        {
            Raw = definition,
            Providers = providers,
            Callbacks = callbacks,
            Agents = agents,
            EnabledAgentsOrdered = enabledAgentsOrdered
        };
    }
}