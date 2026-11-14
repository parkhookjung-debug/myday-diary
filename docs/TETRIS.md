# 상몬 테트리스 (Windows)

일기 앱 왼쪽 메뉴의 **상몬 게임 → 테트리스 · 점수 도전**에서 열어요. 개발 중 단독으로 실행하려면 `MyDay.exe --tetris-game`을 사용하거나 파일 이름을 `TetrisGame.exe`로 바꿔 실행합니다. 게임은 현재 판의 점수만 보관하며 일기·경험치·재화에는 영향을 주지 않습니다.

- **← / →** 이동, **↑** 회전, **↓** 한 칸 내리기
- **Space** 즉시 떨어뜨리기, **P** 또는 **Esc** 일시 정지, **R** 새 판
- 화면 오른쪽에 현재 점수·레벨·지운 줄 수와 다음 블록이 표시됩니다.
- 줄을 지우면 점수를 얻고, 10줄마다 레벨이 올라 블록이 더 빨리 내려옵니다. 한 번에 4줄을 지우면 높은 점수를 받아요.
- 창을 벗어나면 자동으로 일시 정지합니다.

| 역할 | 파일 |
| --- | --- |
| 회전·충돌·줄 삭제·점수 | [TetrisGame.cs](../windows/Core/TetrisGame.cs) |
| 창·키 입력·게임 보드·점수 표시 | [TetrisGameWindow.cs](../windows/UI/TetrisGameWindow.cs) |
| 앱 메뉴 연결 | [DiaryWindow.Shell.cs](../windows/UI/DiaryWindow.Shell.cs), [DiaryWindow.Game.cs](../windows/UI/DiaryWindow.Game.cs) |
