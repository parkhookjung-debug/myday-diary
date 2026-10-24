# 상몬 라운지 서버

**무료 플랜용 공통 서버와 Mac 채팅 소스가 추가됐습니다.** [계정 없는 로컬 실행·같은 Wi-Fi·비용 차단 안내](../docs/FREE-CHAT.md)를 확인하세요. 아래 Python 서버도 계속 사용할 수 있습니다. 외부 공개 서버는 아직 배포하지 않았습니다.

Windows 앱의 **상몬 라운지 · 채팅**에 연결하는 초대형 공용방입니다. Python 3.11 이상에서 추가 패키지 없이 실행합니다. Android 채팅 화면은 다음 단계이며 같은 API를 사용할 수 있습니다.

## 내 컴퓨터에서 실행

저장소 루트에서:

```powershell
python server/chat_server.py
```

기본 주소는 `http://127.0.0.1:8765/`입니다. 이 주소는 같은 컴퓨터에서만 접속합니다. 첫 실행 때 `server/.local/join-code.txt`에 임의의 초대코드를 만듭니다. 해당 파일을 열거나 다음 명령으로 코드를 확인해 앱의 초대코드 칸에 넣으세요.

```powershell
Get-Content server/.local/join-code.txt
```

새 Windows 앱을 실행하고 왼쪽 **상몬 라운지 · 채팅**에서 서버 주소·닉네임·초대코드를 입력합니다. Enter로 전송하고 Shift+Enter로 줄을 바꿉니다. 서버는 실행한 터미널을 열어둔 동안 동작하며 Ctrl+C로 종료합니다.

## 같은 Wi-Fi의 여러 컴퓨터

서버를 맡은 PC에서:

```powershell
python server/chat_server.py --host 0.0.0.0 --port 8765
```

다른 PC는 `http://서버PC의사설IPv4:8765/`와 같은 주소를 입력하고 같은 초대코드를 사용합니다. 예를 들어 `http://192.168.0.10:8765/`입니다. 같은 기기라도 MyDay 앱은 Windows 사용자당 한 번만 실행하므로 여러 참가자 검사는 PC를 나눠 실행합니다. 방화벽이 차단하는 경우 서버 운영자가 해당 사설 네트워크의 접속 허용 범위를 정해야 합니다. 개발 도구는 방화벽이나 공유기 설정을 변경하지 않습니다.

## 인터넷에서 운영할 때

이 저장소에는 서버 프로그램과 Windows 클라이언트가 구현되어 있습니다. 상시 운영할 외부 서버·도메인·호스팅 계정은 아직 연결하지 않았습니다.

인터넷 접속에는 `https://chat.example.com/`처럼 HTTPS 주소를 사용합니다. HTTPS 역방향 프록시 뒤에서 Python 서버를 `127.0.0.1`에 바인딩하고, `/api/messages` 조회가 최대 20초 대기하므로 프록시 응답 제한은 30초 이상으로 설정합니다. Python 서비스는 별도 사용자와 프로세스 관리자로 실행하고 `.local` 데이터를 영구 디스크에 보관합니다. 초대코드를 바꾸면 서버를 다시 시작해 기존 세션을 종료합니다.

기본 초대코드 파일 대신 `MYDAY_CHAT_JOIN_CODE` 환경변수(12자 이상)를 사용할 수 있습니다. 환경변수 값·초대코드 파일·채팅 DB는 GitHub에 올리지 않습니다. 기본 `.local/` 폴더는 Git에서 제외합니다. 메시지는 서버의 SQLite에 저장되며 개인 일기 JSON에는 들어가지 않습니다. 백업할 때는 서버를 멈추고 데이터 폴더 전체를 보관하세요.

현재는 한 서버·공용방 하나·초대코드·임시 닉네임 방식입니다. 계정 인증, 친구 목록, 1:1 대화, 첨부 파일, 알림, 운영자 관리 화면은 후속 범위입니다. 닉네임은 실명이나 계정 소유를 보증하지 않습니다.

## 파일을 수정하려면

| 바꿀 내용 | 파일 |
| --- | --- |
| 입장·초대코드·메시지·접속자·보관 정책 | [chat_server.py](chat_server.py) |
| 서버 API 검사 | [test_chat_server.py](test_chat_server.py) |
| 실제 Windows EXE와 서버 연결 검사 | [test_windows_chat.py](test_windows_chat.py) |
| Windows 요청·취소·응답 해석 | [ChatClient.cs](../windows/Chat/ChatClient.cs) |
| Windows 입장·전송·나가기 화면 | [ChatWindow.cs](../windows/UI/ChatWindow.cs) |
| 말풍선·색·텍스트 배치 | [ChatTranscript.cs](../windows/UI/ChatTranscript.cs) |
| 일기 앱에서 채팅 창 열기 | [DiaryWindow.Chat.cs](../windows/UI/DiaryWindow.Chat.cs) |

## 검사

```powershell
python -m unittest discover -s server -p 'test_*.py' -v
./windows/build.ps1
python server/test_windows_chat.py --exe windows/bin/MyDay.exe --output .bootstrap/chat-preview
```

검사는 임시 폴더·임시 로컬 포트·테스트 초대코드를 사용하고 종료 시 정리합니다. 실제 사용자 채팅방과 개인 일기를 사용하지 않습니다. Windows CI에서도 같은 검사를 실행합니다.

[공통 API와 제한](../docs/CHAT.md) · [실제 화면](../previews/applied/chat-v1/README.md)
