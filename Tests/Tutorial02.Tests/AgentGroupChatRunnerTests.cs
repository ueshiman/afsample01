using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Tutorial02.Models;
using Tutorial02.Services;

namespace Tutorial02.Tests;

public sealed class AgentGroupChatRunnerTests
{
    [Fact]
    public async Task RunAsync_ExecutesThreeGroupsInPriorityOrder_AndStopsOnComplete()
    {
        List<string> executionOrder = [];
        RecordingChatClient firstAgent = new("agent-first", executionOrder, "first candidate");
        RecordingChatClient secondAgent = new("agent-second", executionOrder, "second candidate");
        RecordingChatClient coordinator = new(
            "coordinator",
            executionOrder,
            """{"decision":"continue","reason":"needs review","nextInstruction":"remove unsupported claim"}""");
        RecordingChatClient judge = new(
            "judge",
            executionOrder,
            """{"decision":"complete","reason":"ready","nextInstruction":""}""");
        RecordingChatClient summary = new("summary", executionOrder, "final answer");

        AgentGroupModel group = new()
        {
            Id = "conversation-monitor",
            Input = new AgentInputModel { MaxTurns = 20 },
            AgentGroup =
            [
                CreateAgent("agent-second", 200, secondAgent),
                CreateAgent("agent-first", 100, firstAgent)
            ],
            Coordinators =
            [
                CreateAgent("judge", 200, judge),
                CreateAgent("coordinator", 100, coordinator)
            ],
            SummaryGroup = [CreateAgent("summary", 100, summary)]
        };

        AgentGroupChatRunner runner = new(NullLogger<AgentGroupChatRunner>.Instance);
        AgentGroupChatResult result = await runner.RunAsync(
            group,
            [new ChatMessage(ChatRole.User, "original conversation")],
            maxRounds: 5);

        Assert.Equal(
            ["agent-first", "agent-second", "coordinator", "judge", "summary"],
            executionOrder);
        Assert.Contains(
            secondAgent.Requests.Single(),
            message => message.Text?.Contains("first candidate", StringComparison.Ordinal) == true);

        IReadOnlyList<ChatMessage> summaryRequest = summary.Requests.Single();
        Assert.Equal(1, summaryRequest.Count(message => message.Text == "original conversation"));
        Assert.Contains(summaryRequest, message => message.Text?.Contains("first candidate", StringComparison.Ordinal) == true);
        Assert.Contains(summaryRequest, message => message.Text?.Contains("second candidate", StringComparison.Ordinal) == true);

        AgentChatResult finalResult = Assert.Single(result.Results);
        Assert.Equal("summary", finalResult.AgentId);
        Assert.Equal("final answer", finalResult.Content);
    }

    [Fact]
    public async Task RunAsync_PassesCoordinatorInstructionToNextRound_AndSummarizesFinalCandidatesOnce()
    {
        List<string> executionOrder = [];
        RecordingChatClient agent = new("agent", executionOrder, "candidate one", "candidate two");
        RecordingChatClient coordinator = new(
            "coordinator",
            executionOrder,
            """{"decision":"continue","reason":"unsupported","nextInstruction":"revise candidate"}""",
            """{"decision":"continue","reason":"still reviewing","nextInstruction":"keep concise"}""");
        RecordingChatClient summary = new("summary", executionOrder, "final answer");

        AgentGroupModel group = new()
        {
            Id = "conversation-monitor",
            Input = new AgentInputModel { MaxTurns = 20 },
            AgentGroup = [CreateAgent("agent", 100, agent)],
            Coordinators = [CreateAgent("coordinator", 100, coordinator)],
            SummaryGroup = [CreateAgent("summary", 100, summary)]
        };

        AgentGroupChatRunner runner = new(NullLogger<AgentGroupChatRunner>.Instance);
        AgentGroupChatResult result = await runner.RunAsync(
            group,
            [new ChatMessage(ChatRole.User, "original conversation")],
            maxRounds: 2);

        Assert.Equal(["agent", "coordinator", "agent", "coordinator", "summary"], executionOrder);
        Assert.Contains(
            agent.Requests[1],
            message => message.Text?.Contains("revise candidate", StringComparison.Ordinal) == true);

        IReadOnlyList<ChatMessage> summaryRequest = summary.Requests.Single();
        Assert.Contains(summaryRequest, message => message.Text?.Contains("candidate two", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(summaryRequest, message => message.Text?.Contains("candidate one", StringComparison.Ordinal) == true);
        Assert.Single(result.Results);
    }

    [Fact]
    public async Task RunAsync_SkipsCoordinatorsAndSummary_WhenAgentsProduceNoCandidates()
    {
        List<string> executionOrder = [];
        RecordingChatClient agent = new("agent", executionOrder, string.Empty);
        RecordingChatClient coordinator = new("coordinator", executionOrder, "unused");
        RecordingChatClient summary = new("summary", executionOrder, "unused");

        AgentGroupModel group = new()
        {
            Id = "conversation-monitor",
            Input = new AgentInputModel { MaxTurns = 20 },
            AgentGroup = [CreateAgent("agent", 100, agent)],
            Coordinators = [CreateAgent("coordinator", 100, coordinator)],
            SummaryGroup = [CreateAgent("summary", 100, summary)]
        };

        AgentGroupChatRunner runner = new(NullLogger<AgentGroupChatRunner>.Instance);
        AgentGroupChatResult result = await runner.RunAsync(
            group,
            [new ChatMessage(ChatRole.User, "original conversation")]);

        Assert.Equal(["agent"], executionOrder);
        Assert.Empty(coordinator.Requests);
        Assert.Empty(summary.Requests);
        Assert.Empty(result.Results);
    }

    private static AgentModel CreateAgent(string id, int priority, IChatClient chatClient)
    {
        return new AgentModel
        {
            Id = id,
            Name = id,
            Enabled = true,
            Priority = priority,
            Prompt = new PromptModel { System = $"{id} instructions" },
            Input = new InputModel { MaxTurns = 20 },
            Settings = new AgentSettingsModel(),
            ChatClient = chatClient
        };
    }

    private sealed class RecordingChatClient : IChatClient
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;
        private readonly Queue<string> _responses;

        public RecordingChatClient(string name, List<string> executionOrder, params string[] responses)
        {
            _name = name;
            _executionOrder = executionOrder;
            _responses = new Queue<string>(responses);
        }

        public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _executionOrder.Add(_name);
            Requests.Add(messages.Select(message => message.Clone()).ToList());

            string response = _responses.Dequeue();
            return Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
