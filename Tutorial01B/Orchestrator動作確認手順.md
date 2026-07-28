# Orchestrator 動作確認手順

## 目的
`Tutorial01B` の `POST /api/orchestrator/execute` が、`agentsettings.json` の設定に従ってマルチエージェント実行され、結果を `callbackUrl` へ非同期返却することを確認する。

## 前提
- .NET 10 SDK がインストール済み
- Azure OpenAI のエンドポイントと API キーを利用可能
- プロジェクトルート: `afsample01/`

## 1. 設定確認
### 1-1. `agentsettings.json`
`Tutorial01B/agentsettings.json` の以下を確認:
- `providers[].endpoint` が実在の Azure OpenAI endpoint
- `agents[].enabled` が実行対象で `true`
- `agents[].deployment` が実在のデプロイ名
- `execution.mode` が `parallel` または `sequential`
- `execution.maxDegreeOfParallelism` が 1 以上

### 1-2. 環境変数
PowerShell で設定:

```powershell
$env:AZURE_OPENAI_API_KEY = "YOUR_API_KEY"
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.openai.azure.com"
```

## 2. 起動
プロジェクトルートで実行:

```powershell
dotnet run --project .\Tutorial01B\Tutorial01B.csproj
```

起動ログに表示される URL（例: `http://localhost:5000` または `https://localhost:7xxx`）を控える。

## 3. 正常系テスト
### 3-1. callback 受信用 URL を用意
以下いずれかを用意:
- Webhook.site などの一時受信 URL
- 自前の受信 API（POST JSON を受け取れる endpoint）

### 3-2. 実行リクエスト送信
別ターミナルで実行:

```powershell
$baseUrl = "http://localhost:5000"  # 起動ログに合わせて変更
$callbackUrl = "https://webhook.site/your-id" # 受信用 URL に置換
$body = @{
  input = "社内会議の議事録を要約し、注意点を抽出してください。"
  sessionId = "s-001"
  callbackUrl = $callbackUrl
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/orchestrator/execute" `
  -ContentType "application/json" `
  -Body $body
```

期待結果:
- API 応答は HTTP 202
- 応答に `requestId`, `sessionId`, `status`（`accepted`）が含まれる

### 3-3. callback 受信確認
callback 側で `POST` 受信を確認。期待 payload:
- `requestId`
- `sessionId`
- `input`
- `status`（`completed` または `failed`）
- `results`（`completed` 時）
- `error`（`failed` 時）

## 4. 実行モード確認
`Tutorial01B/agentsettings.json` の `execution.mode` を切り替えて、各モードで 3. を再実行:
- `sequential`
- `parallel`

期待結果:
- どちらも HTTP 200
- `parallel` は設定次第で応答時間が短くなる傾向

## 5. 異常系テスト
### 5-1. 入力バリデーション
```powershell
$invalidBody = @{ input = ""; sessionId = "s-002"; callbackUrl = $callbackUrl } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/orchestrator/execute" -ContentType "application/json" -Body $invalidBody
```
期待結果:
- HTTP 400
- `input is required.`

### 5-2. callbackUrl バリデーション
```powershell
$invalidCallbackBody = @{ input = "test"; sessionId = "s-003"; callbackUrl = "not-a-url" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/orchestrator/execute" -ContentType "application/json" -Body $invalidCallbackBody
```
期待結果:
- HTTP 400
- `callbackUrl must be absolute http/https URL.`

### 5-3. 実行時エラー
以下のいずれかを意図的に不正化して実行:
- `AZURE_OPENAI_API_KEY`
- `providers[].endpoint`
- `agents[].deployment`

期待結果:
- API は HTTP 202 を返す（受付は成功）
- callback payload の `status` が `failed`
- callback payload の `error` に失敗理由が入る

## 6. 補足
- 現在の実装は API Key 認証を前提としている。
- `authentication.type = AccessToken` は未対応。