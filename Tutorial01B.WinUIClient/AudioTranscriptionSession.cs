using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Tutorial01B_WinUIClient;

public sealed record SourceTranscription(
    string SourceId,
    string SourceName,
    AudioDeviceKind SourceKind,
    string Text);

public sealed class AudioTranscriptionSession : IAsyncDisposable
{
    private readonly List<AudioSourceTranscriber> _transcribers = [];

    public event Action<SourceTranscription>? Recognizing;
    public event Action<SourceTranscription>? Recognized;
    public event Action<string>? Error;

    public async Task StartAsync(
        IReadOnlyCollection<AudioDeviceItem> selectedDevices,
        string speechKey,
        string speechRegion,
        string recognitionLanguage,
        CancellationToken cancellationToken = default)
    {
        if (_transcribers.Count > 0)
        {
            throw new InvalidOperationException("文字起こしは既に開始されています。");
        }

        if (selectedDevices.Count == 0)
        {
            throw new ArgumentException("オーディオデバイスを1つ以上選択してください。", nameof(selectedDevices));
        }

        try
        {
            foreach (var device in selectedDevices)
            {
                var transcriber = new AudioSourceTranscriber(
                    device,
                    speechKey,
                    speechRegion,
                    recognitionLanguage);
                transcriber.Recognizing += transcription => Recognizing?.Invoke(transcription);
                transcriber.Recognized += transcription => Recognized?.Invoke(transcription);
                transcriber.Error += message => Error?.Invoke(message);
                _transcribers.Add(transcriber);

                await transcriber.StartAsync(cancellationToken);
            }
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var transcriber in _transcribers)
        {
            await transcriber.DisposeAsync();
        }

        _transcribers.Clear();
    }

    private sealed class AudioSourceTranscriber : IAsyncDisposable
    {
        private const int SampleRate = 16000;
        private const int BitsPerSample = 16;
        private const int ChannelCount = 1;

        private readonly AudioDeviceItem _source;
        private readonly SpeechRecognizer _recognizer;
        private readonly PushAudioInputStream _speechStream;
        private readonly AudioConfig _audioConfig;
        private readonly MMDevice _device;
        private readonly WasapiCapture _capture;
        private readonly ISampleProvider _sampleProvider;
        private CancellationTokenSource? _pumpCancellation;
        private Task? _pumpTask;

        public AudioSourceTranscriber(
            AudioDeviceItem source,
            string speechKey,
            string speechRegion,
            string recognitionLanguage)
        {
            _source = source;

            using var enumerator = new MMDeviceEnumerator();
            _device = enumerator.GetDevice(source.Id);
            _capture = source.Kind switch
            {
                AudioDeviceKind.Microphone => new WasapiCapture(_device),
                AudioDeviceKind.SpeakerLoopback => new WasapiLoopbackCapture(_device),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };

            var buffer = new BufferedWaveProvider(_capture.WaveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            _capture.DataAvailable += (_, args) =>
                buffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
            _capture.RecordingStopped += (_, args) =>
            {
                if (args.Exception is not null)
                {
                    Error?.Invoke(FormatError(args.Exception.Message));
                }
            };

            ISampleProvider sampleProvider = new MonoSampleProvider(buffer.ToSampleProvider());
            if (sampleProvider.WaveFormat.SampleRate != SampleRate)
            {
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, SampleRate);
            }
            _sampleProvider = sampleProvider;

            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = recognitionLanguage;

            var streamFormat = AudioStreamFormat.GetWaveFormatPCM(SampleRate, BitsPerSample, ChannelCount);
            _speechStream = AudioInputStream.CreatePushStream(streamFormat);
            _audioConfig = AudioConfig.FromStreamInput(_speechStream);
            _recognizer = new SpeechRecognizer(speechConfig, _audioConfig);
            _recognizer.Recognizing += OnRecognizing;
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;
        }

        public event Action<SourceTranscription>? Recognizing;
        public event Action<SourceTranscription>? Recognized;
        public event Action<string>? Error;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _pumpCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pumpTask = Task.Run(
                () => PumpAudioAsync(_pumpCancellation.Token),
                CancellationToken.None);

            _capture.StartRecording();
            await _recognizer.StartContinuousRecognitionAsync();
        }

        private async Task PumpAudioAsync(CancellationToken cancellationToken)
        {
            var samples = new float[SampleRate / 10];
            var pcm = new byte[samples.Length * sizeof(short)];

            while (!cancellationToken.IsCancellationRequested)
            {
                var sampleCount = _sampleProvider.Read(samples, 0, samples.Length);
                for (var index = 0; index < sampleCount; index++)
                {
                    var value = (short)(Math.Clamp(samples[index], -1f, 1f) * short.MaxValue);
                    pcm[index * 2] = (byte)(value & 0xff);
                    pcm[(index * 2) + 1] = (byte)((value >> 8) & 0xff);
                }

                _speechStream.Write(pcm[..(sampleCount * sizeof(short))]);
                await Task.Delay(100, cancellationToken);
            }
        }

        private void OnRecognizing(object? sender, SpeechRecognitionEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Result.Text))
            {
                Recognizing?.Invoke(CreateTranscription(args.Result.Text));
            }
        }

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs args)
        {
            if (args.Result.Reason == ResultReason.RecognizedSpeech
                && !string.IsNullOrWhiteSpace(args.Result.Text))
            {
                Recognized?.Invoke(CreateTranscription(args.Result.Text));
            }
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs args)
        {
            Error?.Invoke(FormatError($"{args.Reason}: {args.ErrorDetails}"));
        }

        private SourceTranscription CreateTranscription(string text)
        {
            return new SourceTranscription(_source.Id, _source.Name, _source.Kind, text);
        }

        private string FormatError(string message)
        {
            return $"{_source.Name}: {message}";
        }

        public async ValueTask DisposeAsync()
        {
            await _recognizer.StopContinuousRecognitionAsync();
            _capture.StopRecording();

            if (_pumpCancellation is not null)
            {
                await _pumpCancellation.CancelAsync();
            }

            if (_pumpTask is not null)
            {
                try
                {
                    await _pumpTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _recognizer.Recognizing -= OnRecognizing;
            _recognizer.Recognized -= OnRecognized;
            _recognizer.Canceled -= OnCanceled;
            _recognizer.Dispose();
            _capture.Dispose();
            _device.Dispose();
            _audioConfig.Dispose();
            _speechStream.Dispose();
            _pumpCancellation?.Dispose();
        }
    }

    private sealed class MonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _sourceBuffer;

        public MonoSampleProvider(ISampleProvider source)
        {
            _source = source;
            _sourceBuffer = new float[4096 * source.WaveFormat.Channels];
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var channels = _source.WaveFormat.Channels;
            var sourceSampleCount = Math.Min(count * channels, _sourceBuffer.Length);
            sourceSampleCount -= sourceSampleCount % channels;

            var samplesRead = _source.Read(_sourceBuffer, 0, sourceSampleCount);
            var framesRead = samplesRead / channels;

            for (var frame = 0; frame < framesRead; frame++)
            {
                var sum = 0f;
                for (var channel = 0; channel < channels; channel++)
                {
                    sum += _sourceBuffer[(frame * channels) + channel];
                }

                buffer[offset + frame] = sum / channels;
            }

            return framesRead;
        }
    }
}
