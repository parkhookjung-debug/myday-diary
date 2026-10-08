# Windows 일기 디자인

일기를 나에게 보내는 메시지처럼 기록하는 DM 스타일입니다. 왼쪽 기록함은 날짜와 본문 미리보기를 표시하며 클릭하면 해당 날짜를 엽니다. 오른쪽은 둥근 일기 블록과 자유 배치 캔버스입니다. 아래쪽 버튼으로 글·할 일·습관·감정 블록을 추가합니다.

| 바꾸려는 내용 | 파일 |
| --- | --- |
| 색·글꼴·둥근 버튼·카드 | `windows/UI/Design.cs` |
| 기록함·날짜 헤더·도구·상몬 프로필 배치 | `windows/UI/DiaryWindow.cs` |
| 날짜 행·선택 강조·본문 미리보기 | `windows/UI/HistoryRow.cs` |
| 블록 제목·본문·체크·편집 도구 | `windows/UI/BlockCard.cs` |
| 자유 배치와 드래그 입력 | `windows/UI/DiaryBoard.cs` |

색상은 `Design.Ink`, `Muted`, `Accent`, `Soft`, `Tint`, `Backgrounds`에서 바꿉니다. 기본은 밝은 회색 배경과 보라색 포인트이며 블루·라일락 배경도 고를 수 있습니다. 저장된 테마 번호와 자유 배치 좌표는 기존 형식을 유지합니다. 간격·크기는 `Design.P/Point/Size/Pad`로 Windows 배율에 맞춥니다.

기록함에는 저장된 날짜와 오늘·현재 선택 날짜가 나타납니다. 글 일기 내용을 먼저 미리보고, 없으면 다른 블록의 내용을 표시합니다. 목록의 미리보기는 120자로 제한합니다. 입력은 기존 로컬 자동 저장과 JSON 백업을 사용합니다.

상몬의 게임 외형 8종·기존 48종, 클릭 반응과 바탕화면 이동은 [상몬 디자인](SANGMON-DESIGN.md)에서 관리합니다. 감정 블록은 개인 기록이며 메시지를 전송하거나 AI 답변을 생성하지 않습니다.

검증은 `windows/build.ps1`, `MyDay.exe --self-test`, `MyDay.exe --smoke-test windows/test-output`으로 실행합니다. 네이티브 화면 검증은 격리된 예시 데이터로 날짜 목록 클릭·날짜 이동·자동 저장·드래그·크기 변경·작은 창·캐릭터 메뉴를 확인하고 PNG를 생성합니다.

![DM 스타일 일기 예시](../previews/windows/windows-example.png)

![자유 배치 편집](../previews/windows/windows-free-layout.png)
