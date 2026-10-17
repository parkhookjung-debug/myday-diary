import { env, SELF, runInDurableObject } from "cloudflare:test";
import { describe, it, expect, beforeEach } from "vitest";
import type { ChatRoom } from "../src/index";

declare module "cloudflare:test" { interface ProvidedEnv extends Env {} }
const code = "local-test-code-only";
let testIP = 1;
async function api(path: string, method = "GET", data?: object, token = "") {
  return SELF.fetch("https://chat.test" + path, { method,
    headers: { "Content-Type": "application/json", "CF-Connecting-IP": "192.0.2." + testIP, ...(token ? { Authorization: "Bearer " + token } : {}) },
    body: data ? JSON.stringify(data) : undefined,
  });
}
async function join(nickname = "맥 사용자") {
  const response = await api("/api/sessions", "POST", { nickname, joinCode: code });
  expect(response.status).toBe(201);
  return response.json<{ token: string; nickname: string }>();
}
async function roomSQL(statement: string, ...args: (string | number)[]) {
  const stub = env.ROOMS.getByName("sangmon-lounge-v1");
  return runInDurableObject(stub, (_instance: ChatRoom, state) => state.storage.sql.exec(statement, ...args).toArray());
}
beforeEach(async () => {
  testIP++;
  await roomSQL("DELETE FROM sessions; DELETE FROM messages; DELETE FROM daily;");
});

describe("free native chat contract", () => {
  it("health and routing do not allocate chat data", async () => {
    expect((await (await api("/health")).json<{ protocol: number }>()).protocol).toBe(1);
    expect((await api("/not-an-api")).status).toBe(404);
    expect((await api("/api/messages")).status).toBe(401);
  });
  it("rejects invalid invitation, duplicate and unsafe nicknames", async () => {
    expect((await api("/api/sessions", "POST", { nickname: "친구", joinCode: "wrong" })).status).toBe(403);
    expect((await api("/api/sessions", "POST", { nickname: "<script>", joinCode: code })).status).toBe(400);
    await join("Sangmon");
    expect((await api("/api/sessions", "POST", { nickname: "ＳＡＮＧＭＯＮ", joinCode: code })).status).toBe(409);
  });
  it("shares Unicode/newlines between two clients and retries exactly once", async () => {
    const a = await join("Windows 친구"), b = await join();
    const data = { text: "안녕하세요 🌱\r\n맥에서도 보여요", clientId: "a".repeat(32) };
    const sent = await (await api("/api/messages", "POST", data, a.token)).json<{ id: number }>();
    const retry = await (await api("/api/messages", "POST", data, a.token)).json<{ id: number }>();
    expect(retry.id).toBe(sent.id);
    const page = await (await api("/api/messages?after=0&wait=20", "GET", undefined, b.token))
      .json<{ messages: { text: string }[]; pollAfterMs: number; users: string[] }>();
    expect(page.messages).toHaveLength(1);
    expect(page.messages[0].text).toBe("안녕하세요 🌱\n맥에서도 보여요");
    expect(page.users).toContain("Windows 친구");
    expect(page.pollAfterMs).toBe(5000);
    expect((await api("/api/messages", "POST", { ...data, text: "다른 내용" }, a.token)).status).toBe(409);
    expect((await api("/api/session", "DELETE", undefined, b.token)).status).toBe(200);
    expect((await api("/api/messages", "GET", undefined, b.token)).status).toBe(401);
  });
  it("limits text, control characters and streamed request bodies", async () => {
    const { token } = await join();
    for (const text of [" ", "a".repeat(2001), "bad\u0000text", "bad\u202etext", "bad\ud800text", "bad\f"])
      expect((await api("/api/messages", "POST", { text, clientId: "a".repeat(32) }, token)).status).toBe(400);
    expect((await api("/api/messages", "POST", { text: "hello", clientId: "not-an-id" }, token)).status).toBe(400);
    expect((await api("/api/sessions", "POST", { nickname: "a".repeat(20000), joinCode: code })).status).toBe(413);
    expect((await api("/api/messages?after=-1", "GET", undefined, token)).status).toBe(400);
    expect((await SELF.fetch("https://chat.test/api/messages", { headers: { Origin: "https://other.test" } })).status).toBe(403);
  });
  it("keeps messages and hashed sessions when the object restarts", async () => {
    const a = await join();
    await api("/api/messages", "POST", { text: "다시 만나도 남는 이야기", clientId: "b".repeat(32) }, a.token);
    const sessions = await roomSQL("SELECT * FROM sessions");
    expect(JSON.stringify(sessions)).not.toContain(a.token);
    // New class instance against the same storage (the same constructor path as eviction).
    const stub = env.ROOMS.getByName("sangmon-lounge-v1");
    await runInDurableObject(stub, async (_instance: ChatRoom, state) => {
      const { ChatRoom } = await import("../src/index");
      const restarted = new ChatRoom(state, env);
      const result = await restarted.request({ method: "GET", path: "/api/messages", token: a.token, body: {}, after: 0, wait: 0, ip: "local" });
      expect(result.status).toBe(200);
      expect(JSON.stringify(result.payload)).toContain("다시 만나도 남는 이야기");
    });
  });
  it("expires idle sessions and frees the nickname", async () => {
    const old = await join();
    await roomSQL("UPDATE sessions SET seen=0");
    expect((await api("/api/messages", "GET", undefined, old.token)).status).toBe(401);
    await join();
  });
  it("never skips backlog and resets a cursor from replaced data", async () => {
    const a = await join();
    for (let i = 0; i < 125; i++)
      await roomSQL("INSERT INTO messages(sid,client_id,nickname,text,sent_at) VALUES('fixture',?,'친구','기록','2026-10-17T00:00:00Z')", i.toString(16).padStart(32, "0"));
    const bounds = await roomSQL("SELECT MIN(id) AS first,MAX(id) AS last FROM messages");
    const first = bounds[0].first as number;
    const page = await (await api(`/api/messages?after=${first}&wait=0`, "GET", undefined, a.token)).json<{ cursor: number; messages: object[] }>();
    expect(page.messages).toHaveLength(100);
    expect(page.cursor).toBe(first + 100);
    const rest = await (await api(`/api/messages?after=${page.cursor}`, "GET", undefined, a.token)).json<{ messages: object[] }>();
    expect(rest.messages).toHaveLength(24);
    const reset = await (await api("/api/messages?after=999999", "GET", undefined, a.token)).json<{ reset: boolean; messages: object[] }>();
    expect(reset.reset).toBe(true);
    expect(reset.messages).toHaveLength(50);
  });
  it("stops daily requests and sends instead of upgrading a plan", async () => {
    const a = await join();
    await roomSQL("UPDATE daily SET messages=1500");
    expect((await api("/api/messages", "POST", { text: "오늘 한도", clientId: "c".repeat(32) }, a.token)).status).toBe(503);
    await roomSQL("UPDATE daily SET requests=30000");
    expect((await api("/api/messages", "GET", undefined, a.token)).status).toBe(503);
    expect((await api("/api/sessions", "POST", { nickname: "새 친구", joinCode: code })).status).toBe(503);
    expect((await api("/health")).status).toBe(200);
  });
  it("limits per-session sends, duplicate retries do not consume the limit", async () => {
    const a = await join();
    await api("/api/messages", "POST", { text: "첫 인사", clientId: "d".repeat(32) }, a.token);
    await roomSQL("UPDATE sessions SET sends=30");
    expect((await api("/api/messages", "POST", { text: "첫 인사", clientId: "d".repeat(32) }, a.token)).status).toBe(200);
    expect((await api("/api/messages", "POST", { text: "너무 빠른 전송", clientId: "e".repeat(32) }, a.token)).status).toBe(429);
  });
  it("serializes simultaneous duplicate names and caps active native sessions", async () => {
    const responses = await Promise.all([1, 2].map(() => api("/api/sessions", "POST", { nickname: "동시 입장", joinCode: code })));
    expect(responses.map(r => r.status).sort()).toEqual([201, 409]);
    for (let i = 0; i < 15; i++) await join("친구 " + i);
    expect((await api("/api/sessions", "POST", { nickname: "초과 친구", joinCode: code })).status).toBe(503);
  });
  it("trims retained history and resets a cursor that has fallen out of it", async () => {
    const { token } = await join();
    await roomSQL("WITH RECURSIVE n(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM n WHERE x<1200) INSERT INTO messages(sid,client_id,nickname,text,sent_at) SELECT 'fixture',printf('%032x',x),'친구','기록','2026-10-17T00:00:00Z' FROM n");
    const first = (await roomSQL("SELECT MIN(id) AS id FROM messages"))[0].id as number;
    await api("/api/messages", "POST", { text: "새 기록", clientId: "f".repeat(32) }, token);
    expect((await roomSQL("SELECT COUNT(*) AS n FROM messages"))[0].n).toBe(1000);
    const page = await (await api(`/api/messages?after=${first}`, "GET", undefined, token)).json<{ reset: boolean }>();
    expect(page.reset).toBe(true);
  });
});
