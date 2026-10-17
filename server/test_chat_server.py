"""Isolated HTTP/SQLite integration tests; never connect to a public chat server."""
import concurrent.futures
from contextlib import closing
import json
from pathlib import Path
import sqlite3
import tempfile
import threading
import time
import unittest
from urllib.error import HTTPError
from urllib.request import Request, build_opener, ProxyHandler

from chat_server import ChatState, ChatHTTPServer, APIError

TEST_CODE = "local-test-code-only"


class ChatTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="myday-chat-test-")
        self.state = ChatState(Path(self.temp.name) / "chat.sqlite3", TEST_CODE)
        self.server = ChatHTTPServer(("127.0.0.1", 0), self.state)
        self.thread = threading.Thread(target=self.server.serve_forever, kwargs={"poll_interval": .02}, daemon=True)
        self.thread.start()
        self.address = f"http://127.0.0.1:{self.server.server_port}"
        self.opener = build_opener(ProxyHandler({}))

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(2)
        self.temp.cleanup()

    def request(self, path, method="GET", data=None, token=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = Request(self.address + path, method=method, headers=headers,
                      data=None if data is None else json.dumps(data).encode())
        with self.opener.open(req, timeout=5) as response:
            return json.load(response)

    def join(self, name="테스트가"):
        return self.request("/api/sessions", "POST", dict(nickname=name, joinCode=TEST_CODE))["token"]

    def send(self, token, text, number=1):
        return self.request("/api/messages", "POST", dict(text=text, clientId=f"{number:032x}"), token)

    def test_invitation_authentication_and_duplicate_nickname(self):
        with self.assertRaises(HTTPError) as e:
            self.request("/api/sessions", "POST", dict(nickname="테스트가", joinCode="wrong"))
        self.assertEqual(403, e.exception.code)
        self.join("Alice")
        with self.assertRaises(HTTPError) as e:
            self.join("Ａｌｉｃｅ")
        self.assertEqual(409, e.exception.code)
        with self.assertRaises(HTTPError) as e:
            self.request("/api/messages?wait=0")
        self.assertEqual(401, e.exception.code)

    def test_two_clients_unicode_newlines_presence_and_idempotent_send(self):
        a, b = self.join("참여자 가"), self.join("참여자 나")
        text = "안녕 👨‍👩‍👦\r\n오늘의 한 줄이에요"
        first = self.send(a, text)
        self.assertEqual(first, self.send(a, text))
        page = self.request("/api/messages?after=0&wait=0", token=b)
        self.assertEqual(["참여자 가", "참여자 나"], page["users"])
        self.assertEqual(1, len(page["messages"]))
        self.assertEqual(text.replace("\r\n", "\n"), page["messages"][0]["text"])
        with self.assertRaises(HTTPError) as e:
            self.send(a, "다른 내용")
        self.assertEqual(409, e.exception.code)

    def test_long_poll_wakes_when_another_client_sends(self):
        a, b = self.join("먼저 입장"), self.join("나중 입장")
        first = self.send(a, "첫 메시지")
        with concurrent.futures.ThreadPoolExecutor() as executor:
            poll = executor.submit(self.request, f"/api/messages?after={first['id']}&wait=3", token=b)
            time.sleep(.05)
            second = self.send(a, "바로 도착해요", 2)
            page = poll.result(timeout=4)
        self.assertEqual([second], page["messages"])

    def test_last_fifty_and_cursor_chunks_do_not_skip_messages(self):
        token = self.join()
        with closing(self.state.connect()) as db:
            for i in range(160):
                db.execute("INSERT INTO messages(session_id,client_id,nickname,text,sent_at) VALUES(?,?,?,?,?)",
                           ("fixture", str(i), "기록 예시", str(i), "2026-10-17T00:00:00Z"))
            db.commit()
        page = self.request("/api/messages?wait=0", token=token)
        self.assertEqual(list(range(111, 161)), [m["id"] for m in page["messages"]])
        part = self.request("/api/messages?after=1&wait=0", token=token)
        self.assertEqual(101, part["cursor"])
        following = self.request(f"/api/messages?after={part['cursor']}&wait=0", token=token)
        self.assertEqual(102, following["messages"][0]["id"])

    def test_restart_keeps_messages_but_requires_new_session(self):
        token = self.join();self.send(token, "재시작 후에도 남아요")
        self.server.state = ChatState(self.state.database, TEST_CODE)
        with self.assertRaises(HTTPError) as e:
            self.request("/api/messages?wait=0", token=token)
        self.assertEqual(401, e.exception.code)
        new_token = self.join()
        self.assertEqual("재시작 후에도 남아요", self.request("/api/messages?wait=0", token=new_token)["messages"][0]["text"])

    def test_leave_expiry_and_cursor_reset(self):
        token = self.join();self.request("/api/session", "DELETE", token=token)
        with self.assertRaises(HTTPError) as e:
            self.request("/api/messages?wait=0", token=token)
        self.assertEqual(401, e.exception.code)
        token = self.join();page = self.request("/api/messages?after=999&wait=0", token=token)
        self.assertTrue(page["reset"]);self.assertEqual(0, page["cursor"])
        with self.state.condition:
            self.state.sessions[token]["seen"] -= 100
        with self.assertRaises(HTTPError):
            self.request("/api/messages?wait=0", token=token)

    def test_message_validation_and_rate_limit_preserve_history(self):
        token = self.join()
        for value in (" ", "a" * 2001, "숨은\x00문자", "가\u202e나"):
            with self.assertRaises(HTTPError) as e:
                self.send(token, value)
            self.assertEqual(400, e.exception.code)
        for i in range(30):
            self.send(token, "메시지", i)
        self.send(token, "메시지", 0)  # An acknowledgement retry is not rate-limited.
        with self.assertRaises(HTTPError) as e:
            self.send(token, "다음 메시지", 31)
        self.assertEqual(429, e.exception.code)
        self.assertEqual(30, len(self.request("/api/messages?wait=0", token=token)["messages"]))

    def test_invalid_query_body_and_join_attempt_limit(self):
        token = self.join()
        for query in ("after=-1", "after=oops", "wait=21"):
            with self.assertRaises(HTTPError) as e:
                self.request("/api/messages?" + query, token=token)
            self.assertEqual(400, e.exception.code)
        with self.assertRaises(HTTPError) as e:
            self.request("/api/messages", "POST", dict(text="x" * 17000), token)
        self.assertEqual(413, e.exception.code)
        for _ in range(29):
            with self.assertRaises(APIError):
                self.state.join("테스트", "wrong", "127.0.0.1")
        with self.assertRaises(APIError) as e:
            self.state.join("다른 이름", TEST_CODE, "127.0.0.1")
        self.assertEqual(429, e.exception.status)


if __name__ == "__main__":
    unittest.main()
