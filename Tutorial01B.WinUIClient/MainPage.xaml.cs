using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tutorial01B_WinUIClient;

public sealed partial class MainPage : Page
{
    private readonly HttpClient _httpClient = new();
    private readonly CallbackServer _callbackServer = new();
    private readonly ConcurrentDictionary<string, string> _pendingInputs = new();
    private readonly ConcurrentDictionary<string, RequestHistoryItem> _historyByRequestId = new();
    private readonly Dictionary<string, SourceTranscriptItem> _sourceTranscriptIndex = [];
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private AudioTranscriptionSession? _transcriptionSession;
    private ScrollViewer? _historyScrollViewer;
    private Guid? _sessionId;

    public ObservableCollection<RequestHistoryItem> HistoryItems { get; } = [];
    public ObservableCollection<AudioDeviceItem> MicrophoneDevices { get; } = [];
    public ObservableCollection<AudioDeviceItem> SpeakerDevices { get; } = [];
    public ObservableCollection<SourceTranscriptItem> SourceTranscripts { get; } = [];

    public MainPage()
    {
        InitializeComponent();
        SpeechKeyPasswordBox.Password = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY") ?? string.Empty;
        SpeechRegionTextBox.Text = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? string.Empty;
        Loaded += MainPage_Loaded;
        Unloaded += MainPage_Unloaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _callbackServer.StartAsync(HandleCallback);
            RefreshDevices();
            SetStatus("Callback listener は http://127.0.0.1:1305/callback で待機中。", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            SetStatus($"Callback listener の起動に失敗: {exception.Message}", InfoBarSeverity.Error);
            StartButton.IsEnabled = false;
        }
    }

    private async void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        await StopTranscriptionAsync();
        await _callbackServer.DisposeAsync();
        _httpClient.Dispose();
        _sendGate.Dispose();
    }

    private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_transcriptionSession is not null)
        {
            SetStatus("文字起こし中はデバイスを更新できません。", InfoBarSeverity.Warning);
            return;
        }

        RefreshDevices();
        SetStatus("オーディオデバイスを更新しました。", InfoBarSeverity.Success);
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedDevices = MicrophoneDevices
            .Concat(SpeakerDevices)
            .Where(device => device.IsSelected)
            .ToArray();

        if (selectedDevices.Length == 0)
        {
            SetStatus("マイクまたはスピーカーを1つ以上選択してください。", InfoBarSeverity.Warning);
            return;
        }

        var speechKey = SpeechKeyPasswordBox.Password.Trim();
        var speechRegion = SpeechRegionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(speechKey) || string.IsNullOrWhiteSpace(speechRegion))
        {
            SetStatus("Azure AI Speech key と region を設定してください。", InfoBarSeverity.Warning);
            return;
        }

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;

        try
        {
            _transcriptionSession = new AudioTranscriptionSession();
            _transcriptionSession.Recognizing += HandleRecognizing;
            _transcriptionSession.Recognized += HandleRecognized;
            _transcriptionSession.Error += HandleTranscriptionError;
            await _transcriptionSession.StartAsync(selectedDevices, speechKey, speechRegion, "ja-JP");
            SetStatus($"{selectedDevices.Length}デバイスから文字起こし中。", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            await StopTranscriptionAsync();
            SetStatus($"文字起こしの開始に失敗: {exception.Message}", InfoBarSeverity.Error);
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        await StopTranscriptionAsync();
        SetStatus("文字起こしを停止しました。", InfoBarSeverity.Informational);
    }

    private void HandleRecognizing(SourceTranscription transcription)
    {
        DispatcherQueue.TryEnqueue(() =>
            GetSourceTranscript(transcription).Text = transcription.Text);
    }

    private void HandleRecognized(SourceTranscription transcription)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            GetSourceTranscript(transcription).Text = string.Empty;
            _ = SendRecognizedTextAsync(transcription);
        });
    }

    private void HandleTranscriptionError(string message)
    {
        DispatcherQueue.TryEnqueue(() =>
            SetStatus($"文字起こしエラー: {message}", InfoBarSeverity.Error));
    }

    private async Task SendRecognizedTextAsync(SourceTranscription transcription)
    {
        await _sendGate.WaitAsync();
        try
        {
            if (!Uri.TryCreate(EndpointTextBox.Text.Trim(), UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                DispatcherQueue.TryEnqueue(() =>
                    SetStatus("API endpoint が有効な HTTP/HTTPS URL ではありません。", InfoBarSeverity.Error));
                return;
            }

            var requestId = Guid.NewGuid().ToString("N");
            var sourceLabel = GetSourceLabel(transcription);
            var input = $"【音声ソース: {sourceLabel}】{Environment.NewLine}{transcription.Text}";
            var request = new ExecuteOrchestratorRequest(
                input,
                _sessionId,
                CallbackUrlTextBox.Text,
                requestId);

            var history = new RequestHistoryItem(requestId, sourceLabel, transcription.Text);
            _historyByRequestId[requestId] = history;
            HistoryItems.Insert(0, history);
            ScrollHistoryToLatest();
            _pendingInputs[requestId] = input;

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(endpoint, request);
                var accepted = await response.Content.ReadFromJsonAsync<ExecuteAcceptedResponse>();

                if (!response.IsSuccessStatusCode)
                {
                    var error = accepted?.Status ?? response.ReasonPhrase ?? "Unknown error";
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
                }

                if (accepted is null)
                {
                    throw new InvalidOperationException("受付レスポンスを読み取れませんでした。");
                }

                _sessionId = accepted.SessionId;
                var httpStatusCode = (int)response.StatusCode;
                var httpStatusName = response.StatusCode;
                DispatcherQueue.TryEnqueue(() =>
                {
                    SessionIdTextBox.Text = _sessionId?.ToString() ?? string.Empty;
                    history.ApiResponse =
                        $"HTTP {httpStatusCode} {httpStatusName}, status={accepted.Status}, sessionId={accepted.SessionId}";
                    if (history.CallbackResponse == "未受信")
                    {
                        history.State = "callback待機中";
                    }
                    SetStatus(
                        $"HTTP {httpStatusCode} / {accepted.Status}。callback 待機中: {requestId}",
                        InfoBarSeverity.Success);
                });
            }
            catch (Exception exception)
            {
                _pendingInputs.TryRemove(requestId, out _);
                DispatcherQueue.TryEnqueue(() =>
                {
                    history.ApiResponse = exception.Message;
                    history.State = "送信失敗";
                    SetStatus($"送信に失敗: {exception.Message}", InfoBarSeverity.Error);
                });
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private bool HandleCallback(ExecuteCallbackPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.RequestId)
            || !_pendingInputs.TryRemove(payload.RequestId, out _))
        {
            return false;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            var content = ExtractContent(payload.Result);
            if (_historyByRequestId.TryRemove(payload.RequestId, out var history))
            {
                history.CallbackResponse = !string.IsNullOrWhiteSpace(payload.Error)
                    ? $"エラー: {payload.Error}"
                    : string.IsNullOrWhiteSpace(content) ? "<回答なし>" : content;
                history.State = "完了";
            }
            SetStatus($"Callback 受信完了: {payload.RequestId}", InfoBarSeverity.Success);
        });

        return true;
    }

    private void RefreshDevices()
    {
        ReplaceItems(MicrophoneDevices, AudioDeviceService.GetMicrophones());
        ReplaceItems(SpeakerDevices, AudioDeviceService.GetSpeakers());
    }

    private async Task StopTranscriptionAsync()
    {
        var session = _transcriptionSession;
        _transcriptionSession = null;

        if (session is not null)
        {
            session.Recognizing -= HandleRecognizing;
            session.Recognized -= HandleRecognized;
            session.Error -= HandleTranscriptionError;
            await session.DisposeAsync();
        }

        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        SourceTranscripts.Clear();
        _sourceTranscriptIndex.Clear();
    }

    private static void ReplaceItems(
        ObservableCollection<AudioDeviceItem> target,
        IReadOnlyList<AudioDeviceItem> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private SourceTranscriptItem GetSourceTranscript(SourceTranscription transcription)
    {
        if (_sourceTranscriptIndex.TryGetValue(transcription.SourceId, out var item))
        {
            return item;
        }

        item = new SourceTranscriptItem
        {
            SourceLabel = GetSourceLabel(transcription)
        };
        _sourceTranscriptIndex[transcription.SourceId] = item;
        SourceTranscripts.Add(item);
        return item;
    }

    private static string GetSourceLabel(SourceTranscription transcription)
    {
        var kind = transcription.SourceKind == AudioDeviceKind.Microphone
            ? "マイク"
            : "スピーカー";
        return $"{kind}: {transcription.SourceName}";
    }

    private void SetStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
    }

    private void ScrollHistoryToLatest()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            _historyScrollViewer ??= FindDescendant<ScrollViewer>(HistoryListView);
            _historyScrollViewer?.ChangeView(
                horizontalOffset: null,
                verticalOffset: 0,
                zoomFactor: null,
                disableAnimation: false);
        });
    }

    private static T? FindDescendant<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindDescendant<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static string ExtractContent(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
        {
            return result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? string.Empty
                : result.ToString();
        }

        return string.Join(
            Environment.NewLine,
            results.EnumerateArray()
                .Select(item => item.TryGetProperty("content", out var content) ? content.GetString() : null)
                .Where(content => !string.IsNullOrWhiteSpace(content)));
    }
}

public sealed class RequestHistoryItem : INotifyPropertyChanged
{
    private string _state = "送信中";
    private string _apiResponse = "未受信";
    private string _callbackResponse = "未受信";

    public RequestHistoryItem(string requestId, string sourceLabel, string input)
    {
        RequestId = requestId;
        SourceLabel = sourceLabel;
        Input = input;
    }

    public string RequestId { get; }

    public string SourceLabel { get; }

    public string Input { get; }

    public string State
    {
        get => _state;
        set
        {
            if (SetField(ref _state, value))
            {
                NotifySummaryChanged();
            }
        }
    }

    public string ApiResponse
    {
        get => _apiResponse;
        set
        {
            if (SetField(ref _apiResponse, value))
            {
                NotifySummaryChanged();
            }
        }
    }

    public string CallbackResponse
    {
        get => _callbackResponse;
        set
        {
            if (SetField(ref _callbackResponse, value))
            {
                NotifySummaryChanged();
            }
        }
    }

    public string Title => $"{State}: {RequestId}";

    public string Detail =>
        $"ソース: {SourceLabel}{Environment.NewLine}" +
        $"送信: {Input}{Environment.NewLine}" +
        $"API応答: {ApiResponse}{Environment.NewLine}" +
        $"Callback: {CallbackResponse}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField(ref string field, string value)
    {
        if (field == value)
        {
            return false;
        }

        field = value;
        return true;
    }

    private void NotifySummaryChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
    }
}

public sealed class SourceTranscriptItem : INotifyPropertyChanged
{
    private string _text = string.Empty;

    public string SourceLabel { get; set; } = string.Empty;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
