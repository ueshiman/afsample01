import { createCanvas, joinSession } from "@github/copilot-sdk/extension";
import { startCanvasServer } from "./server.mjs";

const servers = new Map();

await joinSession({
    canvases: [
        createCanvas({
            id: "voice-transcriber",
            displayName: "音声文字起こし",
            description: "マイク入力を文字起こしし、確定発話をTutorial01B APIへ送って回答を表示します。",
            actions: [
                {
                    name: "get_capabilities",
                    description: "音声文字起こしCanvasの機能と対応言語を返します。",
                    handler: () => ({
                        defaultLanguage: "ja-JP",
                        defaultEndpoint: "http://localhost:12670/api/orchestrator/execute",
                        languages: ["ja-JP", "en-US", "en-GB", "zh-CN", "zh-TW", "ko-KR", "fr-FR", "de-DE", "es-ES"],
                        features: ["continuous recognition", "automatic API submission", "callback display", "copy", "clear"],
                    }),
                },
            ],
            open: async (ctx) => {
                let entry = servers.get(ctx.instanceId);
                if (!entry) {
                    entry = await startCanvasServer();
                    servers.set(ctx.instanceId, entry);
                }

                return {
                    title: "音声文字起こし",
                    status: "マイク入力を待機中",
                    url: entry.url,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    await entry.close();
                }
            },
        }),
    ],
});
