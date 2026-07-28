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

            if (enabledAgents.Count == 0)
            {
                return new AgentGroupChatResult(group.Id, []);
            }

            // 実行前にChatClientの設定状態を検証
            ValidateChatClients(enabledAgents);

            List<ChatMessage> sharedHistory = message;
            
            List<AgentChatResult> results = [];

            for (int round = 1; round <= maxRounds; round++)
            {
                foreach (AgentModel agent in enabledAgents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // AgentModelにキャッシュされているChatClientを利用
                    ChatClient chatClient = agent.ChatClient!;

                    List<ChatMessage> requestMessages =
                    [
                        new SystemChatMessage(agent.Prompt.System),.. GetRecentHistory(sharedHistory, agent.Input.MaxTurns)
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
                        _logger.LogInformation(canceledException,"エージェント '{AgentName}' の実行がタイムアウトしました。", agent.Name);
                        //throw new TimeoutException($"エージェント '{agent.Name}' の実行が{timeoutSeconds}秒でタイムアウトしました。");
                        continue;
                    }

                    string responseText = string.Concat(
                        completion.Content
                            .Select(content => content.Text));

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        throw new InvalidOperationException($"エージェント '{agent.Name}' からテキスト応答が返されませんでした。");
                    }

                    results.Add(new AgentChatResult(agent.Id, agent.Name, round, responseText));

                    // 後続エージェントへ回答を引き継ぐ
                    sharedHistory.Add(new AssistantChatMessage($"【発言者: {agent.Name}】\n{responseText}"));
                }
            }

            return new AgentGroupChatResult(group.Id, results);
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
                options.Temperature =
                    (float)agent.Settings.Temperature.Value;
            }

            if (agent.Settings.MaxOutputTokens is not null)
            {
                options.MaxOutputTokenCount =
                    agent.Settings.MaxOutputTokens.Value;
            }

            return options;
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