using Microsoft.Extensions.Logging;
using Tutorial01B.DataAccess.Service;

namespace ConversationSuggestionService.Configuration;

public sealed class AgentConfigurationWatcher : IDisposable, IAgentConfigurationWatcher
{
    /// <summary>   
    /// ファイル変更イベントを受け取ってから再読み込み処理を開始するまでの待機時間 (ミリ秒)。
    /// 連続して発生する変更通知をまとめるためのデバウンス間隔として使用します。
    /// </summary>
    private const int DueTime = 500;

    private readonly IAgentConfigurationLoader _loader;
    private readonly IAgentConfigurationStore _store;
    private readonly ILogger<AgentConfigurationWatcher> _logger;
    private readonly IAgentConfigurationFile _file;
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _reloadLock = new();

    private Timer? _reloadTimer;
    private bool _disposed;

    public AgentConfigurationWatcher(IAgentConfigurationLoader loader, IAgentConfigurationStore store, ILogger<AgentConfigurationWatcher> logger, IAgentConfigurationFile file)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _loader = loader;
        _store = store;
        _logger = logger;
        _file = file;

        var directory = _file.Directory ?? throw new InvalidOperationException("設定ファイルのディレクトリを取得できません。");

        _watcher = new FileSystemWatcher(_file.Directory, _file.Name)
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
        AgentConfigurationSnapshot initial = _loader.Load(_file.Path);
        _store.Set(initial);

        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("設定ファイル監視を開始しました: {File.Path}", _file.Path);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        lock (_reloadLock)
        {
            _reloadTimer?.Dispose();
            // ファイル変更イベントを受け取ってから再読み込み処理を開始するまで待機
            // DueTime ミリ秒の間に連続して発生する変更通知をまとめるための緩衝間隔
            _reloadTimer = new Timer(_ => Reload(), null, DueTime, Timeout.Infinite);
            //Reload();
        }
    }

    private void Reload()
    {
        try
        {
            AgentConfigurationSnapshot snapshot = _loader.Load(_file.Directory);
            _store.Set(snapshot);

            _logger.LogInformation("設定ファイルを再読み込みしました: {File.Path}", _file.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ファイルの再読み込みに失敗しました: {File.Path}", _file.Path);
        }
        finally
        {
            lock (_reloadLock)
            {
                _reloadTimer?.Dispose();
                _reloadTimer = null;
            }
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