# Windows 채팅 통신

[ChatClient.cs](ChatClient.cs)는 초대코드 입장·Bearer 토큰·메시지 조회/전송·취소·나가기를 담당합니다. 일기 저장 모델이나 사진 파일에 접근하지 않습니다.

화면은 [ChatWindow.cs](../UI/ChatWindow.cs), 말풍선은 [ChatTranscript.cs](../UI/ChatTranscript.cs), 서버는 [chat_server.py](../../server/chat_server.py)에서 수정합니다.

[서버 실행](../../server/README.md) · [프로토콜과 제한](../../docs/CHAT.md)

[무료 공통 서버·Mac과 연결](../../docs/FREE-CHAT.md): `pollAfterMs`가 있는 서버에는 지정 간격으로 재조회합니다. 기존 Python 서버의 긴 대기도 지원합니다. 무료 서버의 503 응답에서는 연결을 정리하고 안내를 표시합니다.
