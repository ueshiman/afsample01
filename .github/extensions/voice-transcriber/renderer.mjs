export function renderHtml() {
    return `<!doctype html>
<html lang="ja">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>音声文字起こし</title>
    <style>
      :root { color-scheme: light dark; }
      * { box-sizing: border-box; }
      body {
        margin: 0;
        background: var(--background-color-default, #ffffff);
        color: var(--text-color-default, #1f2328);
        font-family: var(--font-sans, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif);
        font-size: var(--text-body-medium, 14px);
        line-height: var(--leading-body-medium, 20px);
      }
      main {
        display: grid;
        gap: 16px;
        max-width: 880px;
        margin: 0 auto;
        padding: 24px;
      }
      h1, h2 { margin: 0; font-weight: var(--font-weight-semibold, 600); }
      h1 {
        font-family: var(--font-sans-display, var(--font-sans, sans-serif));
        font-size: var(--text-title-large, 26px);
        line-height: var(--leading-title-large, 32px);
      }
      h2 { font-size: var(--text-title-medium, 18px); }
      .description, .status, .meta {
        margin: 0;
        color: var(--text-color-muted, #59636e);
      }
      .toolbar, .actions {
        display: flex;
        flex-wrap: wrap;
        align-items: end;
        gap: 10px;
      }
      label {
        display: grid;
        flex: 1 1 220px;
        gap: 6px;
        font-weight: var(--font-weight-semibold, 600);
      }
      select, input, button, textarea {
        border: 1px solid var(--border-color-default, #d1d9e0);
        border-radius: 6px;
        background: var(--background-color-default, #ffffff);
        color: var(--text-color-default, #1f2328);
        font: inherit;
      }
      select, input, button { min-height: 36px; padding: 6px 12px; }
      button { cursor: pointer; font-weight: var(--font-weight-semibold, 600); }
      button:focus-visible, select:focus-visible, input:focus-visible, textarea:focus-visible {
        outline: 2px solid var(--color-focus-outline, #0969da);
        outline-offset: 2px;
      }
      button:disabled { cursor: not-allowed; opacity: 0.55; }
      .primary {
        border-color: transparent;
        background: var(--true-color-blue, #0969da);
        color: var(--color-white, #ffffff);
      }
      .recording { background: var(--true-color-red, #cf222e); }
      .status { min-height: 20px; }
      .status[data-kind="error"] { color: var(--true-color-red, #cf222e); }
      textarea {
        width: 100%;
        min-height: 180px;
        resize: vertical;
        padding: 12px;
        line-height: 1.6;
      }
      .interim {
        min-height: 24px;
        margin: 0;
        color: var(--text-color-muted, #59636e);
        font-style: italic;
      }
      .answers { display: grid; gap: 10px; }
      .answer {
        border: 1px solid var(--border-color-default, #d1d9e0);
        border-radius: 8px;
        padding: 12px;
      }
      .answer[data-kind="error"] { border-color: var(--true-color-red, #cf222e); }
      .answer p { margin: 0; white-space: pre-wrap; }
      .answer .source { margin-bottom: 8px; color: var(--text-color-muted, #59636e); }
      .empty {
        padding: 16px;
        border: 1px dashed var(--border-color-default, #d1d9e0);
        border-radius: 8px;
        color: var(--text-color-muted, #59636e);
        text-align: center;
      }
    </style>
  </head>
  <body>
    <main>
      <div>
        <h1>音声文字起こし</h1>
        <p class="description">確定した発話をTutorial01B APIへ順番に送信し、Callbackの回答を表示します。</p>
      </div>

      <label>
        APIエンドポイント
        <input id="endpoint" type="url" value="http://localhost:12670/api/orchestrator/execute" spellcheck="false" />
      </label>

      <div class="toolbar">
        <label>
          認識言語
          <select id="language">
            <option value="ja-JP" selected>日本語</option>
            <option value="en-US">English (US)</option>
            <option value="en-GB">English (UK)</option>
            <option value="zh-CN">中文（简体）</option>
            <option value="zh-TW">中文（繁體）</option>
            <option value="ko-KR">한국어</option>
            <option value="fr-FR">Français</option>
            <option value="de-DE">Deutsch</option>
            <option value="es-ES">Español</option>
          </select>
        </label>
        <button id="toggle" class="primary" type="button">文字起こしを開始</button>
      </div>

      <p id="recognition-status" class="status" role="status" aria-live="polite">待機中</p>
      <p id="interim" class="interim" aria-live="polite"></p>
      <textarea id="transcript" aria-label="文字起こし結果" placeholder="認識された音声がここに表示されます"></textarea>

      <div class="actions">
        <button id="copy" type="button">コピー</button>
        <button id="clear" type="button">文字起こしを消去</button>
      </div>

      <h2>APIの回答</h2>
      <p id="api-status" class="status" role="status" aria-live="polite">発話の確定を待っています。</p>
      <div id="answers" class="answers">
        <div id="empty-answer" class="empty">回答はまだありません。</div>
      </div>
    </main>

    <script>
      const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
      const toggleButton = document.querySelector("#toggle");
      const languageSelect = document.querySelector("#language");
      const endpointInput = document.querySelector("#endpoint");
      const transcript = document.querySelector("#transcript");
      const interim = document.querySelector("#interim");
      const recognitionStatus = document.querySelector("#recognition-status");
      const apiStatus = document.querySelector("#api-status");
      const answers = document.querySelector("#answers");
      const copyButton = document.querySelector("#copy");
      const clearButton = document.querySelector("#clear");

      let recognition;
      let shouldListen = false;

      const savedEndpoint = localStorage.getItem("voice-transcriber-endpoint");
      if (savedEndpoint) endpointInput.value = savedEndpoint;
      endpointInput.addEventListener("change", () => {
        localStorage.setItem("voice-transcriber-endpoint", endpointInput.value.trim());
      });

      function setRecognitionStatus(message, kind = "info") {
        recognitionStatus.textContent = message;
        recognitionStatus.dataset.kind = kind;
      }

      function setApiStatus(message, kind = "info") {
        apiStatus.textContent = message;
        apiStatus.dataset.kind = kind;
      }

      function setListening(listening) {
        toggleButton.textContent = listening ? "停止" : "文字起こしを開始";
        toggleButton.classList.toggle("recording", listening);
        languageSelect.disabled = listening;
      }

      function appendFinalText(text) {
        const prefix = transcript.value && !transcript.value.endsWith("\\n") ? " " : "";
        transcript.value += prefix + text.trim();
        transcript.scrollTop = transcript.scrollHeight;
      }

      function addAnswer(input, content, kind = "result") {
        document.querySelector("#empty-answer")?.remove();
        const article = document.createElement("article");
        article.className = "answer";
        article.dataset.kind = kind;

        const source = document.createElement("p");
        source.className = "source";
        source.textContent = "発話: " + input;

        const result = document.createElement("p");
        result.textContent = content;
        article.append(source, result);
        answers.prepend(article);
      }

      function extractAnswer(payload) {
        const result = payload.result ?? payload.Result ?? payload;
        const items = result.results ?? result.Results ?? [];
        const contents = Array.isArray(items)
          ? items.map((item) => item.content ?? item.Content).filter(Boolean)
          : [];
        return contents.length > 0 ? contents.join("\\n\\n") : "（回答なし）";
      }

      async function sendUtterance(input) {
        const endpoint = endpointInput.value.trim();
        if (!endpoint) {
          setApiStatus("APIエンドポイントを入力してください。", "error");
          return;
        }

        localStorage.setItem("voice-transcriber-endpoint", endpoint);
        try {
          const response = await fetch("/api/send", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ endpoint, input }),
          });
          const result = await response.json();
          if (!response.ok) throw new Error(result.error || "API送信を予約できませんでした。");
          setApiStatus("発話を送信キューへ追加しました。");
        } catch (error) {
          const message = error instanceof Error ? error.message : String(error);
          setApiStatus(message, "error");
          addAnswer(input, message, "error");
        }
      }

      const events = new EventSource("/events");
      events.addEventListener("queued", (event) => {
        const detail = JSON.parse(event.data);
        setApiStatus("送信待ち: " + detail.position + "件");
      });
      events.addEventListener("sending", (event) => {
        const detail = JSON.parse(event.data);
        setApiStatus("APIへ送信中: " + detail.input);
      });
      events.addEventListener("accepted", () => {
        setApiStatus("APIが受け付けました。回答を待っています。");
      });
      events.addEventListener("api-result", (event) => {
        const detail = JSON.parse(event.data);
        addAnswer(detail.input, extractAnswer(detail.payload));
        setApiStatus("APIから回答を受信しました。");
      });
      events.addEventListener("api-error", (event) => {
        const detail = JSON.parse(event.data);
        addAnswer(detail.input, detail.message, "error");
        setApiStatus(detail.message, "error");
      });
      events.onerror = () => setApiStatus("Canvasサーバーとの接続が切れました。", "error");

      if (!Recognition) {
        toggleButton.disabled = true;
        setRecognitionStatus("このブラウザは音声認識に対応していません。Chrome または Edge をお試しください。", "error");
      } else {
        recognition = new Recognition();
        recognition.continuous = true;
        recognition.interimResults = true;

        recognition.addEventListener("start", () => {
          setListening(true);
          setRecognitionStatus("認識中… マイクに向かって話してください。");
        });

        recognition.addEventListener("result", (event) => {
          let finalText = "";
          let interimText = "";
          for (let index = event.resultIndex; index < event.results.length; index += 1) {
            const text = event.results[index][0].transcript;
            if (event.results[index].isFinal) finalText += text;
            else interimText += text;
          }

          if (finalText.trim()) {
            appendFinalText(finalText);
            void sendUtterance(finalText.trim());
          }
          interim.textContent = interimText;
        });

        recognition.addEventListener("error", (event) => {
          const messages = {
            "audio-capture": "マイクが見つかりません。",
            "not-allowed": "マイクの使用が許可されていません。ブラウザの権限設定を確認してください。",
            "no-speech": "音声を検出できませんでした。",
            "network": "音声認識サービスに接続できませんでした。",
          };
          setRecognitionStatus(messages[event.error] || "音声認識エラー: " + event.error, "error");
          if (event.error === "not-allowed" || event.error === "audio-capture") shouldListen = false;
        });

        recognition.addEventListener("end", () => {
          interim.textContent = "";
          if (shouldListen) {
            try {
              recognition.start();
            } catch {
              shouldListen = false;
              setListening(false);
              setRecognitionStatus("音声認識を再開できませんでした。もう一度開始してください。", "error");
            }
          } else {
            setListening(false);
            if (recognitionStatus.dataset.kind !== "error") setRecognitionStatus("停止しました。");
          }
        });
      }

      toggleButton.addEventListener("click", () => {
        if (shouldListen) {
          shouldListen = false;
          recognition.stop();
          return;
        }

        recognition.lang = languageSelect.value;
        shouldListen = true;
        setRecognitionStatus("マイクを準備しています…");
        try {
          recognition.start();
        } catch {
          shouldListen = false;
          setRecognitionStatus("音声認識を開始できませんでした。もう一度お試しください。", "error");
        }
      });

      copyButton.addEventListener("click", async () => {
        if (!transcript.value) {
          setRecognitionStatus("コピーする文字起こし結果がありません。");
          return;
        }
        try {
          await navigator.clipboard.writeText(transcript.value);
          setRecognitionStatus("クリップボードにコピーしました。");
        } catch {
          transcript.select();
          setRecognitionStatus("コピーできませんでした。選択したテキストを手動でコピーしてください。", "error");
        }
      });

      clearButton.addEventListener("click", () => {
        transcript.value = "";
        interim.textContent = "";
        setRecognitionStatus(shouldListen ? "認識中…" : "文字起こし結果を消去しました。");
      });
    </script>
  </body>
</html>`;
}
