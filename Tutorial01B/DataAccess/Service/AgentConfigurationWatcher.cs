using Microsoft.Extensions.Logging;

namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationWatcher : IDisposable
{
    private readonly string _filePath;
    private readonly AgentConfigurationLoader _loader;
    private readonly AgentConfigurationStore _store;
    private readonly ILogger<AgentConfigurationWatcher> _logger;
    private readonly FileSystemWatcher _watcher;
    private readonly object _reloadLock = new();

    private Timer? _reloadTimer;
    private bool _disposed;

    public AgentConfigurationWatcher(
        string filePath,
        AgentConfigurationLoader loader,
        AgentConfigurationStore store,
        ILogger<AgentConfigurationWatcher> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _filePath = Path.GetFullPath(filePath);
        _loader = loader;
        _store = store;
        _logger = logger;

        var directory = Path.GetDirectoryName(_filePath)
                        ?? throw new InvalidOperationException("設定ファイルのディレクトリを取得できません。");

        var fileName = Path.GetFileName(_filePath);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite
                         | NotifyFilters.Size
                         | NotifyFilters.FileName
                         | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    public void Start()
    {
        var initial = _loader.Load(_filePath);
        _store.Set(initial);

        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("設定ファイル監視を開始しました: {FilePath}", _filePath);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        lock (_reloadLock)
        {
            _reloadTimer?.Dispose();
            _reloadTimer = new Timer(_ => Reload(), null, 500, Timeout.Infinite);
        }
    }

    private void Reload()
    {
        try
        {
            var snapshot = _loader.Load(_filePath);
            _store.Set(snapshot);

            _logger.LogInformation("設定ファイルを再読み込みしました: {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ファイルの再読み込みに失敗しました: {FilePath}", _filePath);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _watcher.Dispose();
        _reloadTimer?.Dispose();
    }
}