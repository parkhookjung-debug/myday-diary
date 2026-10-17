"""MyDay invitation-only chat API. Python standard library, no diary access."""
from __future__ import annotations

import argparse
from collections import defaultdict, deque
from contextlib import closing
import hmac
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import secrets
import sqlite3
import threading
import time
import unicodedata
from urllib.parse import parse_qs, urlsplit

MAX_TEXT = 2000
MAX_BODY = 16384
HISTORY = 50
MAX_MESSAGES = 10000
SESSION_IDLE = 90
SESSION_LIFETIME = 8 * 60 * 60


class APIError(Exception):
    def __init__(self, status: int, message: str):
        self.status, self.message = status, message


class ChatState:
    def __init__(self, database: Path, join_code: str):
        if len(join_code) < 12:
            raise ValueError("초대코드는 12자 이상이어야 합니다.")
        self.database, self.join_code = database, join_code
        database.parent.mkdir(parents=True, exist_ok=True)
        self.condition = threading.Condition(threading.RLock())
        self.sessions: dict[str, dict] = {}
        self.join_attempts: dict[str, deque] = defaultdict(deque)
        self.poll_slots = threading.BoundedSemaphore(48)
        with closing(self.connect()) as db:
            db.execute("PRAGMA journal_mode=WAL")
            db.execute("""CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT, session_id TEXT NOT NULL,
                client_id TEXT NOT NULL, nickname TEXT NOT NULL, text TEXT NOT NULL,
                sent_at TEXT NOT NULL, UNIQUE(session_id, client_id))""")
            db.commit()

    def connect(self):
        return sqlite3.connect(self.database, timeout=5)

    def prune(self):
        now = time.monotonic()
        stale = [key for key, s in self.sessions.items()
                 if now - s["seen"] > SESSION_IDLE or now - s["born"] > SESSION_LIFETIME]
        for key in stale:
            del self.sessions[key]
        # Bound rate-limit bookkeeping as well as active sessions.
        for ip in list(self.join_attempts):
            queue = self.join_attempts[ip]
            while queue and now - queue[0] >= 60:
                queue.popleft()
            if not queue:
                del self.join_attempts[ip]

    def join(self, nickname, code, ip):
        with self.condition:
            self.prune()
            attempts = self.join_attempts[ip]
            if len(attempts) >= 30:
                raise APIError(429, "입장을 너무 자주 시도했어요. 잠시 후 다시 시도해 주세요.")
            attempts.append(time.monotonic())
            if not isinstance(code, str) or not hmac.compare_digest(code.encode(), self.join_code.encode()):
                raise APIError(403, "초대코드를 확인해 주세요.")
            if not isinstance(nickname, str):
                raise APIError(400, "닉네임을 입력해 주세요.")
            nickname = unicodedata.normalize("NFKC", nickname).strip()
            if not 2 <= len(nickname) <= 20 or not all(c.isalnum() or c in " _-" for c in nickname):
                raise APIError(400, "닉네임은 글자·숫자·공백·밑줄·하이픈으로 2~20자예요.")
            if len(self.sessions) >= 64:
                raise APIError(503, "채팅방이 가득 찼어요. 잠시 후 다시 들어와 주세요.")
            if any(s["nickname"].casefold() == nickname.casefold() for s in self.sessions.values()):
                raise APIError(409, "이미 접속 중인 닉네임이에요.")
            token = secrets.token_urlsafe(32)
            self.sessions[token] = dict(nickname=nickname, id=secrets.token_hex(16),
                                       born=time.monotonic(), seen=time.monotonic(), sends=deque())
            self.condition.notify_all()
            return dict(token=token, nickname=nickname, room="상몬 라운지", maxText=MAX_TEXT)

    def session(self, token):
        self.prune()
        session = self.sessions.get(token)
        if session is None:
            raise APIError(401, "연결이 만료됐어요. 다시 입장해 주세요.")
        session["seen"] = time.monotonic()
        return session

    def leave(self, token):
        with self.condition:
            self.session(token)
            del self.sessions[token]
            self.condition.notify_all()
        return dict(ok=True)

    @staticmethod
    def message(row):
        return dict(id=row[0], nickname=row[1], text=row[2], sentAt=row[3], clientId=row[4])

    def send(self, token, text, client_id):
        if not isinstance(text, str) or not text.strip() or len(text) > MAX_TEXT:
            raise APIError(400, "메시지는 공백을 제외한 내용이 있고 2,000자 이하여야 해요.")
        text = text.replace("\r\n", "\n").replace("\r", "\n")
        if any((unicodedata.category(c) in ("Cc", "Cs") and c not in "\n\t") or c in "\u202a\u202b\u202c\u202d\u202e\u2066\u2067\u2068\u2069" for c in text):
            raise APIError(400, "메시지에 표시할 수 없는 문자가 있어요.")
        if not isinstance(client_id, str) or not re.fullmatch(r"[a-f0-9]{32}", client_id):
            raise APIError(400, "메시지 전송 ID가 올바르지 않아요.")
        text = text.strip()
        with self.condition:
            session = self.session(token)
            with closing(self.connect()) as db:
                row = db.execute("SELECT id,nickname,text,sent_at,client_id FROM messages WHERE session_id=? AND client_id=?",
                                 (session["id"], client_id)).fetchone()
                if row:
                    if row[2] != text:
                        raise APIError(409, "같은 전송 ID의 내용이 달라요.")
                    return self.message(row)
                now = time.monotonic()
                while session["sends"] and now - session["sends"][0] >= 60:
                    session["sends"].popleft()
                if len(session["sends"]) >= 30:
                    raise APIError(429, "메시지는 1분에 30개까지 보낼 수 있어요.")
                sent_at = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                cursor = db.execute("INSERT INTO messages(session_id,client_id,nickname,text,sent_at) VALUES(?,?,?,?,?)",
                                    (session["id"], client_id, session["nickname"], text, sent_at))
                db.execute("DELETE FROM messages WHERE id <= (SELECT MAX(id) FROM messages)-?", (MAX_MESSAGES,))
                db.commit()
                session["sends"].append(now)
                row = (cursor.lastrowid, session["nickname"], text, sent_at, client_id)
            self.condition.notify_all()
            return self.message(row)

    def poll(self, token, after, wait):
        if not self.poll_slots.acquire(blocking=False):
            raise APIError(503, "서버가 잠시 바빠요. 잠시 후 다시 시도해 주세요.")
        try:
            deadline = time.monotonic() + wait
            with self.condition:
                while True:
                    self.session(token)
                    with closing(self.connect()) as db:
                        latest = db.execute("SELECT COALESCE(MAX(id),0) FROM messages").fetchone()[0]
                        oldest = db.execute("SELECT COALESCE(MIN(id),0) FROM messages").fetchone()[0]
                        reset = after > latest or (after > 0 and after < oldest - 1)
                        if after == 0 or reset:
                            rows = db.execute("SELECT id,nickname,text,sent_at,client_id FROM messages ORDER BY id DESC LIMIT ?", (HISTORY,)).fetchall()[::-1]
                        else:
                            rows = db.execute("SELECT id,nickname,text,sent_at,client_id FROM messages WHERE id>? ORDER BY id LIMIT 100", (after,)).fetchall()
                    remaining = deadline - time.monotonic()
                    if rows or reset or remaining <= 0:
                        return dict(messages=[self.message(r) for r in rows],
                                    cursor=rows[-1][0] if rows else (0 if reset else after), reset=reset,
                                    users=sorted(s["nickname"] for s in self.sessions.values()))
                    self.condition.wait(remaining)
        finally:
            self.poll_slots.release()


class ChatHTTPServer(ThreadingHTTPServer):
    daemon_threads = True
    allow_reuse_address = True

    def __init__(self, address, state):
        self.state = state
        # Bound slow/request-handler threads before accepting a socket into a handler.
        self.handlers = threading.BoundedSemaphore(80)
        super().__init__(address, Handler)

    def process_request(self, request, client_address):
        if not self.handlers.acquire(blocking=False):
            self.shutdown_request(request)
            return
        try:
            super().process_request(request, client_address)
        except Exception:
            self.handlers.release()
            raise

    def process_request_thread(self, request, client_address):
        try:
            super().process_request_thread(request, client_address)
        finally:
            self.handlers.release()


class Handler(BaseHTTPRequestHandler):
    server_version = "MyDayChat/1"

    def setup(self):
        super().setup()
        self.connection.settimeout(30)

    def log_message(self, format, *args):
        pass  # Request headers, invitation codes and message bodies never go into logs.

    def reply(self, status, payload):
        raw = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.end_headers()
        self.wfile.write(raw)

    def body(self):
        if self.headers.get("Transfer-Encoding") or self.headers.get_content_type() != "application/json":
            raise APIError(400, "JSON 요청만 지원해요.")
        try:
            size = int(self.headers.get("Content-Length", "-1"))
        except ValueError:
            raise APIError(400, "요청 크기가 올바르지 않아요.")
        if not 0 <= size <= MAX_BODY:
            raise APIError(413, "요청이 너무 커요.")
        try:
            raw = self.rfile.read(size)
            if len(raw) != size:
                raise ValueError()
            value = json.loads(raw)
            if not isinstance(value, dict):
                raise ValueError()
            return value
        except (ValueError, UnicodeError, RecursionError):
            raise APIError(400, "요청 내용이 올바르지 않아요.")

    def token(self):
        auth = self.headers.get("Authorization", "")
        if not auth.startswith("Bearer ") or len(auth) > 200:
            raise APIError(401, "먼저 채팅방에 입장해 주세요.")
        return auth[7:]

    def route(self):
        parsed = urlsplit(self.path)
        state = self.server.state
        if self.command == "GET" and parsed.path == "/health":
            return 200, dict(ok=True, protocol=1)
        if self.command == "POST" and parsed.path == "/api/sessions":
            data = self.body()
            return 201, state.join(data.get("nickname"), data.get("joinCode"), self.client_address[0])
        if self.command == "DELETE" and parsed.path == "/api/session":
            return 200, state.leave(self.token())
        if self.command == "POST" and parsed.path == "/api/messages":
            token = self.token()
            data = self.body()
            return 200, state.send(token, data.get("text"), data.get("clientId"))
        if self.command == "GET" and parsed.path == "/api/messages":
            token = self.token()
            query = parse_qs(parsed.query)
            try:
                after = int(query.get("after", ["0"])[0])
                wait = int(query.get("wait", ["20"])[0])
                if not 0 <= after <= 2**63 - 1 or not 0 <= wait <= 20:
                    raise ValueError()
            except (ValueError, OverflowError):
                raise APIError(400, "메시지 조회 범위가 올바르지 않아요.")
            return 200, state.poll(token, after, wait)
        raise APIError(404, "없는 채팅 API예요.")

    def handle_api(self):
        try:
            try:
                status, payload = self.route()
            except APIError as error:
                status, payload = error.status, dict(error=error.message)
            except (ConnectionError, TimeoutError, OSError):
                raise
            except Exception:
                status, payload = 500, dict(error="서버에서 요청을 처리하지 못했어요.")
            self.reply(status, payload)
        except (ConnectionError, TimeoutError, OSError):
            pass  # A cancelled long poll can close its socket before the reply.

    do_GET = do_POST = do_DELETE = handle_api


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--data-dir", type=Path, default=Path(__file__).parent / ".local")
    args = parser.parse_args()
    args.data_dir.mkdir(parents=True, exist_ok=True)
    code_file = args.data_dir / "join-code.txt"
    code = os.environ.get("MYDAY_CHAT_JOIN_CODE")
    if code is None:
        if not code_file.exists():
            with code_file.open("x", encoding="utf-8") as output:
                output.write(secrets.token_urlsafe(24))
            code_file.chmod(0o600)
        code = code_file.read_text(encoding="utf-8").strip()
    state = ChatState(args.data_dir / "chat.sqlite3", code)
    server = ChatHTTPServer((args.host, args.port), state)
    print(f"MyDay chat listening on {args.host}:{server.server_port}", flush=True)
    print(f"Invitation: MYDAY_CHAT_JOIN_CODE or {code_file} (not printed)", flush=True)
    try:
        server.serve_forever(poll_interval=0.2)
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
