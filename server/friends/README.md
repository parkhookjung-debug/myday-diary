# 마이데이 친구 일기 서버

> 이 폴더는 이전 0.13 실험 구현입니다. 현재 0.14 앱은 Supabase 무료 요금제와 초대코드 방식을 사용합니다. [무료 연결 안내](../../docs/FREE_FRIENDS_SETUP.md)를 참고하세요. Render 유료 서버는 생성하지 않았습니다.

카카오 로그인을 확인한 후 사용자가 **직접 고른 기록의 사본만** 보관합니다. 휴대폰의 전체 일기나 백업 파일을 업로드하지 않습니다. 친구 관계는 읽기 요청마다 카카오 API로 확인합니다.

Python 3.11 이상과 기본 라이브러리만 사용합니다. 기존 채팅 라운지 서버와 별도로 실행하므로 기존 라운지 기능에 영향을 주지 않습니다.

## 실행

```powershell
$env:MYDAY_KAKAO_APP_ID="카카오디벨로퍼스_숫자_앱_ID"
python friends_server.py
```

기본 주소는 `http://127.0.0.1:8787`입니다. `/health`에서 준비 상태를 확인할 수 있습니다. 카카오 앱 ID를 설정하지 않으면 로그인 요청을 받지 않습니다. 앱 ID는 네이티브 앱 키나 어드민 키와 다른 값입니다.

같은 Wi-Fi의 휴대폰으로 개발 테스트할 때만 `HOST=0.0.0.0`으로 지정하고 개발 PC의 사설 IP를 앱 설정에 사용합니다. Windows 방화벽은 사용하는 사설 네트워크에서만 해당 포트를 허용하세요. 실제 사용은 HTTPS 도메인과 역방향 프록시 뒤에서 실행합니다. Android 릴리스 빌드는 친구 서버의 HTTP 주소를 거부합니다.

```powershell
$env:HOST="0.0.0.0"
$env:PORT="8787"
$env:MYDAY_FRIENDS_DB="D:/myday-private/friends.sqlite3"
python friends_server.py
```

DB 기본 위치는 `.local/friends.sqlite3`입니다. 이 폴더는 웹 정적 파일로 제공하지 않습니다. 서버를 옮길 때 DB를 함께 옮겨야 공개 기록이 유지됩니다. Docker를 쓰는 경우 `/data`에 영구 볼륨을 연결합니다.

```sh
docker build -t myday-friends .
docker run -d --name myday-friends -p 127.0.0.1:8787:8787 \
  -e MYDAY_KAKAO_APP_ID=123456 -v myday-friends:/data myday-friends
```

## API

`/health`를 제외한 요청은 `Authorization: Bearer {KAKAO_ACCESS_TOKEN}`이 필요합니다. 앱 ID와 토큰의 소유자를 서버에서 검증합니다. 카카오 토큰은 DB와 로그에 저장하지 않습니다.

| 요청 | 기능 |
|---|---|
| `GET /v1/me` | 로그인한 계정 확인 |
| `GET /v1/friends` | 동의 조건을 충족한 카카오 친구 목록 |
| `GET /v1/feed` | 친구들이 공개한 최근 100개 일기 요약 |
| `GET /v1/friends/{id}/entries` | 특정 친구가 공개한 날짜별 일기 요약 |
| `GET /v1/mine` | 내 공개 일기 목록 |
| `GET /v1/posts/{id}/{date}` | 본인 또는 확인된 친구의 공개 일기 본문·사진 |
| `PUT /v1/diary/{date}` | 내가 선택한 내용으로 공개 사본 생성·교체 |
| `DELETE /v1/diary/{date}` | 해당 날짜의 친구 공개 내리기 |
| `DELETE /v1/account` | 내 공개 기록과 서버 프로필 삭제 |

공개 한도는 날짜별 기록 30개, 사진 6장입니다. 사진은 앱이 정규화한 JPEG, 한 장에 2MB·최대 1600px입니다. 개인별 공개 보관 한도는 128MB·3,660일입니다. 본문 요청은 최대 20MB로 제한합니다. 카카오 친구 조회가 실패하면 타인의 기록을 반환하지 않습니다.

친구 공개는 게시 시점의 사본입니다. 공개 후 개인 일기를 수정해도 자동 반영되지 않습니다. 다시 공개하기를 선택하면 교체하고, 공개를 내리면 서버 사본을 삭제합니다. 휴대폰의 개인 일기는 서버 삭제의 영향을 받지 않습니다.

## 테스트

```sh
python -m unittest -v test_friends_server
```

테스트에서는 가짜 카카오 응답을 주입해 인증·친구 접근·공개 철회·다른 앱 토큰 차단을 검증합니다. 실제 서버 실행에는 테스트 계정 우회 경로가 없습니다. 실제 카카오 로그인과 친구 조회의 종단 테스트는 운영자가 발급한 앱 키·앱 ID·동의 설정을 연결한 뒤 수행합니다.
