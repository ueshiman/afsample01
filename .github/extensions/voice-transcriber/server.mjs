import { randomUUID } from "node:crypto";
import { createServer } from "node:http";
import { renderHtml } from "./renderer.mjs";

const MAX_BODY_BYTES = 1024 * 1024;
const API_ACCEPT_TIMEOUT_MS = 15_000;
const CALLBACK_TIMEOUT_MS = 120_000;

function writeJson(response, statusCode, value) {
    response.writeHead(statusCode, {
        "Content-Type": "application/json; charset=utf-8",
        "Cache-Control": "no-store",
    });
    response.end(JSON.stringify(value));
}

async function readJson(request) {
    const contentType = request.headers["content-type"] ?? "";
    if (!contentType.toLowerCase().startsWith("application/json")) {
        throw new Error("Content-Type must be application/json.");
    }

    const chunks = [];
    let length = 0;
    for await (const chunk of request) {
        length += chunk.length;
        if (length > MAX_BODY_BYTES) {
            throw new Error("Request body is too large.");
        }
        chunks.push(chunk);
    }

    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function validateEndpoint(value) {
    const endpoint = new URL(value);
    if (endpoint.protocol !== "http:" && endpoint.protocol !== "https:") {
        throw new Error("API URL must use http or https.");
    }
    return endpoint.toString();
}

export async function startCanvasServer() {
    const clients = new Set();
    const queue = [];
    const pendingCallbacks = new Map();
    let baseUrl;
    let processing = false;
    let sessionId = null;

    function publish(type, value) {
        const message = `event: ${type}\ndata: ${JSON.stringify(value)}\n\n`;
        for (const client of clients) {
            client.write(message);
        }
    }

    function waitForCallback(requestId, input) {
        let timer;
        const promise = new Promise((resolve, reject) => {
            timer = setTimeout(() => {
                pendingCallbacks.delete(requestId);
                reject(new Error("API callback timed out after 120 seconds."));
            }, CALLBACK_TIMEOUT_MS);

            pendingCallbacks.set(requestId, {
                input,
                resolve: (payload) => {
                    clearTimeout(timer);
                    resolve(payload);
                },
                reject: (error) => {
                    clearTimeout(timer);
                    reject(error);
                },
            });
        });

        return {
            promise,
            cancel: () => {
                clearTimeout(timer);
                pendingCallbacks.delete(requestId);
            },
        };
    }

    async function executeJob(job) {
        const callbackWait = waitForCallback(job.requestId, job.input);
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), API_ACCEPT_TIMEOUT_MS);

        publish("sending", job);

        try {
            const response = await fetch(job.endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    input: job.input,
                    sessionId,
                    callbackUrl: `${baseUrl}api/callback?requestId=${encodeURIComponent(job.requestId)}`,
                    requestId: job.requestId,
                }),
                signal: controller.signal,
            });

            const responseText = await response.text();
            let accepted;
            try {
                accepted = responseText ? JSON.parse(responseText) : {};
            } catch {
                throw new Error(`API returned invalid JSON (HTTP ${response.status}).`);
            }

            if (!response.ok) {
                const detail = accepted.error ?? accepted.title ?? response.statusText;
                throw new Error(`API rejected the request (HTTP ${response.status}): ${detail}`);
            }

            sessionId = accepted.sessionId ?? accepted.SessionId ?? sessionId;
            publish("accepted", { requestId: job.requestId, input: job.input, sessionId });
            await callbackWait.promise;
        } catch (error) {
            callbackWait.cancel();
            throw error;
        } finally {
            clearTimeout(timeout);
        }
    }

    async function processQueue() {
        if (processing) {
            return;
        }

        processing = true;
        try {
            while (queue.length > 0) {
                const job = queue.shift();
                try {
                    await executeJob(job);
                } catch (error) {
                    publish("api-error", {
                        requestId: job.requestId,
                        input: job.input,
                        message: error instanceof Error ? error.message : String(error),
                    });
                }
            }
        } finally {
            processing = false;
        }
    }

    const server = createServer(async (request, response) => {
        const requestUrl = new URL(request.url ?? "/", baseUrl ?? "http://127.0.0.1");

        if (request.method === "GET" && requestUrl.pathname === "/") {
            response.writeHead(200, {
                "Content-Type": "text/html; charset=utf-8",
                "Cache-Control": "no-store",
            });
            response.end(renderHtml());
            return;
        }

        if (request.method === "GET" && requestUrl.pathname === "/events") {
            response.writeHead(200, {
                "Content-Type": "text/event-stream; charset=utf-8",
                "Cache-Control": "no-cache",
                Connection: "keep-alive",
            });
            response.write("event: ready\ndata: {}\n\n");
            clients.add(response);
            request.on("close", () => clients.delete(response));
            return;
        }

        if (request.method === "POST" && requestUrl.pathname === "/api/send") {
            try {
                const body = await readJson(request);
                const input = typeof body.input === "string" ? body.input.trim() : "";
                if (!input) {
                    writeJson(response, 400, { error: "文字起こし結果が空です。" });
                    return;
                }

                const endpoint = validateEndpoint(body.endpoint);
                const job = { requestId: randomUUID(), input, endpoint };
                queue.push(job);
                writeJson(response, 202, { requestId: job.requestId, status: "queued" });
                publish("queued", { ...job, position: queue.length });
                void processQueue();
            } catch (error) {
                writeJson(response, 400, {
                    error: error instanceof Error ? error.message : String(error),
                });
            }
            return;
        }

        if (request.method === "POST" && requestUrl.pathname === "/api/callback") {
            const requestId = requestUrl.searchParams.get("requestId");
            const pending = requestId ? pendingCallbacks.get(requestId) : null;
            if (!requestId || !pending) {
                writeJson(response, 404, { error: "Unknown or expired requestId." });
                return;
            }

            try {
                const payload = await readJson(request);
                pendingCallbacks.delete(requestId);
                publish("api-result", { requestId, input: pending.input, payload });
                pending.resolve(payload);
                response.writeHead(204).end();
            } catch (error) {
                writeJson(response, 400, {
                    error: error instanceof Error ? error.message : String(error),
                });
            }
            return;
        }

        response.writeHead(404).end();
    });

    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    baseUrl = `http://127.0.0.1:${port}/`;

    return {
        server,
        url: baseUrl,
        close: async () => {
            for (const client of clients) {
                client.end();
            }
            clients.clear();

            for (const pending of pendingCallbacks.values()) {
                pending.reject(new Error("Canvas closed."));
            }
            pendingCallbacks.clear();

            await new Promise((resolve) => server.close(resolve));
        },
    };
}
