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
            ValidateRequired(agent.Name, $"agents[{agent.Id}].name");
            ValidateRequired(agent.Type, $"agents[{agent.Id}].type");
            ValidateRequired(agent.ProviderRef, $"agents[{agent.Id}].providerRef");
            ValidateRequired(agent.Deployment, $"agents[{agent.Id}].deployment");
            ValidateRequired(agent.CallbackRef, $"agents[{agent.Id}].callbackRef");
            ValidateRequired(agent.Prompt.System, $"agents[{agent.Id}].prompt.system");

            if (!providerIds.Contains(agent.ProviderRef))
            {
                throw new InvalidOperationException(
                    $"agents[{agent.Id}].providerRef '{agent.ProviderRef}' に対応する provider が存在しません。");
            }

            if (!callbackIds.Contains(agent.CallbackRef))
            {
                throw new InvalidOperationException(
                    $"agents[{agent.Id}].callbackRef '{agent.CallbackRef}' に対応する callback が存在しません。");
            }

            if (agent.TimeoutSeconds is <= 0)
            {
                throw new InvalidOperationException(
                    $"agents[{agent.Id}].timeoutSeconds は 1 以上である必要があります。");
            }

            if (agent.Input.MaxTurns <= 0)
            {
                throw new InvalidOperationException(
                    $"agents[{agent.Id}].input.maxTurns は 1 以上である必要があります。");
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