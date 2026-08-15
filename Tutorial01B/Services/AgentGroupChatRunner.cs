using System.Diagnostics;
using System.Text.Json;
using OpenAI.Chat;
using Tutorial01B.Models;

namespace Tutorial01B.Services
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
            List<AgentChatResult> results = new();
            List<AgentChatResult> summary = new();

            for (int round = 1; round <= maxRounds; round++)
            {
                List<ChatMessage> resultSummary = new();

                foreach (AgentModel agent in enabledAgents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // AgentModelにキャッシュされているChatClientを利用
                    ChatClient chatClient = agent.ChatClient!;

                    int maxTurns = agent.Input.MaxTurns > 0 ? agent.Input.MaxTurns : group.Input.MaxTurns;
                    List<ChatMessage> requestMessages =
                    [
                        new SystemChatMessage(agent.Prompt.System),.. GetRecentHistory(sharedHistory, maxTurns)
                    ];

                    ChatCompletionOptions options = CreateOptions(agent);

                    int timeoutSeconds = agent.TimeoutSeconds ?? defaultTimeoutSeconds;

                    using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                    ChatCompletion completion;

                    try
                    {
                        completion = await chatClient.CompleteChatAsync(requestMessages, options, timeoutCts.Token);
                    }
                    catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation(canceledException, "エージェント '{AgentName}' の実行がタイムアウトしました。", agent.Name);
                        continue;
                    }

                    string responseText = string.Concat(completion.Content.Select(content => content.Text));

                    Console.WriteLine($"エージェント '{agent.Name}' の応答: {responseText}");

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        _logger.LogInformation("エージェント '{AgentName}' からテキスト応答が返されませんでした。", agent.Name);
                    }
                    else
                    {
                        results.Add(new AgentChatResult(agent.Id, agent.Name, round, responseText));
                        // 後続エージェントへ回答を引き継ぐ
                        sharedHistory.Add(new AssistantChatMessage($"【発言者: {agent.Name}】\n{responseText}"));
                        //resultSummary.Add(new UserChatMessage(responseText));
                        resultSummary.Add(new AssistantChatMessage($"【候補情報: {agent.Name}】\n{responseText}"));
                    }
                }

                // エージェント結果がある場合のみ、後段の coordinator を実行
                if (resultSummary.Count > 0)
                {
                    foreach (AgentModel coordinator in enabledCoordinators)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ChatClient chatClient = coordinator.ChatClient!;
                        int maxTurns = coordinator.Input.MaxTurns > 0 ? coordinator.Input.MaxTurns : group.Input.MaxTurns;

                        List<ChatMessage> requestMessages =
                        [
                            new SystemChatMessage(coordinator.Prompt.System),
                        .. GetRecentHistory(sharedHistory, maxTurns)
                        ];

                        ChatCompletionOptions options = CreateOptions(coordinator);
                        int timeoutSeconds = coordinator.TimeoutSeconds ?? defaultTimeoutSeconds;

                        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                        ChatCompletion completion;
                        try
                        {
                            completion = await chatClient.CompleteChatAsync(requestMessages, options, timeoutCts.Token);
                        }
                        catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation(canceledException, "コーディネーター '{AgentName}' の実行がタイムアウトしました。", coordinator.Name);
                            continue;
                        }

                        string responseText = string.Concat(completion.Content.Select(content => content.Text));
                        Console.WriteLine($"コーディネーター '{coordinator.Name}' の応答: {responseText}");

                        if (string.IsNullOrWhiteSpace(responseText))
                        {
                            _logger.LogInformation("コーディネーター '{AgentName}' からテキスト応答が返されませんでした。", coordinator.Name);
                            continue;
                        }

                        //results.Add(new AgentChatResult(coordinator.Id, coordinator.Name, round + 1, responseText));
                        //resultSummary.Add(ChatMessage.CreateAssistantMessage(responseText));

                        string handoffText = BuildCoordinatorHandoffMessage(responseText, out bool isComplete);
                        sharedHistory.Add(new AssistantChatMessage($"【発言者: {coordinator.Name}】\n{handoffText}"));
                        if (!string.IsNullOrWhiteSpace(handoffText))
                        {
                            sharedHistory.Add(new SystemChatMessage($"【内部調整情報: {coordinator.Name}】\n{handoffText}"));
                        }
                        if (isComplete)
                        {
                            _logger.LogInformation("コーディネーター '{AgentName}' が complete を返したため、後続処理を終了します。", coordinator.Name);
                            break;
                        }
                        //resultSummary.Add(ChatMessage.CreateAssistantMessage(handoffText));

                    }
                }


                // summaryGroup処理
                if (enabledSummaryGroup.Count > 0 && resultSummary.Count > 0)
                {
                    foreach (AgentModel summaryModel in enabledSummaryGroup)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ChatClient chatClient = summaryModel.ChatClient!;
                        int maxTurns = summaryModel.Input.MaxTurns > 0 ? summaryModel.Input.MaxTurns : group.Input.MaxTurns;

                        List<ChatMessage> requestMessages =
                        [
                            new SystemChatMessage(summaryModel.Prompt.System),

                            // 元の会話
                            .. GetRecentHistory(sourceHistory, maxTurns),

                            // Agent が生成した提示候補だけ
                            .. resultSummary
                        ];

                        ChatCompletionOptions options = CreateOptions(summaryModel);
                        int timeoutSeconds = summaryModel.TimeoutSeconds ?? defaultTimeoutSeconds;

                        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                        ChatCompletion completion;
                        try
                        {
                            completion = await chatClient.CompleteChatAsync(requestMessages, options, timeoutCts.Token);
                        }
                        catch (OperationCanceledException canceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation(canceledException, "サマリー '{AgentName}' の実行がタイムアウトしました。", summaryModel.Name);
                            continue;
                        }

                        string responseText = string.Concat(completion.Content.Select(content => content.Text));
                        Console.WriteLine($"サマリー '{summaryModel.Name}' の応答: {responseText}");

                        if (string.IsNullOrWhiteSpace(responseText))
                        {
                            _logger.LogInformation("サマリー '{AgentName}' からテキスト応答が返されませんでした。", summaryModel.Name);
                            continue;
                        }
                        Debug.WriteLine(responseText);
                        results.Add(new AgentChatResult(summaryModel.Id, summaryModel.Name, round + 2, responseText));
                        summary.Add(new AgentChatResult(summaryModel.Id, summaryModel.Name, round + 2, responseText));
                        //sharedHistory.Add(new AssistantChatMessage($"【発言者: {summaryModel.Name}】\n{responseText}"));
                    }
                }



                //if (results.Any()) break; // 1ラウンドで1つの応答があれば次のラウンドへ進む
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

        private static ChatCompletionOptions CreateOptions(
            AgentModel agent)
        {
            ChatCompletionOptions options = new();

            if (agent.Settings.Temperature is not null)
            {
                options.Temperature = (float)agent.Settings.Temperature.Value;
            }

            if (agent.Settings.MaxOutputTokens is not null)
            {
                options.MaxOutputTokenCount =
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