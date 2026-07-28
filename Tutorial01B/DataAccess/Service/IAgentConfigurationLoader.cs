namespace ConversationSuggestionService.Configuration;

public interface IAgentConfigurationLoader
{
    Task<AgentConfigurationSnapshot> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    AgentConfigurationSnapshot Load(string filePath);
}