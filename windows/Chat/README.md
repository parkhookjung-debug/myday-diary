# Windows 채팅 통신

[ChatClient.cs](ChatClient.cs)는 초대코드 입장·Bearer 토큰·메시지 조회/전송·취소·나가기를 담당합니다. 일기 저장 모델이나 사진 파일에 접근하지 않습니다.

화면은 [ChatWindow.cs](../UI/ChatWindow.cs), 말풍선은 [ChatTranscript.cs](../UI/ChatTranscript.cs), 서버는 [chat_server.py](../../server/chat_server.py)에서 수정합니다.

[서버 실행](../../server/README.md) · [프로토콜과 제한](../../docs/CHAT.md)
