namespace SampleOpenAIApp.Clients;

public sealed class ChatResult
{
    public string Model { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public string FinishReason { get; init; } = string.Empty;
}
