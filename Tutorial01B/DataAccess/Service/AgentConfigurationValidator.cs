namespace ConversationSuggestionService.Configuration;

public static class AgentConfigurationValidator
{
    public static void Validate(AgentServiceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidateRequired(definition.Service.Name, "service.name");

        ValidateUniqueIds(
            definition.Providers.Select(x => x.Id),
            "providers[].id");

        ValidateUniqueIds(
            definition.Callbacks.Select(x => x.Id),
            "callbacks[].id");

        ValidateUniqueIds(
            definition.Agents.Select(x => x.Id),
            "agents[].id");

        foreach (var provider in definition.Providers)
        {
            ValidateRequired(provider.Id, "providers[].id");
            ValidateRequired(provider.Type, $"providers[{provider.Id}].type");
            ValidateRequired(provider.Endpoint, $"providers[{provider.Id}].endpoint");
            ValidateRequired(provider.Authentication.Type, $"providers[{provider.Id}].authentication.type");
        }

        var providerIds = new HashSet<string>(
            definition.Providers.Select(x => x.Id),
            StringComparer.OrdinalIgnoreCase);

        var callbackIds = new HashSet<string>(
            definition.Callbacks.Select(x => x.Id),
            StringComparer.OrdinalIgnoreCase);

        foreach (var callback in definition.Callbacks)
        {
            ValidateRequired(callback.Id, "callbacks[].id");
            ValidateRequired(callback.Type, $"callbacks[{callback.Id}].type");
            ValidateRequired(callback.Url, $"callbacks[{callback.Id}].url");
        }

        foreach (var agent in definition.Agents)
        {
            ValidateRequired(agent.Id, "agents[].id");
            ValidateRequired(agent.Input.Source, $"agents[{agent.Id}].input.source");
            ValidateRequired(agent.Input.Format, $"agents[{agent.Id}].input.format");

            if (agent.Input.MaxTurns <= 0)
            {
                throw new InvalidOperationException($"agents[{agent.Id}].input.maxTurns は 1 以上である必要があります。");
            }

            ValidateUniqueIds(agent.AgentGroup.Select(x => x.Id), $"agents[{agent.Id}].agentsGroup[].id");
            ValidateUniqueIds(agent.Coordinators.Select(x => x.Id), $"agents[{agent.Id}].coordinators[].id");
            ValidateUniqueIds(agent.SummaryGroup.Select(x => x.Id), $"agents[{agent.Id}].summaryGroup[].id");

            foreach (var member in agent.AgentGroup)
            {
                ValidateAgentDefinition(member, $"agents[{agent.Id}].agentsGroup", providerIds, callbackIds);
            }

            foreach (var coordinator in agent.Coordinators)
            {
                ValidateAgentDefinition(coordinator, $"agents[{agent.Id}].coordinators", providerIds, callbackIds);
            }

            foreach (var summary in agent.SummaryGroup)
            {
                ValidateAgentDefinition(summary, $"agents[{agent.Id}].summaryGroup", providerIds, callbackIds);
            }
        }

        if (definition.Service.DefaultTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("service.defaultTimeoutSeconds は 1 以上である必要があります。");
        }

        if (definition.Execution.MaxDegreeOfParallelism <= 0)
        {
            throw new InvalidOperationException("execution.maxDegreeOfParallelism は 1 以上である必要があります。");
        }
    }

    private static void ValidateAgentDefinition(AgentDefinition agent, string pathPrefix, HashSet<string> providerIds, HashSet<string> callbackIds)
    {
        ValidateRequired(agent.Id, $"{pathPrefix}[].id");
        ValidateRequired(agent.Name, $"{pathPrefix}[{agent.Id}].name");
        ValidateRequired(agent.Type, $"{pathPrefix}[{agent.Id}].type");
        ValidateRequired(agent.ProviderRef, $"{pathPrefix}[{agent.Id}].providerRef");
        ValidateRequired(agent.Deployment, $"{pathPrefix}[{agent.Id}].deployment");

        if (!string.IsNullOrWhiteSpace(agent.CallbackRef))
        {
            if (!callbackIds.Contains(agent.CallbackRef))
            {
                throw new InvalidOperationException(
                    $"{pathPrefix}[{agent.Id}].callbackRef '{agent.CallbackRef}' に対応する callback が存在しません。");
            }
        }

        if (!providerIds.Contains(agent.ProviderRef))
        {
            throw new InvalidOperationException(
                $"{pathPrefix}[{agent.Id}].providerRef '{agent.ProviderRef}' に対応する provider が存在しません。");
        }

        ValidateRequired(agent.Prompt.System, $"{pathPrefix}[{agent.Id}].prompt.system");
    }

    private static void ValidateRequired(string? value, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{path} は必須です。");
        }
    }

    private static void ValidateUniqueIds(IEnumerable<string> ids, string path)
    {
        var duplicates = ids
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"{path} に重複があります: {string.Join(", ", duplicates)}");
        }
    }
}