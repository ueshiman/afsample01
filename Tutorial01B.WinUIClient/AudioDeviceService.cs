using NAudio.CoreAudioApi;

namespace Tutorial01B_WinUIClient;

public enum AudioDeviceKind
{
    Microphone,
    SpeakerLoopback
}

public sealed class AudioDeviceItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AudioDeviceKind Kind { get; set; }

    public bool IsSelected { get; set; }
}

public static class AudioDeviceService
{
    public static IReadOnlyList<AudioDeviceItem> GetMicrophones()
    {
        return GetDevices(DataFlow.Capture, AudioDeviceKind.Microphone);
    }

    public static IReadOnlyList<AudioDeviceItem> GetSpeakers()
    {
        return GetDevices(DataFlow.Render, AudioDeviceKind.SpeakerLoopback);
    }

    private static IReadOnlyList<AudioDeviceItem> GetDevices(
        DataFlow dataFlow,
        AudioDeviceKind kind)
    {
        using var enumerator = new MMDeviceEnumerator();

        return enumerator
            .EnumerateAudioEndPoints(dataFlow, DeviceState.Active)
            .Select(device => new AudioDeviceItem
            {
                Id = device.ID,
                Name = device.FriendlyName,
                Kind = kind
            })
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
