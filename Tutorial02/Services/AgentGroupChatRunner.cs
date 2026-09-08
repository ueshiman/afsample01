using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Tutorial02.Models;

namespace Tutorial02.Services
{
    public sealed class AgentGroupChatRunner : IAgentGroupChatRunner
    {
        private readonly ILogger<AgentGroupChatRunner> _logger;

        public AgentGroupChatRunner(ILogger<AgentGroupChatRunner> logger)
        {
            _logger = logger;
        }

        public async Task<AgentGroupChatResult> RunAsync(AgentGroupModel group, List<ChatMessage> message, int maxRounds = 1, int defaultTimeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (maxRounds <= 0) throw new ArgumentOutOfRangeException(nameof(maxRounds), "最大ラウンド数は1以上にしてください。");

            List<AgentModel> enabledAgents = group.AgentGroup.Where(agent => agent.Enabled).OrderBy(agent => agent.Priority).ToList();
            List<AgentModel> enabledCoordinators = group.Coordinators.Where(agent => agent.Enabled).OrderBy(agent => agent.Priority).ToList();
            List<AgentModel> enabledSummaryGroup = group.SummaryGroup.Where(agent => agent.Enabled).OrderBy(agent => agent.Priority).ToList();

            if (enabledAgents.Count == 0 && enabledCoordinators.Count == 0 && enabledSummaryGroup.Count == 0)
            {
                return new AgentGroupChatResult(group.Id, []);
            }

            // 実行前にChatClientの設定状態を検証
            ValidateChatClients(enabledAgents.Concat(enabledCoordinators).Concat(enabledSummaryGroup));
            List<ChatMessage> sourceHistory = new(message);
            List<ChatMessage> sharedHistory = new(message);
            List<AgentChatResult> summary = new();
            List<ChatMessage> finalCandidates = [];
            int finalRound = 0;

            for (int round = 1; round <= maxRounds; round++)
            {
                finalRound = round;
                List<ChatMessage> resultSummary = new();

                foreach (AgentModel agent in enabledAgents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // AgentModelにキャッシュされているChatClientを利用
                    IChatClient chatClient = agent.ChatClient!;

                    int maxTurns = agent.Input.MaxTurns > 0 ? agent.Input.MaxTurns : group.Input.MaxTurns;
                    List<ChatMessage> requestMessages =
                    [
                        new ChatMessage(ChatRole.System, agent.Prompt.System),.. GetRecentHistory(sharedHistory, maxTurns)
                    ];

                    ChatOptions options = CreateOptions(agent);

                    int timeoutSeconds = agent.TimeoutSeconds ?? defaultTimeoutSeconds;

                    using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                    ChatResponse completion;

                    try
                    {
                        completion = await chatClient.GetResponseAsync(requestMessages, options, timeoutCts.Token);
                    }
                    catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation(canceledException, "エージェント '{AgentName}' の実行がタイムアウトしました。", agent.Name);
                        continue;
                    }

                    string responseText = completion.Text;

                    Console.WriteLine($"エージェント '{agent.Name}' の応答: {responseText}");

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        _logger.LogInformation("エージェント '{AgentName}' からテキスト応答が返されませんでした。", agent.Name);
                    }
                    else
                    {
                        // 後続エージェントへ回答を引き継ぐ
                        sharedHistory.Add(new ChatMessage(ChatRole.Assistant, $"【発言者: {agent.Name}】\n{responseText}"));
                        resultSummary.Add(new ChatMessage(ChatRole.Assistant, $"【候補情報: {agent.Name}】\n{responseText}"));
                    }
                }

                bool isComplete = false;

                // エージェント結果がある場合のみ、後段の coordinator を実行
                if (resultSummary.Count > 0)
                {
                    foreach (AgentModel coordinator in enabledCoordinators)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        IChatClient chatClient = coordinator.ChatClient!;
                        int maxTurns = coordinator.Input.MaxTurns > 0 ? coordinator.Input.MaxTurns : group.Input.MaxTurns;

                        List<ChatMessage> requestMessages =
                        [
                            new ChatMessage(ChatRole.System, coordinator.Prompt.System),
                        .. GetRecentHistory(sharedHistory, maxTurns)
                        ];

                        ChatOptions options = CreateOptions(coordinator);
                        int timeoutSeconds = coordinator.TimeoutSeconds ?? defaultTimeoutSeconds;

                        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                        ChatResponse completion;
                        try
                        {
                            completion = await chatClient.GetResponseAsync(requestMessages, options, timeoutCts.Token);
                        }
                        catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation(canceledException, "コーディネーター '{AgentName}' の実行がタイムアウトしました。", coordinator.Name);
                            continue;
                        }

                        string responseText = completion.Text;
                        Console.WriteLine($"コーディネーター '{coordinator.Name}' の応答: {responseText}");

                        if (string.IsNullOrWhiteSpace(responseText))
                        {
                            _logger.LogInformation("コーディネーター '{AgentName}' からテキスト応答が返されませんでした。", coordinator.Name);
                            continue;
                        }

                        string handoffText = BuildCoordinatorHandoffMessage(responseText, out isComplete);
                        sharedHistory.Add(new ChatMessage(ChatRole.Assistant, $"【発言者: {coordinator.Name}】\n{handoffText}"));
                        if (!string.IsNullOrWhiteSpace(handoffText))
                        {
                            sharedHistory.Add(new ChatMessage(ChatRole.System, $"【内部調整情報: {coordinator.Name}】\n{handoffText}"));
                        }
                        if (isComplete)
                        {
                            _logger.LogInformation("コーディネーター '{AgentName}' が complete を返したため、後続処理を終了します。", coordinator.Name);
                            break;
                        }
                    }
                }

                finalCandidates = resultSummary;

                if (isComplete)
                {
                    break;
                }
            }

            if (finalCandidates.Count == 0)
            {
                return new AgentGroupChatResult(group.Id, summary);
            }

            foreach (AgentModel summaryModel in enabledSummaryGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IChatClient chatClient = summaryModel.ChatClient!;
                int maxTurns = summaryModel.Input.MaxTurns > 0 ? summaryModel.Input.MaxTurns : group.Input.MaxTurns;

                List<ChatMessage> requestMessages =
                [
                    new ChatMessage(ChatRole.System, summaryModel.Prompt.System),
                    .. GetRecentHistory(sourceHistory, maxTurns),
                    .. finalCandidates
                ];

                ChatOptions options = CreateOptions(summaryModel);
                int timeoutSeconds = summaryModel.TimeoutSeconds ?? defaultTimeoutSeconds;

                using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                ChatResponse completion;
                try
                {
                    completion = await chatClient.GetResponseAsync(requestMessages, options, timeoutCts.Token);
                }
                catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(canceledException, "サマリー '{AgentName}' の実行がタイムアウトしました。", summaryModel.Name);
                    continue;
                }

                string responseText = completion.Text;
                Console.WriteLine($"サマリー '{summaryModel.Name}' の応答: {responseText}");

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogInformation("サマリー '{AgentName}' からテキスト応答が返されませんでした。", summaryModel.Name);
                    continue;
                }

                Debug.WriteLine(responseText);
                summary.Add(new AgentChatResult(summaryModel.Id, summaryModel.Name, finalRound + 2, responseText));
            }

            return new AgentGroupChatResult(group.Id, summary);
        }

        private static string BuildCoordinatorHandoffMessage(string responseText, out bool isComplete)
        {
            isComplete = false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseText);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return responseText;
                }

                string? reason = GetJsonStringIgnoreCase(document.RootElement, "reason");
                string? nextInstruction = GetJsonStringIgnoreCase(document.RootElement, "nextInstruction");
                string? decision = GetJsonStringIgnoreCase(document.RootElement, "decision");

                isComplete = string.Equals(decision, "complete", StringComparison.OrdinalIgnoreCase);
                if (isComplete)
                {
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(reason) && string.IsNullOrWhiteSpace(nextInstruction))
                {
                    return responseText;
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    return $"nextInstruction: {nextInstruction}";
                }

                if (string.IsNullOrWhiteSpace(nextInstruction))
                {
                    return $"reason: {reason}";
                }

                return $"reason: {reason}\nnextInstruction: {nextInstruction}";
            }
            catch (JsonException)
            {
                return responseText;
            }
        }

        private static void ValidateChatClients(
            IEnumerable<AgentModel> agents)
        {
            List<string> uninitializedAgents = agents.Where(agent => agent.ChatClient is null).Select(agent => $"{agent.Name}（ID: {agent.Id}）").ToList();

            if (uninitializedAgents.Count == 0) return;

            throw new InvalidOperationException("ChatClientが生成されていないエージェントがあります: " + string.Join(", ", uninitializedAgents));
        }

        private static IEnumerable<ChatMessage> GetRecentHistory(IReadOnlyList<ChatMessage> history, int maxTurns)
        {
            int turns = Math.Max(1, maxTurns);
            int messageCount = turns * 2;

            return history.TakeLast(messageCount);
        }

        private static ChatOptions CreateOptions(
            AgentModel agent)
        {
            ChatOptions options = new();

            if (agent.Settings.Temperature is not null)
            {
                options.Temperature = (float)agent.Settings.Temperature.Value;
            }

            if (agent.Settings.MaxOutputTokens is not null)
            {
                options.MaxOutputTokens =
                    agent.Settings.MaxOutputTokens.Value;
            }

            return options;
        }

        private static string? GetJsonStringIgnoreCase(JsonElement element, string propertyName)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString()
                };
            }

            return null;
        }
    }

    public sealed record AgentChatResult(
        string AgentId,
        string AgentName,
        int Round,
        string Content);

    public sealed record AgentGroupChatResult(
        string GroupId,
        IReadOnlyList<AgentChatResult> Results);
}