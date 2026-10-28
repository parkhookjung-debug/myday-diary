import copy
import json
import tempfile
import threading
import unittest
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from friends_server import APIError, FriendsServer, FriendsService, KakaoGateway, Profile, snapshot


class FakeKakao:
    app_id = "1234"
    def __init__(self):
        self.people = {"alice": Profile("101", "앨리스"), "bob": Profile("202", "밥"), "eve": Profile("303", "다른 사람")}
        self.links = {"alice": ["bob"], "bob": ["alice"], "eve": []}
        self.calls = 0
    def authenticate(self, token):
        if token not in self.people:
            raise APIError(401, "로그인 필요")
        return self.people[token]
    def friends(self, token):
        self.calls += 1
        return [self.people[key] for key in self.links[token]]


class FriendsServerTest(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.kakao = FakeKakao()
        self.service = FriendsService(Path(self.directory.name) / "test.sqlite3", self.kakao)
        self.server = FriendsServer(("127.0.0.1", 0), self.service)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.base = "http://127.0.0.1:" + str(self.server.server_port)
        self.body = {"title": "산책", "mood": "", "blocks": [{"id": "chosen", "type": "text", "text": "친구에게 공개한 내용", "checked": False, "title": ""}]}

    def tearDown(self):
        self.server.shutdown()
        self.thread.join()
        self.server.server_close()
        self.service.db.close()
        self.directory.cleanup()

    def api(self, method, path, token="", body=None):
        request = Request(self.base + path, method=method, data=None if body is None else json.dumps(body).encode(),
                          headers={"Authorization": "Bearer " + token, "Content-Type": "application/json"})
        try:
            response = urlopen(request, timeout=5)
        except HTTPError as error:
            response = error
        with response:
            return response.status, json.load(response)

    def publish(self):
        self.assertEqual(200, self.api("PUT", "/v1/diary/2026-10-29", "alice", self.body)[0])

    def test_authentication_and_anonymous_access(self):
        self.assertEqual(200, self.api("GET", "/health")[0])
        self.assertEqual(401, self.api("GET", "/v1/feed")[0])
        self.assertEqual(401, self.api("GET", "/v1/me", "forged")[0])

    def test_actual_friend_can_read_but_stranger_cannot(self):
        self.publish()
        code, feed = self.api("GET", "/v1/feed", "bob")
        self.assertEqual(200, code)
        self.assertEqual(["101"], [p["ownerId"] for p in feed["entries"]])
        self.assertNotIn("blocks", feed["entries"][0])
        code, post = self.api("GET", "/v1/posts/101/2026-10-29", "bob")
        self.assertEqual(200, code)
        self.assertEqual("친구에게 공개한 내용", post["blocks"][0]["text"])
        self.assertEqual(404, self.api("GET", "/v1/posts/101/2026-10-29", "eve")[0])
        self.assertEqual([], self.api("GET", "/v1/feed", "eve")[1]["entries"])
        self.assertEqual(404, self.api("GET", "/v1/friends/101/entries", "eve")[0])

    def test_friend_removal_takes_effect_on_next_read(self):
        self.publish()
        self.assertEqual(200, self.api("GET", "/v1/posts/101/2026-10-29", "bob")[0])
        self.kakao.links["bob"] = []
        self.assertEqual(404, self.api("GET", "/v1/posts/101/2026-10-29", "bob")[0])
        self.assertEqual([], self.api("GET", "/v1/feed", "bob")[1]["entries"])
        self.assertGreaterEqual(self.kakao.calls, 3)

    def test_unpublish_is_owner_scoped_and_immediate(self):
        self.publish()
        # Bob's DELETE only addresses Bob's diary, never Alice's.
        self.assertEqual(200, self.api("DELETE", "/v1/diary/2026-10-29", "bob")[0])
        self.assertEqual(200, self.api("GET", "/v1/posts/101/2026-10-29", "bob")[0])
        self.assertEqual(200, self.api("DELETE", "/v1/diary/2026-10-29", "alice")[0])
        self.assertEqual(404, self.api("GET", "/v1/posts/101/2026-10-29", "bob")[0])

    def test_account_deletion_removes_all_public_data(self):
        self.publish()
        self.assertEqual(200, self.api("DELETE", "/v1/account", "alice")[0])
        self.assertEqual([], self.api("GET", "/v1/feed", "bob")[1]["entries"])
        self.assertEqual(0, self.service.db.execute("SELECT COUNT(*) FROM profiles").fetchone()[0])

    def test_whole_private_book_is_rejected(self):
        body = copy.deepcopy(self.body)
        body["days"] = {"2026-10-28": "개인 일기"}
        self.assertEqual(400, self.api("PUT", "/v1/diary/2026-10-29", "alice", body)[0])
        self.assertEqual([], self.api("GET", "/v1/mine", "alice")[1]["entries"])

    def test_invalid_snapshot_never_overwrites_published_post(self):
        self.publish()
        for replacement in [{"title": "", "mood": "", "blocks": []}, {"title": "", "mood": "", "blocks": [{"id": "x", "type": "photo", "photo": "not-a-photo"}]}]:
            self.assertEqual(400, self.api("PUT", "/v1/diary/2026-10-29", "alice", replacement)[0])
        self.assertEqual("산책", self.api("GET", "/v1/posts/101/2026-10-29", "alice")[1]["title"])
        self.assertEqual(400, self.api("PUT", "/v1/diary/2026-02-30", "alice", self.body)[0])


class KakaoVerificationTest(unittest.TestCase):
    def test_tokens_from_another_kakao_app_are_rejected(self):
        gateway = KakaoGateway("1234")
        gateway.get = lambda *args, **kwargs: {"app_id": 9999, "expires_in": 3000, "id": 101}
        with self.assertRaises(APIError) as caught:
            gateway.authenticate("foreign-token")
        self.assertEqual(401, caught.exception.status)

    def test_identity_must_match_the_verified_token(self):
        gateway = KakaoGateway("1234")
        gateway.get = lambda path, *args: {"app_id": 1234, "expires_in": 3000, "id": 101} if "access_token_info" in path else {"id": 202}
        with self.assertRaises(APIError):
            gateway.authenticate("token")

    def test_incomplete_friend_pagination_fails_closed(self):
        gateway = KakaoGateway("1234")
        gateway.get = lambda *args, **kwargs: {"elements": [], "total_count": 20}
        with self.assertRaises(APIError):
            gateway.friends("token")


if __name__ == "__main__":
    unittest.main()
