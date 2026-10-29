"""Kakao authenticated, opt-in diary snapshots. Python 3.11+, no extra packages."""
from __future__ import annotations

import base64
import binascii
import datetime as dt
import json
import os
import re
import signal
import sqlite3
import threading
import time
from collections import defaultdict
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urlsplit
from urllib.request import Request, build_opener, HTTPRedirectHandler

MAX_BODY = 20 * 1024 * 1024
MAX_PHOTO = 2 * 1024 * 1024
MAX_OWNER_STORAGE = 128 * 1024 * 1024
KINDS = {"text", "todo", "habit", "emotion", "photo"}


class APIError(Exception):
    def __init__(self, status: int, message: str):
        self.status, self.message = status, message


@dataclass(frozen=True)
class Profile:
    id: str
    nickname: str


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class KakaoGateway:
    def __init__(self, app_id: str):
        self.app_id = app_id
        self.opener = build_opener(NoRedirect())

    def get(self, path: str, token: str, params: dict | None = None) -> dict:
        url = "https://kapi.kakao.com" + path
        if params:
            url += "?" + urlencode(params)
        request = Request(url, headers={"Authorization": "Bearer " + token})
        try:
            with self.opener.open(request, timeout=12) as response:
                raw = response.read(2 * 1024 * 1024 + 1)
            if len(raw) > 2 * 1024 * 1024:
                raise APIError(502, "카카오 응답을 확인하지 못했어요.")
            value = json.loads(raw)
            if not isinstance(value, dict):
                raise ValueError()
            return value
        except HTTPError as error:
            if error.code == 401:
                raise APIError(401, "카카오 로그인이 만료됐어요. 다시 로그인해 주세요.") from None
            if error.code == 403:
                raise APIError(403, "카카오 친구 목록 권한을 확인해 주세요.") from None
            raise APIError(502, "카카오 연결이 원활하지 않아요. 잠시 후 다시 시도해 주세요.") from None
        except (URLError, TimeoutError, ValueError, OSError):
            raise APIError(502, "카카오 연결이 원활하지 않아요. 잠시 후 다시 시도해 주세요.") from None

    def authenticate(self, token: str) -> Profile:
        if not re.fullmatch(r"[0-9]+", self.app_id or ""):
            raise APIError(503, "운영자가 카카오 앱 연결을 준비하고 있어요.")
        info = self.get("/v1/user/access_token_info", token)
        if str(info.get("app_id")) != self.app_id or not isinstance(info.get("expires_in"), (int, float)) or info["expires_in"] <= 0:
            raise APIError(401, "이 앱의 카카오 계정으로 다시 로그인해 주세요.")
        user = self.get("/v2/user/me", token)
        identity = str(user.get("id", ""))
        if not re.fullmatch(r"[0-9]{1,32}", identity) or identity != str(info.get("id")):
            raise APIError(401, "카카오 계정을 확인하지 못했어요.")
        nickname = user.get("kakao_account", {}).get("profile", {}).get("nickname") or user.get("properties", {}).get("nickname") or "마이데이 친구"
        nickname = "".join(c for c in str(nickname) if c.isprintable()).strip()[:40] or "마이데이 친구"
        return Profile(identity, nickname)

    def friends(self, token: str) -> list[Profile]:
        # Never trust friend IDs sent by the Android client. Refresh the actual
        # Kakao relationship for every read, including direct post links.
        found: dict[str, Profile] = {}
        offset = 0
        for _ in range(50):
            page = self.get("/v1/api/talk/friends", token, {"offset": offset, "limit": 100, "order": "asc"})
            elements = page.get("elements")
            total = page.get("total_count")
            if not isinstance(elements, list) or not isinstance(total, int) or not 0 <= total <= 5000:
                raise APIError(502, "친구 목록을 확인하지 못했어요.")
            for item in elements:
                identity = str(item.get("id", ""))
                if re.fullmatch(r"[0-9]{1,32}", identity):
                    found[identity] = Profile(identity, str(item.get("profile_nickname") or "마이데이 친구")[:40])
            offset += len(elements)
            if offset >= total:
                return list(found.values())
            if not elements:
                break
        raise APIError(502, "친구 목록을 끝까지 확인하지 못했어요. 다시 시도해 주세요.")


def text(value, limit: int, label: str) -> str:
    if not isinstance(value, str) or len(value) > limit or any(ord(c) < 32 and c not in "\n\t\r" for c in value):
        raise APIError(400, f"{label} 내용을 확인해 주세요.")
    return value


def jpeg_dimensions(data: bytes) -> tuple[int, int]:
    if not data.startswith(b"\xff\xd8\xff") or not data.endswith(b"\xff\xd9"):
        raise APIError(400, "사진 형식을 확인해 주세요.")
    position = 2
    while position + 4 < len(data):
        if data[position] != 255:
            break
        while position < len(data) and data[position] == 255:
            position += 1
        if position >= len(data):
            break
        marker = data[position]
        position += 1
        if marker in (0xD8, 0xD9, 0xDA):
            break
        length = int.from_bytes(data[position:position + 2], "big")
        if length < 2 or position + length > len(data):
            break
        if marker in (0xC0, 0xC1, 0xC2):
            if length < 8:
                break
            height = int.from_bytes(data[position + 3:position + 5], "big")
            width = int.from_bytes(data[position + 5:position + 7], "big")
            if 1 <= width <= 1600 and 1 <= height <= 1600:
                return width, height
            break
        position += length
    raise APIError(400, "사진은 앱에서 다시 선택해 주세요.")


def snapshot(value: dict) -> dict:
    if not isinstance(value, dict) or set(value) - {"title", "mood", "blocks"}:
        raise APIError(400, "공개할 내용만 선택해 주세요.")
    title = text(value.get("title", ""), 80, "제목").strip()
    mood = text(value.get("mood", ""), 40, "기분")
    blocks = value.get("blocks")
    if not isinstance(blocks, list) or not 1 <= len(blocks) <= 30:
        raise APIError(400, "공개할 기록을 1~30개 선택해 주세요.")
    result = []
    seen = set()
    photos = 0
    for block in blocks:
        if not isinstance(block, dict) or set(block) - {"id", "type", "title", "text", "checked", "photo"}:
            raise APIError(400, "공개할 기록 형식을 확인해 주세요.")
        identity = text(block.get("id"), 128, "기록 ID")
        if not identity or identity in seen or block.get("type") not in KINDS:
            raise APIError(400, "공개할 기록 형식을 확인해 주세요.")
        seen.add(identity)
        content = text(block.get("text", ""), 10000, "공개 글(최대 10,000자)")
        heading = text(block.get("title", ""), 80, "기록 제목")
        checked = block.get("checked", False)
        if not isinstance(checked, bool):
            raise APIError(400, "체크 항목을 확인해 주세요.")
        public = {"id": identity, "type": block["type"], "title": heading, "text": content, "checked": checked}
        photo = block.get("photo")
        if photo is not None:
            if block["type"] != "photo" or not isinstance(photo, str) or len(photo) > ((MAX_PHOTO + 2) // 3) * 4:
                raise APIError(400, "사진은 한 장에 2MB 이하여야 해요.")
            try:
                image = base64.b64decode(photo, validate=True)
            except (ValueError, binascii.Error):
                raise APIError(400, "사진을 읽지 못했어요.") from None
            if len(image) > MAX_PHOTO:
                raise APIError(400, "사진은 한 장에 2MB 이하여야 해요.")
            jpeg_dimensions(image)
            public["photo"] = photo
            photos += 1
        elif block["type"] == "photo":
            raise APIError(400, "비어 있는 사진은 공개할 수 없어요.")
        if not content.strip() and not photo and not checked:
            raise APIError(400, "내용이 있는 기록을 선택해 주세요.")
        result.append(public)
    if photos > 6:
        raise APIError(400, "한 번에 사진 6장까지 공개할 수 있어요.")
    return {"title": title, "mood": mood, "blocks": result}


def valid_date(value: str) -> str:
    try:
        if not re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}", value):
            raise ValueError()
        dt.date.fromisoformat(value)
        return value
    except ValueError:
        raise APIError(400, "일기 날짜를 확인해 주세요.") from None


class FriendsService:
    def __init__(self, database: Path, gateway: KakaoGateway):
        database.parent.mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(database, check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.lock = threading.RLock()
        self.gateway = gateway
        with self.db:
            self.db.executescript("""
              PRAGMA foreign_keys=ON;
              CREATE TABLE IF NOT EXISTS profiles(id TEXT PRIMARY KEY, nickname TEXT NOT NULL);
              CREATE TABLE IF NOT EXISTS entries(owner TEXT NOT NULL, date TEXT NOT NULL,
                updated TEXT NOT NULL, payload TEXT NOT NULL, PRIMARY KEY(owner,date),
                FOREIGN KEY(owner) REFERENCES profiles(id) ON DELETE CASCADE);
            """)

    @staticmethod
    def summary(row) -> dict:
        content = json.loads(row["payload"])
        return {"ownerId": row["owner"], "nickname": row["nickname"], "date": row["date"],
                "updatedAt": row["updated"], "title": content["title"], "mood": content["mood"],
                "blockCount": len(content["blocks"]), "photoCount": sum("photo" in b for b in content["blocks"])}

    def handle(self, method: str, path: str, token: str, payload=None) -> tuple[int, dict]:
        if method == "GET" and path == "/health":
            return 200, {"ok": True, "service": "myday-friends", "ready": bool(re.fullmatch(r"[0-9]+", self.gateway.app_id or ""))}
        if not token or len(token) > 4096 or any(c.isspace() for c in token):
            raise APIError(401, "카카오 로그인이 필요해요.")
        user = self.gateway.authenticate(token)
        if method == "GET" and path == "/v1/me":
            return 200, {"id": user.id, "nickname": user.nickname}
        if method == "DELETE" and path == "/v1/account":
            with self.lock, self.db:
                self.db.execute("DELETE FROM profiles WHERE id=?", (user.id,))
            return 200, {"ok": True}
        if path.startswith("/v1/diary/") and method in {"PUT", "DELETE"}:
            date = valid_date(path[len("/v1/diary/"):])
            if method == "DELETE":
                with self.lock, self.db:
                    self.db.execute("DELETE FROM entries WHERE owner=? AND date=?", (user.id, date))
                return 200, {"ok": True}
            public = snapshot(payload)
            encoded = json.dumps(public, ensure_ascii=False, separators=(",", ":"))
            with self.lock, self.db:
                used = self.db.execute("SELECT COALESCE(SUM(length(CAST(payload AS BLOB))),0) FROM entries WHERE owner=? AND date<>?", (user.id, date)).fetchone()[0]
                count = self.db.execute("SELECT COUNT(*) FROM entries WHERE owner=? AND date<>?", (user.id, date)).fetchone()[0]
                if used + len(encoded.encode()) > MAX_OWNER_STORAGE or count >= 3660:
                    raise APIError(413, "공개 보관함이 가득 찼어요. 이전 공개 글을 정리해 주세요.")
                updated = dt.datetime.now(dt.timezone.utc).isoformat()
                self.db.execute("INSERT INTO profiles VALUES(?,?) ON CONFLICT(id) DO UPDATE SET nickname=excluded.nickname", (user.id, user.nickname))
                self.db.execute("INSERT INTO entries VALUES(?,?,?,?) ON CONFLICT(owner,date) DO UPDATE SET updated=excluded.updated,payload=excluded.payload", (user.id, date, updated, encoded))
            return 200, {"ok": True, "date": date, "updatedAt": updated}
        if method == "GET" and path in {"/v1/mine", "/v1/feed", "/v1/friends"}:
            friends = [] if path == "/v1/mine" else self.gateway.friends(token)
            ids = [user.id] if path == "/v1/mine" else [p.id for p in friends if p.id != user.id]
            with self.lock:
                if path == "/v1/friends":
                    counts = dict(self.db.execute("SELECT owner,COUNT(*) FROM entries GROUP BY owner").fetchall())
                    return 200, {"friends": [{"id": p.id, "nickname": p.nickname, "entryCount": counts.get(p.id, 0)} for p in friends]}
                rows = [] if not ids else self.db.execute("SELECT e.*,p.nickname FROM entries e JOIN profiles p ON p.id=e.owner WHERE e.owner IN (" + ",".join("?" for _ in ids) + ") ORDER BY e.date DESC,e.updated DESC LIMIT ?", ids + [3660 if path == "/v1/mine" else 100]).fetchall()
                return 200, {"entries": [self.summary(r) for r in rows]}
        friend_page = re.fullmatch(r"/v1/friends/([0-9]{1,32})/entries", path)
        if method == "GET" and friend_page:
            owner = friend_page.group(1)
            if owner not in {p.id for p in self.gateway.friends(token)}:
                raise APIError(404, "이 친구의 공개 일기를 볼 수 없어요.")
            with self.lock:
                rows = self.db.execute("SELECT e.*,p.nickname FROM entries e JOIN profiles p ON p.id=e.owner WHERE e.owner=? ORDER BY e.date DESC LIMIT 3660", (owner,)).fetchall()
                return 200, {"entries": [self.summary(r) for r in rows]}
        match = re.fullmatch(r"/v1/posts/([0-9]{1,32})/([0-9-]{10})", path)
        if method == "GET" and match:
            owner, date = match.groups()
            valid_date(date)
            if owner != user.id and owner not in {p.id for p in self.gateway.friends(token)}:
                raise APIError(404, "공개된 일기를 볼 수 없어요.")
            with self.lock:
                row = self.db.execute("SELECT e.*,p.nickname FROM entries e JOIN profiles p ON p.id=e.owner WHERE e.owner=? AND e.date=?", (owner, date)).fetchone()
                if not row:
                    raise APIError(404, "공개된 일기를 볼 수 없어요.")
                return 200, self.summary(row) | json.loads(row["payload"])
        raise APIError(404, "요청한 기능을 찾지 못했어요.")


class FriendsServer(ThreadingHTTPServer):
    # Bound peak memory for photo uploads on a small hosting instance. Graceful
    # shutdown waits for the active requests before closing the SQLite handle.
    daemon_threads = False
    block_on_close = True

    def __init__(self, address, service: FriendsService):
        self.service = service
        self.attempts = defaultdict(list)
        self.rate_lock = threading.Lock()
        self.slots = threading.BoundedSemaphore(4)
        super().__init__(address, Handler)

    def process_request(self, request, client_address):
        if not self.slots.acquire(blocking=False):
            response = json.dumps({"error": "접속이 몰리고 있어요. 잠시 후 다시 시도해 주세요."}).encode()
            try:
                request.sendall(b"HTTP/1.1 503 Service Unavailable\r\nContent-Type: application/json\r\nConnection: close\r\nContent-Length: " + str(len(response)).encode() + b"\r\n\r\n" + response)
            finally:
                self.shutdown_request(request)
            return
        try:
            super().process_request(request, client_address)
        except BaseException:
            self.slots.release()
            raise

    def process_request_thread(self, request, client_address):
        try:
            super().process_request_thread(request, client_address)
        finally:
            self.slots.release()

    def allowed(self, ip: str) -> bool:
        now = time.monotonic()
        with self.rate_lock:
            if len(self.attempts) > 2048:
                self.attempts = defaultdict(list, {k: v for k, v in self.attempts.items() if v and v[-1] > now - 60})
                if ip not in self.attempts and len(self.attempts) > 2048:
                    return False
            values = self.attempts[ip] = [t for t in self.attempts[ip] if t > now - 60]
            if len(values) >= 120:
                return False
            values.append(now)
            return True


class Handler(BaseHTTPRequestHandler):
    server: FriendsServer

    def log_message(self, format, *args):
        pass  # Never log diary contents or Kakao access tokens.

    def process(self):
        self.connection.settimeout(20)
        try:
            if not self.server.allowed(self.client_address[0]):
                raise APIError(429, "잠시 후 다시 시도해 주세요.")
            parsed = urlsplit(self.path)
            if parsed.query or parsed.fragment:
                raise APIError(400, "요청 주소를 확인해 주세요.")
            payload = None
            if self.command == "PUT":
                if self.headers.get("Transfer-Encoding"):
                    raise APIError(400, "요청 크기를 확인해 주세요.")
                if self.headers.get("Content-Type", "").split(";")[0].strip() != "application/json":
                    raise APIError(415, "JSON 요청이 필요해요.")
                try:
                    length = int(self.headers.get("Content-Length", "-1"))
                except ValueError:
                    raise APIError(400, "요청 크기를 확인해 주세요.") from None
                if not 0 < length <= MAX_BODY:
                    raise APIError(413, "한 번에 공개하는 사진과 글의 크기를 줄여 주세요.")
                try:
                    raw = self.rfile.read(length)
                    if len(raw) != length:
                        raise ValueError()
                    payload = json.loads(raw)
                except (ValueError, UnicodeDecodeError):
                    raise APIError(400, "요청 내용을 확인해 주세요.") from None
            header = self.headers.get("Authorization", "")
            token = header[7:] if header.startswith("Bearer ") else ""
            status, value = self.server.service.handle(self.command, parsed.path, token, payload)
        except APIError as error:
            status, value = error.status, {"error": error.message}
        except Exception:
            status, value = 503, {"error": "지금은 처리하지 못했어요. 다시 시도해 주세요."}
        data = json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode()
        try:
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Connection", "close")
            self.end_headers()
            self.wfile.write(data)
        except (OSError, TimeoutError):
            pass
        self.close_connection = True

    do_GET = do_PUT = do_DELETE = process


if __name__ == "__main__":
    os.umask(0o077)
    location = Path(os.environ.get("MYDAY_FRIENDS_DB", str(Path(__file__).parent / ".local/friends.sqlite3")))
    service = FriendsService(location, KakaoGateway(os.environ.get("MYDAY_KAKAO_APP_ID", "")))
    address = (os.environ.get("HOST", "127.0.0.1"), int(os.environ.get("PORT", "8787")))
    print(f"MyDay friends server: {address[0]}:{address[1]} (Kakao app configured: {bool(service.gateway.app_id)})", flush=True)
    server = FriendsServer(address, service)
    def stop(_signum, _frame):
        threading.Thread(target=server.shutdown, daemon=True).start()
    signal.signal(signal.SIGTERM, stop)
    signal.signal(signal.SIGINT, stop)
    try:
        server.serve_forever()
    finally:
        server.server_close()
        service.db.close()
