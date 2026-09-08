namespace ConversationSuggestionService.Configuration;

public interface IAgentConfigurationWatcher
{
    void Start();
    void Dispose();
}