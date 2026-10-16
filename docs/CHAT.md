# Windows 서버 채팅 · Android 연결을 위한 공통 API

**Windows 먼저 구현한 기능**입니다. 일기 왼쪽의 **상몬 라운지 · 채팅**으로 엽니다. 채팅 창을 열거나 서버가 꺼져 있는 동안에도 일기는 별도로 작동합니다. 일기·사진·성장·장비 정보는 채팅 API로 전송하지 않습니다. 채팅 입력창에 직접 적고 보내기 버튼을 누른 내용만 보냅니다.

[서버 실행·다른 PC 접속](../server/README.md) · [실제 채팅 창](../previews/applied/chat-v1/README.md)

## 현재 기능

- 초대코드·닉네임으로 공용 **상몬 라운지** 입장, 접속 중인 닉네임 중복 방지.
- 텍스트·한글·이모지·줄바꿈, Enter 전송·Shift+Enter 줄바꿈, 전송 실패 시 입력 보존.
- 서버 알림을 기다리는 조회 방식으로 새 메시지 수신. 연결 장애 시 재시도 상태를 표시하며 만료된 연결은 재입장.
- 최근 메시지 50개 불러오기, Windows 화면에는 최근 200개 표시, 서버에는 최근 10,000개 보관.
- 접속자 목록, 명시적 나가기, 서버 재시작 후 대화 유지. 재시작 후에는 초대코드로 다시 입장.
- 같은 세션의 같은 전송 ID를 재사용하면 보관 중인 메시지를 다시 만들지 않습니다. 실패한 메시지를 그대로 재시도할 때 같은 ID를 사용합니다.

초대코드와 세션 토큰은 앱의 일기 백업이나 로그에 기록하지 않습니다. Windows 앱은 재실행 시 자동 입장하지 않습니다. HTTP는 로컬·사설 IPv4 개발용, 인터넷 주소는 HTTPS를 사용하며 인증서 검증을 끄지 않습니다. 외부 호스팅은 별도 운영 공간을 정해 배포하는 다음 단계입니다.

## 프로토콜 1

JSON UTF-8 요청/응답입니다. 로그인 이후에는 `Authorization: Bearer <token>` 헤더를 사용합니다. 토큰을 URL에 넣지 않습니다. 브라우저용 CORS는 열지 않았습니다.

| 메서드·경로 | 요청 | 응답 |
| --- | --- | --- |
| GET `/health` | 없음 | `ok`, `protocol: 1` |
| POST `/api/sessions` | `nickname`, `joinCode` | `token`, `nickname`, `room`, `maxText: 2000` |
| GET `/api/messages?after=0&wait=20` | Bearer 토큰 | `messages`, `cursor`, `reset`, `users` |
| POST `/api/messages` | Bearer 토큰, `text`, `clientId` | 저장한 메시지 |
| DELETE `/api/session` | Bearer 토큰 | `ok: true` |

메시지 필드는 `id`(정수), `nickname`, `text`, `sentAt`(UTC ISO 8601), `clientId`입니다. 클라이언트의 `clientId`는 소문자 16진수 32자로, 한 번의 전송과 그 재시도에 같은 값을 사용합니다.

처음 조회할 때 `after=0`으로 최근 50개를 받습니다. 이후에는 응답의 `cursor`를 다음 요청의 `after`로 사용합니다. 서버는 최대 100개씩 순서대로 보내며 `cursor`는 마지막으로 전달한 메시지를 가리킵니다. `wait`는 0~20초입니다. DB가 교체되었거나 오래된 기록이 정리되어 커서가 유효하지 않으면 `reset: true`와 최근 내역을 받습니다.

## 제한과 데이터

- 닉네임: 유니코드 정규화 후 2~20자, 글자·숫자·공백·밑줄·하이픈.
- 메시지: 최대 2,000 유니코드 문자. Windows 입력창은 UTF-16 기준 2,000자로 더 보수적으로 제한합니다. 빈 메시지·제어문자·방향 조작 문자는 거부합니다. 줄바꿈은 LF로 통일합니다.
- 동시 세션 최대 64, 동시에 대기하는 메시지 조회 최대 48, HTTP 처리 스레드 최대 80.
- 메시지 전송은 세션마다 분당 30회. 입장은 접속 IP마다 분당 30회 시도. 프록시 뒤에서는 프록시 IP가 기준이며 전달 헤더를 임의로 신뢰하지 않습니다.
- 세션은 90초간 요청이 없거나 발급 후 8시간이 지나면 만료합니다. 앱 강제 종료 시 접속자 목록에서 바로 사라지지 않을 수 있습니다.
- 서버 DB에는 메시지와 중복 전송 확인용 세션 식별자를 보관합니다. 접속용 Bearer 토큰·초대코드는 메시지 테이블에 저장하지 않습니다.
- 오류는 `{"error":"안내 문구"}`와 HTTP 상태 400/401/403/409/413/429/503 등으로 응답합니다.

## 팀원이 수정할 파일

| 변경 | 파일 |
| --- | --- |
| 서버와 공통 규칙 | [chat_server.py](../server/chat_server.py) |
| Windows HTTP·JSON·취소 | [ChatClient.cs](../windows/Chat/ChatClient.cs) |
| 채팅 창·접속·전송 | [ChatWindow.cs](../windows/UI/ChatWindow.cs) |
| 말풍선 그림·줄바꿈·스크롤 | [ChatTranscript.cs](../windows/UI/ChatTranscript.cs) |
| 진입 버튼·창 관리 | [DiaryWindow.Shell.cs](../windows/UI/DiaryWindow.Shell.cs), [DiaryWindow.Chat.cs](../windows/UI/DiaryWindow.Chat.cs) |
| 실제 Windows 통신·화면 검사 | [ChatTests.cs](../windows/ChatTests.cs), [test_windows_chat.py](../server/test_windows_chat.py) |

Android는 위 프로토콜을 그대로 사용해 전송 계층과 화면을 추가할 예정입니다. 현재 Android 앱에 채팅 탭이나 채팅 알림을 추가하지 않았습니다.
