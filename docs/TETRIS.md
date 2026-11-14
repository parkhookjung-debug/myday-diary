# 상몬 테트리스 (Windows)

일기 앱 왼쪽 **상몬 게임** 버튼을 누르고, 선택 메뉴에서 **테트리스 · 점수 도전**을 고르면 열려요. 개발 중 단독으로 실행하려면 `MyDay.exe --tetris-game`을 사용하거나 파일 이름을 `TetrisGame.exe`로 바꿔 실행합니다. 게임은 현재 판의 점수만 보관하며 일기·경험치·재화에는 영향을 주지 않습니다.

Windows 실행 파일: [MyDay.exe](../builds/windows-tetris-0.15.0/MyDay.exe) · [파일 출처와 SHA-256](builds/windows-tetris-0.15.0.json)

- **← / →** 이동, **↑** 회전, **↓** 한 칸 내리기
- **Space** 즉시 떨어뜨리기, **P** 또는 **Esc** 일시 정지, **R** 새 판
- 화면 오른쪽에 현재 점수·최고 점수·레벨·지운 줄 수와 다음 블록이 표시됩니다. 최고 점수는 게임이 끝날 때 갱신되며 다음 실행에도 유지됩니다.
- 줄을 지우면 점수를 얻고, 블록 5개를 놓을 때마다 낙하 속도가 빨라집니다. 줄 10개마다 점수 레벨이 올라가며 한 번에 4줄을 지우면 높은 점수를 받아요.
- 상몬 그림이 보드 배경에 깔리고, 블록은 배경이 은은하게 비치는 색으로 표시됩니다.
- 창을 벗어나면 자동으로 일시 정지합니다.

최고 점수는 Windows 사용자 프로필의 `%LOCALAPPDATA%\MyDay\Windows\tetris-record.json`에 따로 저장합니다. 일기 본문이나 공통 백업에는 포함하지 않습니다.

| 역할 | 파일 |
| --- | --- |
| 회전·충돌·줄 삭제·점수 | [TetrisGame.cs](../windows/Core/TetrisGame.cs) |
| 창·키 입력·게임 보드·점수 표시 | [TetrisGameWindow.cs](../windows/UI/TetrisGameWindow.cs) |
| 앱 메뉴 연결 | [DiaryWindow.Shell.cs](../windows/UI/DiaryWindow.Shell.cs), [DiaryWindow.Game.cs](../windows/UI/DiaryWindow.Game.cs) |
