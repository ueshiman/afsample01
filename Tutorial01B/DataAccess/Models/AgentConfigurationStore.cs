namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationStore
{
    private AgentConfigurationSnapshot? _current;

    public AgentConfigurationSnapshot Current
        => _current ?? throw new InvalidOperationException("設定はまだ読み込まれていません。");

    public void Set(AgentConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref _current, snapshot);
    }
}