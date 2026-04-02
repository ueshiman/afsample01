# Multi-Agent Evaluation Service (Cline01)

POC向けの会話補助マルチエージェントサービスMVPです。1つの発話をAPIで受け取り、JSON定義ファイルで指定された複数エージェントで非同期評価し、結果をWebhookへ返します。

## 技術スタック

- **.NET 10**
- **ASP.NET Core Web API**
- **Microsoft Semantic Kernel** (AI Agent Framework)
- **Dependency Injection** (Microsoft.Extensions.DependencyInjection)
- **BackgroundService** (非同期ジョブ処理)

## 機能

- ✅ REST API エンドポイント (`POST /api/messages/evaluate`)
- ✅ エージェント定義のJSON読み込み (`agents.json`)
- ✅ 複数エージェントの並列実行
- ✅ エージェント間会話調整（異なる評価結果がある場合）
- ✅ Webhook通知
- ✅ タイムアウト・エラーハンドリング
- ✅ 構造化ログ出力

## セットアップ

### 1. 必要なツール

- .NET 10 SDK
- Visual Studio Code または Visual Studio 2022

### 2. 設定ファイルの編集

#### appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "AzureEndpoint": ""
  },
  "Webhook": {
    "Url": "https://webhook.site/your-unique-url",
    "TimeoutSeconds": 30
  }
}
```

**OpenAI設定:**
- **OpenAI API使用時**: `ApiKey`にOpenAI APIキーを設定し、`AzureEndpoint`は空欄
- **Azure OpenAI使用時**: `ApiKey`にAzure APIキー、`AzureEndpoint`にエンドポイントURLを設定

**Webhook設定:**
- `Url`: 評価結果を送信するWebhook URL（テスト用には [webhook.site](https://webhook.site/) が便利）

#### agents.json

エージェント定義ファイル。プロジェクトルートに配置します。

```json
{
  "version": "1.0",
  "agents": [
    {
      "agentId": "compliance-agent",
      "agentName": "ComplianceAgent",
      "enabled": true,
      "systemPrompt": "あなたはコンプライアンス評価エージェントです...",
      "model": {
        "deployment": "gpt-4o-mini",
        "temperature": 0.2,
        "maxTokens": 1000
      },
      "timeoutSeconds": 15,
      "outputFormat": "json"
    }
  ]
}
```

- `enabled: true` のエージェントのみ実行されます（最大10個）
- `agentId` は一意である必要があります

### 3. パッケージの復元

```bash
dotnet restore
```

### 4. アプリケーションの実行

```bash
dotnet run
```

アプリケーションは `http://localhost:5000` および `https://localhost:5001` で起動します。

## API使用方法

### エンドポイント

**POST** `/api/messages/evaluate`

### リクエスト例

```json
{
  "conversationId": "conv-001",
  "messageId": "msg-001",
  "sentenceText": "この商品は必ず利益が出ます",
  "speakerRole": "operator",
  "timestamp": "2026-03-10T12:00:00Z"
}
```

### レスポンス例

```json
{
  "accepted": true,
  "jobId": "job-abc123def456",
  "status": "Accepted"
}
```

### Webhook通知ペイロード例

評価完了後、以下の形式でWebhookにPOSTリクエストが送信されます。

```json
{
  "eventType": "evaluation.completed",
  "jobId": "job-abc123def456",
  "conversationId": "conv-001",
  "messageId": "msg-001",
  "agentResults": [
    {
      "agentId": "compliance-agent",
      "agentName": "ComplianceAgent",
      "status": "Completed",
      "result": {
        "status": "warning",
        "reason": "断定的表現の可能性"
      }
    },
    {
      "agentId": "sentiment-agent",
      "agentName": "SentimentAgent",
      "status": "Completed",
      "result": {
        "sentiment": "positive",
        "confidence": 0.85
      }
    }
  ],
  "conversationResult": {
    "summary": "エージェント間の会話結果として注意喚起が必要と判断された。",
    "details": {
      "finalAssessment": "warning"
    }
  }
}
```

**注意:**
- `conversationResult` は、複数のエージェントが異なる評価結果を返した場合にのみ含まれます
- エージェントの実行に失敗した場合、`status: "Failed"` または `status: "Timeout"` となり、`errorMessage` が含まれます

## プロジェクト構成

```
Cline01/
├── Controllers/
│   └── EvaluationController.cs       # REST API エンドポイント
├── Models/
│   ├── EvaluateMessageRequest.cs     # リクエストモデル
│   ├── EvaluateMessageResponse.cs    # レスポンスモデル
│   ├── AgentDefinition.cs            # エージェント定義
│   ├── AgentDefinitionsRoot.cs       # agents.json ルート
│   ├── AgentExecutionResult.cs       # エージェント実行結果
│   ├── ConversationResult.cs         # 会話調整結果
│   ├── EvaluationJob.cs              # 評価ジョブ
│   └── EvaluationWebhookPayload.cs   # Webhook ペイロード
├── Services/
│   ├── Interfaces/
│   │   ├── IAgentDefinitionLoader.cs
│   │   ├── IAgentDefinitionValidator.cs
│   │   ├── IAgentExecutor.cs
│   │   ├── IAgentConversationCoordinator.cs
│   │   ├── IEvaluationJobQueue.cs
│   │   └── IWebhookNotifier.cs
│   ├── AgentDefinitionLoader.cs      # エージェント定義読み込み
│   ├── AgentDefinitionValidator.cs   # エージェント定義検証
│   ├── AgentExecutor.cs              # エージェント実行
│   ├── AgentConversationCoordinator.cs # エージェント間会話調整
│   ├── EvaluationJobQueue.cs         # ジョブキュー
│   ├── EvaluationBackgroundService.cs # バックグラウンド処理
│   └── WebhookNotifier.cs            # Webhook 通知
├── Program.cs                        # アプリケーションエントリーポイント
├── appsettings.json                  # 設定ファイル
├── agents.json                       # エージェント定義ファイル
└── Cline01.csproj                    # プロジェクトファイル
```

## アーキテクチャ

1. **API レイヤー**: EvaluationController がリクエストを受け取り、ジョブをキューに登録
2. **ジョブキュー**: スレッドセーフなチャネルベースのキュー（in-memory）
3. **BackgroundService**: キューからジョブを取得し、非同期で処理
4. **エージェント実行**: 有効なエージェントを並列実行（Semantic Kernel使用）
5. **会話調整**: 異なる評価結果がある場合、AIによる統合判断
6. **Webhook通知**: 結果をHTTP POSTで送信

## 開発のポイント

### DIコンテナ

全てのサービスはProgram.csでDI登録されています：
- `Singleton`: キュー、ローダー、バリデーター
- `Scoped`: エグゼキューター、コーディネーター、通知サービス
- `HostedService`: バックグラウンドサービス

### エラーハンドリング

- エージェント実行の失敗は個別に処理され、他のエージェントの実行を妨げません
- タイムアウトは各エージェントの `timeoutSeconds` 設定で制御されます
- 全体的な失敗時もWebhookで通知されます

### 拡張性

- **Azure OpenAI への切り替え**: `appsettings.json` で `AzureEndpoint` を設定するだけ
- **新しいエージェント追加**: `agents.json` にエージェント定義を追加
- **カスタムバリデーション**: `IAgentDefinitionValidator` を実装

## テスト方法

### curlでのテスト

```bash
curl -X POST https://localhost:5001/api/messages/evaluate \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "conv-001",
    "messageId": "msg-001",
    "sentenceText": "この商品は必ず利益が出ます",
    "speakerRole": "operator",
    "timestamp": "2026-03-13T10:00:00Z"
  }'
```

### Swagger UI

ブラウザで `https://localhost:5001/swagger` にアクセスして、インタラクティブにAPIをテストできます。

## トラブルシューティング

### agents.json が見つからない

エラー: `Agent definition file not found`

**解決策**: プロジェクトルート（Cline01.csproj と同じディレクトリ）に `agents.json` が存在することを確認してください。

### API キーエラー

エラー: `401 Unauthorized` または認証エラー

**解決策**: `appsettings.json` の `OpenAI.ApiKey` が正しく設定されているか確認してください。

### Webhook が届かない

**確認事項**:
1. `appsettings.json` の `Webhook.Url` が正しいか
2. ネットワーク接続が有効か
3. ログに Webhook 送信のエラーメッセージがないか

## ライセンス

このプロジェクトはPOC/MVP用のサンプル実装です。
