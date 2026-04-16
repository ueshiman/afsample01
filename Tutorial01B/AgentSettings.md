。

## 前提整理

今はこう考えます。

* **Foundry Project** は使わない
* **Azure OpenAI の resource endpoint** に接続する
* **deployment は agent ごと**に持つ
* **agent の構成は JSON から動的生成**する

つまり、今の設計では **Project は登場しません**。
登場するのは次の3つです。

* `provider`
* `deployment`
* `agent`

---

## 役割分担

### provider

接続先の定義です。

持つもの:

* endpoint
* 認証方式
* 共通既定値

### agent

実行するエージェントの定義です。

持つもの:

* id
* name
* type
* providerRef
* deployment
* prompt
* timeout
* callbackRef

### callback

結果返却先です。

---

## いまのおすすめ最終形

```json
{
  "version": "0.1",
  "service": {
    "name": "ConversationSuggestionService",
    "defaultLocale": "ja-JP",
    "defaultTimeoutSeconds": 30
  },
  "providers": [
    {
      "id": "aoai-main",
      "type": "AzureOpenAI",
      "endpoint": "https://your-resource.openai.azure.com",
      "authentication": {
        "type": "AccessToken"
      },
      "defaults": {
        "temperature": 0.2,
        "maxOutputTokens": 2000
      }
    }
  ],
  "callbacks": [
    {
      "id": "default",
      "type": "webhook",
      "url": "https://example.com/api/agent-callback",
      "includeConversation": true,
      "includeAgentMetadata": true
    }
  ],
  "execution": {
    "mode": "parallel",
    "returnMode": "perAgent",
    "maxDegreeOfParallelism": 3
  },
  "agents": [
    {
      "id": "compliance-checker",
      "name": "Compliance Checker",
      "enabled": true,
      "type": "policy-check",
      "providerRef": "aoai-main",
      "deployment": "gpt-4o-mini",
      "callbackRef": "default",
      "priority": 100,
      "timeoutSeconds": 15,
      "prompt": {
        "system": "あなたは会話監視エージェントです。会話文から、法令違反、ハラスメント、不適切表現、個人情報漏えい、社内規程違反の可能性を抽出してください。"
      },
      "input": {
        "source": "conversation",
        "format": "plainText",
        "maxTurns": 20
      },
      "output": {
        "format": "json",
        "schemaName": "ComplianceResult"
      },
      "settings": {
        "temperature": 0.1,
        "maxOutputTokens": 1000
      }
    },
    {
      "id": "reference-suggester",
      "name": "Reference Suggester",
      "enabled": true,
      "type": "suggestion",
      "providerRef": "aoai-main",
      "deployment": "gpt-4.1-mini",
      "callbackRef": "default",
      "priority": 50,
      "timeoutSeconds": 20,
      "prompt": {
        "system": "あなたは会話支援エージェントです。会話内容を読み取り、参考情報、確認事項、次のアクション候補を簡潔に提案してください。"
      },
      "input": {
        "source": "conversation",
        "format": "plainText",
        "maxTurns": 20
      },
      "output": {
        "format": "json",
        "schemaName": "SuggestionResult"
      },
      "settings": {
        "temperature": 0.3,
        "maxOutputTokens": 1200
      }
    }
  ]
}
```

```
{
  // 設定ファイルのバージョン
  "version": "0.1",

  // サービス全体の基本設定
  "service": {
    // サービス名
    "name": "ConversationSuggestionService",

    // 既定ロケール
    "defaultLocale": "ja-JP",

    // agent 個別に timeoutSeconds が未指定の場合の既定値
    "defaultTimeoutSeconds": 30
  },

  // LLM 接続先の定義一覧
  // 今は Azure OpenAI を前提としている
  "providers": [
    {
      // provider の識別子
      // agents[].providerRef から参照する
      "id": "aoai-main",

      // 接続先種別
      "type": "AzureOpenAI",

      // Azure OpenAI の resource endpoint
      // Project endpoint ではなく resource endpoint
      "endpoint": "https://your-resource.openai.azure.com",

      // 認証設定
      "authentication": {
        // 認証方式
        // 例: AccessToken / ApiKey
        "type": "AccessToken"
      },

      // provider 共通の既定推論設定
      // agent 側 settings で個別上書き可能
      "defaults": {
        // 既定 temperature
        "temperature": 0.2,

        // 既定最大出力トークン数
        "maxOutputTokens": 2000
      }
    }
  ],

  // agent 実行結果の返却先定義
  "callbacks": [
    {
      // callback の識別子
      // agents[].callbackRef から参照する
      "id": "default",

      // callback 種別
      // 今は webhook を想定
      "type": "webhook",

      // 結果送信先 URL
      "url": "https://example.com/api/agent-callback",

      // 元の会話内容を callback payload に含めるか
      "includeConversation": true,

      // agentId, agentName, type などのメタ情報を含めるか
      "includeAgentMetadata": true
    }
  ],

  // サービス全体の実行方式
  "execution": {
    // 実行モード
    // parallel: 並列実行
    // sequential: 順次実行
    "mode": "parallel",

    // 戻り値の返し方
    // perAgent: agent ごとに返す
    // aggregated: 後で統合返却する拡張も考えられる
    "returnMode": "perAgent",

    // 最大並列実行数
    "maxDegreeOfParallelism": 3
  },

  // 動的生成する agent 一覧
  "agents": [
    {
      // agent の内部識別子
      "id": "compliance-checker",

      // 表示名
      "name": "Compliance Checker",

      // この agent を有効化するか
      "enabled": true,

      // agent 種別
      // 実装側で分岐やロギングに使える
      "type": "policy-check",

      // 利用する provider
      // providers[].id を参照
      "providerRef": "aoai-main",

      // 利用する Azure OpenAI deployment 名
      // provider ではなく agent 側で持つ
      "deployment": "gpt-4o-mini",

      // 結果の返却先
      // callbacks[].id を参照
      "callbackRef": "default",

      // 実行優先度
      // 数字が大きいものを優先する運用を想定
      "priority": 100,

      // この agent 個別の timeout 秒数
      "timeoutSeconds": 15,

      // Prompt 定義
      "prompt": {
        // system prompt
        "system": "あなたは会話監視エージェントです。会話文から、法令違反、ハラスメント、不適切表現、個人情報漏えい、社内規程違反の可能性を抽出してください。"
      },

      // この agent が受け取る入力の定義
      "input": {
        // 入力元
        // 今は conversation 固定に近い想定
        "source": "conversation",

        // 入力形式
        "format": "plainText",

        // 会話の最大取り込みターン数
        "maxTurns": 20
      },

      // 出力の期待形式
      "output": {
        // 返却形式
        "format": "json",

        // 期待する出力スキーマ名
        // 実装側で DTO マッピングや検証に利用可能
        "schemaName": "ComplianceResult"
      },

      // provider.defaults を個別上書きする設定
      "settings": {
        // 厳密判定寄りなので低め
        "temperature": 0.1,

        // 最大出力トークン数
        "maxOutputTokens": 1000
      }
    },
    {
      // agent の内部識別子
      "id": "reference-suggester",

      // 表示名
      "name": "Reference Suggester",

      // この agent を有効化するか
      "enabled": true,

      // agent 種別
      "type": "suggestion",

      // 利用する provider
      "providerRef": "aoai-main",

      // 利用する deployment 名
      "deployment": "gpt-4.1-mini",

      // 結果の返却先
      "callbackRef": "default",

      // 実行優先度
      "priority": 50,

      // この agent 個別の timeout 秒数
      "timeoutSeconds": 20,

      // Prompt 定義
      "prompt": {
        // system prompt
        "system": "あなたは会話支援エージェントです。会話内容を読み取り、参考情報、確認事項、次のアクション候補を簡潔に提案してください。"
      },

      // 入力定義
      "input": {
        // 入力元
        "source": "conversation",

        // 入力形式
        "format": "plainText",

        // 会話の最大取り込みターン数
        "maxTurns": 20
      },

      // 出力定義
      "output": {
        // 返却形式
        "format": "json",

        // 想定スキーマ名
        "schemaName": "SuggestionResult"
      },

      // この agent 個別の推論設定
      "settings": {
        // 提案生成なので少し高め
        "temperature": 0.3,

        // 最大出力トークン数
        "maxOutputTokens": 1200
      }
    }
  ]
}
```



---

## この形の読み方

### 1. provider

```json
{
  "id": "aoai-main",
  "type": "AzureOpenAI",
  "endpoint": "https://your-resource.openai.azure.com"
}
```

これは
**「どこに接続するか」**
だけを表します。

---

### 2. agent

```json
{
  "id": "compliance-checker",
  "providerRef": "aoai-main",
  "deployment": "gpt-4o-mini"
}
```

これは
**「どの接続先を使い、どの deployment で動かすか」**
を表します。

---

### 3. callback

```json
{
  "id": "default",
  "type": "webhook",
  "url": "https://example.com/api/agent-callback"
}
```

これは
**「結果をどこへ返すか」**
です。

---

## 今の段階で入れないもの

今は入れなくてよいです。

* project
* foundry 固有設定
* agent 間の複雑な依存関係
* ツール定義
* RAG 定義
* 条件分岐 DSL

最初は **単純に JSON を読み、agent を並列実行する** ところまでで十分です。

---

## 実装の流れ

C# 側ではこうなります。

1. JSON を読む
2. `providers` を辞書化
3. `callbacks` を辞書化
4. `enabled == true` の agents を取得
5. `providerRef` から接続先を解決
6. `deployment` を指定して実行
7. 結果を webhook に返す

かなり素直です。

---

