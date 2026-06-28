# Orchestrator 動作確認手順

## 目的
`Tutorial01B` の `POST /api/orchestrator/execute` が、`agentsettings.json` の設定に従ってマルチエージェント実行されることを確認する。

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
別ターミナルで実行:

```powershell
$baseUrl = "http://localhost:5000"  # 起動ログに合わせて変更
$body = @{
  input = "社内会議の議事録を要約し、注意点を抽出してください。"
  sessionId = "s-001"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/orchestrator/execute" `
  -ContentType "application/json" `
  -Body $body
```

期待結果:
- HTTP 200
- レスポンスに `input`, `sessionId`, `results` が含まれる
- `results` に有効 agent ごとの出力が入る

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
$invalidBody = @{ input = ""; sessionId = "s-002" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/orchestrator/execute" -ContentType "application/json" -Body $invalidBody
```
期待結果:
- HTTP 400
- `input is required.`

### 5-2. 接続エラー
以下のいずれかを意図的に不正化して実行:
- `AZURE_OPENAI_API_KEY`
- `providers[].endpoint`
- `agents[].deployment`

期待結果:
- HTTP 500

## 6. 補足
- 現在の実装は API Key 認証を前提としている。
- `authentication.type = AccessToken` は未対応。