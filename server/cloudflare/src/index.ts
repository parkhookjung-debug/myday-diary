import { DurableObject } from "cloudflare:workers";

// This server speaks protocol 1 used by the native Windows and macOS apps.
// Limits stop service; they are not a substitute for a verified Free account.
const MAX_BODY = 16_384;
const MAX_TEXT = 2_000;
const HISTORY = 50;
const RETAIN = 1_000;
const IDLE_MS = 90_000;
const LIFE_MS = 8 * 60 * 60 * 1_000;
const POLL_MS = 5_000;
const DAILY_REQUESTS = 30_000;
const DAILY_MESSAGES = 1_500;
const DAILY_JOINS = 500;

type Body = { nickname?: string; joinCode?: string; text?: string; clientId?: string };
type Input = { method: string; path: string; token: string; body: Body;
  after: number; wait: number; ip: string };
type Result = { status: number; payload: { error: string } | { ok: boolean }
  | { token: string; nickname: string; room: string; maxText: number }
  | ReturnType<typeof message>
  | { messages: ReturnType<typeof message>[]; users: string[]; reset: boolean; cursor: number; pollAfterMs: number } };
type Session = { hash: string; sid: string; nickname: string; nickkey: string;
  born: number; seen: number; window: number; sends: number; invitation: string };
type MessageRow = { id: number; nickname: string; text: string; sent_at: string; client_id: string };

class APIError extends Error {
  constructor(readonly status: number, message: string) { super(message); }
}
const encoder = new TextEncoder();
async function digest(value: string): Promise<string> {
  const bytes = new Uint8Array(await crypto.subtle.digest("SHA-256", encoder.encode(value)));
  return Array.from(bytes, b => b.toString(16).padStart(2, "0")).join("");
}
async function sameSecret(a: string, b: string): Promise<boolean> {
  const [left, right] = await Promise.all([digest(a), digest(b)]);
  let difference = 0;
  for (let i = 0; i < left.length; i++) difference |= left.charCodeAt(i) ^ right.charCodeAt(i);
  return difference === 0;
}
function json(status: number, payload: Record<string, unknown>): Response {
  return Response.json(payload, { status, headers: {
    "Cache-Control": "no-store", "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "no-referrer",
  } });
}
function message(row: MessageRow) {
  return { id: row.id, nickname: row.nickname, text: row.text, sentAt: row.sent_at, clientId: row.client_id };
}
async function body(request: Request): Promise<Body> {
  if (request.headers.get("content-type")?.split(";")[0].trim() !== "application/json")
    throw new APIError(400, "JSON 요청만 지원해요.");
  if (Number(request.headers.get("content-length") || "0") > MAX_BODY)
    throw new APIError(413, "요청이 너무 커요.");
  const reader = request.body?.getReader();
  if (!reader) throw new APIError(400, "요청 내용이 없어요.");
  const chunks: Uint8Array[] = [];
  let length = 0;
  try {
    for (;;) {
      const part = await reader.read();
      if (part.done) break;
      length += part.value.byteLength;
      if (length > MAX_BODY) { await reader.cancel(); throw new APIError(413, "요청이 너무 커요."); }
      chunks.push(part.value);
    }
  } finally { reader.releaseLock(); }
  const bytes = new Uint8Array(length);
  let offset = 0;
  for (const part of chunks) { bytes.set(part, offset); offset += part.byteLength; }
  try {
    const value: unknown = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
    if (!value || typeof value !== "object" || Array.isArray(value)) throw new Error();
    const fields = value as Record<string, unknown>;
    const field = (name: string) => typeof fields[name] === "string" ? fields[name] : undefined;
    return { nickname: field("nickname"), joinCode: field("joinCode"), text: field("text"), clientId: field("clientId") };
  } catch { throw new APIError(400, "요청 내용이 올바르지 않아요."); }
}

export class ChatRoom extends DurableObject<Env> {
  private attempts = new Map<string, { start: number; count: number }>();
  constructor(ctx: DurableObjectState, env: Env) {
    super(ctx, env);
    this.ctx.storage.sql.exec(`CREATE TABLE IF NOT EXISTS sessions (
      hash TEXT PRIMARY KEY, sid TEXT NOT NULL, nickname TEXT NOT NULL, nickkey TEXT UNIQUE NOT NULL,
      born INTEGER NOT NULL, seen INTEGER NOT NULL, window INTEGER NOT NULL, sends INTEGER NOT NULL,
      invitation TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS messages (
      id INTEGER PRIMARY KEY AUTOINCREMENT, sid TEXT NOT NULL, client_id TEXT NOT NULL,
      nickname TEXT NOT NULL, text TEXT NOT NULL, sent_at TEXT NOT NULL, UNIQUE(sid,client_id));
      CREATE TABLE IF NOT EXISTS daily (
      day TEXT PRIMARY KEY, requests INTEGER NOT NULL, joins INTEGER NOT NULL, messages INTEGER NOT NULL);`);
  }
  private budget() {
    const day = new Date().toISOString().slice(0, 10);
    const sql = this.ctx.storage.sql;
    const row = sql.exec<{ requests: number }>("SELECT requests FROM daily WHERE day=?", day).toArray()[0];
    if (row && row.requests >= DAILY_REQUESTS)
      throw new APIError(503, "오늘의 무료 이용량을 다 썼어요. 한국 시간 오전 9시 이후 다시 이용해 주세요.");
    sql.exec("INSERT INTO daily VALUES(?,1,0,0) ON CONFLICT(day) DO UPDATE SET requests=requests+1", day);
    if (!row) sql.exec("DELETE FROM daily WHERE day<>?", day);
    return day;
  }
  private prune(now: number) {
    this.ctx.storage.sql.exec("DELETE FROM sessions WHERE seen<? OR born<?", now - IDLE_MS, now - LIFE_MS);
  }
  private async session(token: string): Promise<Session> {
    if (!token || token.length > 100) throw new APIError(401, "먼저 채팅방에 입장해 주세요.");
    const hash = await digest(token);
    const invitation = await digest(this.env.MYDAY_CHAT_JOIN_CODE || "");
    const now = Date.now();
    this.prune(now);
    const found = this.ctx.storage.sql.exec<Session>("SELECT * FROM sessions WHERE hash=?", hash).toArray()[0];
    if (!found || found.invitation !== invitation)
      throw new APIError(401, "연결이 만료됐어요. 다시 입장해 주세요.");
    this.ctx.storage.sql.exec("UPDATE sessions SET seen=? WHERE hash=?", now, hash);
    return found;
  }
  private async join(input: Input, day: string): Promise<Result> {
    const now = Date.now();
    for (const [ip, attempt] of this.attempts) if (now - attempt.start >= 60_000) this.attempts.delete(ip);
    if (!this.attempts.has(input.ip)) {
      if (this.attempts.size >= 512) throw new APIError(429, "입장이 몰리고 있어요. 잠시 후 다시 시도해 주세요.");
      this.attempts.set(input.ip, { start: now, count: 0 });
    }
    const attempt = this.attempts.get(input.ip)!;
    if (++attempt.count > 30) throw new APIError(429, "입장을 너무 자주 시도했어요.");
    const expected = this.env.MYDAY_CHAT_JOIN_CODE;
    if (!expected || Array.from(expected).length < 12) throw new APIError(503, "운영자가 초대코드를 설정해야 해요.");
    const { nickname: raw, joinCode } = input.body;
    if (typeof joinCode !== "string" || !await sameSecret(joinCode, expected))
      throw new APIError(403, "초대코드를 확인해 주세요.");
    const nickname = typeof raw === "string" ? raw.normalize("NFKC").trim() : "";
    if (Array.from(nickname).length < 2 || Array.from(nickname).length > 20 || !/^[\p{L}\p{N} _-]+$/u.test(nickname))
      throw new APIError(400, "닉네임은 글자·숫자·공백·밑줄·하이픈으로 2~20자예요.");
    const token = crypto.randomUUID().replaceAll("-", "") + crypto.randomUUID().replaceAll("-", "");
    const [hash, invitation] = await Promise.all([digest(token), digest(expected)]);
    const sql = this.ctx.storage.sql;
    // No await between duplicate/capacity checks and insert: another join cannot overtake these checks.
    this.prune(Date.now());
    // Match common Unicode case folds used by the Python server, including sharp-s and final sigma.
    const key = nickname.toLowerCase().replaceAll("ß", "ss").replaceAll("ς", "σ");
    if (sql.exec("SELECT hash FROM sessions WHERE nickkey=?", key).toArray().length)
      throw new APIError(409, "이미 접속 중인 닉네임이에요.");
    if (sql.exec<{ n: number }>("SELECT COUNT(*) AS n FROM sessions").one().n >= 16)
      throw new APIError(503, "무료 테스트 채팅방이 가득 찼어요.");
    if (sql.exec<{ joins: number }>("SELECT joins FROM daily WHERE day=?", day).one().joins >= DAILY_JOINS)
      throw new APIError(503, "오늘의 무료 입장 횟수를 다 썼어요.");
    this.ctx.storage.transactionSync(() => {
      sql.exec("INSERT INTO sessions VALUES(?,?,?,?,?,?,?,?,?)", hash, crypto.randomUUID(), nickname, key,
        Date.now(), Date.now(), Date.now(), 0, invitation);
      sql.exec("UPDATE daily SET joins=joins+1 WHERE day=?", day);
    });
    return { status: 201, payload: { token, nickname, room: "상몬 라운지", maxText: MAX_TEXT } };
  }
  private send(session: Session, input: Input, day: string): Result {
    const { text: raw, clientId } = input.body;
    if (typeof raw !== "string" || !raw.trim() || Array.from(raw).length > MAX_TEXT)
      throw new APIError(400, "메시지는 공백을 제외한 내용이 있고 2,000자 이하여야 해요.");
    const normalized = raw.replace(/\r\n?/g, "\n");
    if (/[\p{Cc}\p{Cs}]/u.test(normalized.replace(/[\n\t]/g, "")) || /[\u202a-\u202e\u2066-\u2069]/u.test(normalized))
      throw new APIError(400, "메시지에 표시할 수 없는 문자가 있어요.");
    const text = normalized.trim();
    if (typeof clientId !== "string" || !/^[a-f0-9]{32}$/.test(clientId))
      throw new APIError(400, "메시지 전송 ID가 올바르지 않아요.");
    const sql = this.ctx.storage.sql;
    const previous = sql.exec<MessageRow>("SELECT * FROM messages WHERE sid=? AND client_id=?", session.sid, clientId).toArray()[0];
    if (previous) {
      if (previous.text !== text) throw new APIError(409, "같은 전송 ID의 내용이 달라요.");
      return { status: 200, payload: message(previous) };
    }
    const now = Date.now();
    const sends = now - session.window >= 60_000 ? 0 : session.sends;
    if (sends >= 30) throw new APIError(429, "메시지는 1분에 30개까지 보낼 수 있어요.");
    if (sql.exec<{ messages: number }>("SELECT messages FROM daily WHERE day=?", day).one().messages >= DAILY_MESSAGES)
      throw new APIError(503, "오늘의 무료 메시지 수를 다 썼어요.");
    const row = this.ctx.storage.transactionSync(() => {
      const saved = sql.exec<MessageRow>("INSERT INTO messages(sid,client_id,nickname,text,sent_at) VALUES(?,?,?,?,?) RETURNING *",
        session.sid, clientId, session.nickname, text, new Date().toISOString()).one();
      sql.exec("UPDATE sessions SET window=?,sends=? WHERE hash=?", sends === 0 ? now : session.window, sends + 1, session.hash);
      sql.exec("UPDATE daily SET messages=messages+1 WHERE day=?", day);
      sql.exec("DELETE FROM messages WHERE id<=?", saved.id - RETAIN);
      return saved;
    });
    return { status: 200, payload: message(row) };
  }
  private poll(input: Input): Result {
    const sql = this.ctx.storage.sql;
    // Two index lookups avoid scanning the retained history on every poll.
    const oldest = sql.exec<{ id: number }>("SELECT id FROM messages ORDER BY id LIMIT 1").toArray()[0]?.id ?? 0;
    const latest = sql.exec<{ id: number }>("SELECT id FROM messages ORDER BY id DESC LIMIT 1").toArray()[0]?.id ?? 0;
    const range = { oldest, latest };
    const reset = input.after > range.latest || (input.after > 0 && input.after < range.oldest - 1);
    const rows = input.after === 0 || reset
      ? sql.exec<MessageRow>("SELECT * FROM messages ORDER BY id DESC LIMIT ?", HISTORY).toArray().reverse()
      : sql.exec<MessageRow>("SELECT * FROM messages WHERE id>? ORDER BY id LIMIT 100", input.after).toArray();
    const users = sql.exec<{ nickname: string }>("SELECT nickname FROM sessions ORDER BY nickname").toArray().map(r => r.nickname);
    return { status: 200, payload: { messages: rows.map(message), users, reset,
      cursor: rows.at(-1)?.id ?? (reset ? 0 : input.after), pollAfterMs: POLL_MS } };
  }
  async request(input: Input): Promise<Result> {
    try {
      const day = this.budget();
      if (input.method === "POST" && input.path === "/api/sessions") return await this.join(input, day);
      const session = await this.session(input.token);
      if (input.method === "DELETE") {
        this.ctx.storage.sql.exec("DELETE FROM sessions WHERE hash=?", session.hash);
        return { status: 200, payload: { ok: true } };
      }
      if (input.method === "POST") return this.send(session, input, day);
      return this.poll(input);
    } catch (error) {
      if (error instanceof APIError) return { status: error.status, payload: { error: error.message } };
      // Do not log invitation codes, bearer tokens or message bodies.
      return { status: 503, payload: { error: "무료 서버를 잠시 사용할 수 없어요. 잠시 후 다시 시도해 주세요." } };
    }
  }
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    try {
      const url = new URL(request.url);
      if (request.method === "GET" && url.pathname === "/health") return json(200, { ok: true, protocol: 1, hosting: "free-only" });
      const valid = (request.method === "POST" && ["/api/sessions", "/api/messages"].includes(url.pathname))
        || (request.method === "GET" && url.pathname === "/api/messages")
        || (request.method === "DELETE" && url.pathname === "/api/session");
      if (!valid) throw new APIError(404, "없는 채팅 API예요.");
      const origin = request.headers.get("origin");
      if (origin && origin !== url.origin) throw new APIError(403, "다른 사이트의 요청은 허용하지 않아요.");
      const after = Number(url.searchParams.get("after") || "0");
      const wait = Number(url.searchParams.get("wait") || "20");
      if (!Number.isSafeInteger(after) || after < 0 || !Number.isInteger(wait) || wait < 0 || wait > 20)
        throw new APIError(400, "메시지 조회 범위가 올바르지 않아요.");
      const auth = request.headers.get("authorization") || "";
      const input: Input = { method: request.method, path: url.pathname,
        token: auth.startsWith("Bearer ") && auth.length <= 200 ? auth.slice(7) : "",
        body: request.method === "POST" ? await body(request) : {}, after, wait,
        ip: request.headers.get("CF-Connecting-IP") || "local" };
      // A fixed room name prevents untrusted callers allocating arbitrary storage objects.
      const result = await env.ROOMS.getByName("sangmon-lounge-v1").request(input);
      return json(result.status, result.payload);
    } catch (error) {
      if (error instanceof APIError) return json(error.status, { error: error.message });
      return json(503, { error: "무료 서버 한도 또는 연결 상태를 확인해 주세요." });
    }
  },
} satisfies ExportedHandler<Env>;
