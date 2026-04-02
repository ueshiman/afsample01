using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using SampleOpenAIApp.Clients;
using Tutorial01B.Clients;
using Tutorial01B.Services;
using Xunit;

namespace Tutorial01B.Tests;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task RunSampleAsync_CallsExecutorOnce_AndPassesFourMessages()
    {
        FakeExecutor executor = new();
        ChatService service = new(executor, NullLogger<ChatService>.Instance);

        await service.RunSampleAsync();

        Assert.Equal(1, executor.CallCount);
        Assert.NotNull(executor.MessagesPassed);
        Assert.Equal(4, executor.MessagesPassed!.Count);
        Assert.IsType<SystemChatMessage>(executor.MessagesPassed[0]);
        Assert.IsType<UserChatMessage>(executor.MessagesPassed[1]);
        Assert.IsType<AssistantChatMessage>(executor.MessagesPassed[2]);
        Assert.IsType<UserChatMessage>(executor.MessagesPassed[3]);
    }

    private sealed class FakeExecutor : IChatCompletionExecutor
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<ChatMessage>? MessagesPassed { get; private set; }

        public Task<ChatResult> CompleteAsync(
            IEnumerable<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            MessagesPassed = messages.ToList();

            return Task.FromResult(new ChatResult
            {
                Model = "test-model",
                Role = "Assistant",
                Text = "test response",
                FinishReason = "Stop"
            });
        }
    }
}