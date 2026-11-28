# MyDay Windows · 0.16.1 시험판

**[최신 Windows 실행 파일 받기](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.16.1-preview/MyDay-Windows.exe)** · [세 플랫폼 다운로드/업데이트](../docs/DOWNLOADS.md) · [릴리스 소스](https://github.com/parkhookjung-debug/myday-diary/tree/v0.16.1-preview)

기존 앱을 알림 영역의 **모두 종료**로 완전히 종료한 뒤 새 EXE를 실행합니다. Windows 10/11과 .NET Framework 4.8 이상이 필요합니다. 소스 빌드는 `./windows/build.ps1`이고 테스트는 `windows/bin/MyDay.exe --self-test`입니다.

최신 변경: [Android·Mac·Windows 공통 기능과 수정 파일](../docs/THREE-PLATFORMS.md). 영상 첨부·재생을 추가하고 기존 일곱 게임·기념일·할 일 목록을 세 버전에 맞췄습니다. 영상은 원본 파일 참조이며 다른 기기로 옮기면 다시 선택합니다.

## 현재 기능

- 글·사진·영상·할 일·습관·감정 블록과 자유 배치, 100종 일기 형식과 내 레이아웃.
- 달력·본문/사진 설명 검색·태그·즐겨찾기·앨범·기록 관리·생일/기념일.
- 기본 상몬·출석·레벨업, 바탕화면에서 늘어나기·던지기·튕기기.
- 간식 산책·슬링샷·낚시·통통 점프·불꽃 요리·상몬 블록 쌓기·상몬 블록 퍼즐과 바탕화면 공놀이.
- 자동 저장·변경 이력·휴지통·백업/복구·기기 간 전송·로컬 채팅.

상몬 게임 ▾에서 바로 고릅니다. [게임만 실행](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.16.1-preview/SangmonGame.exe) · [공놀이 바로 시작](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.16.1-preview/SangmonFetch.exe). 게임 점수는 일기 경험치와 별도입니다. 변신·코디·뽑기·재화·합성 UI는 간소화 정책에 따라 제공하지 않습니다. 카카오 SDK 원본은 보존하고 공개 서버 연결은 비활성입니다.

## 수정할 파일

| 내용 | 파일/안내 |
| --- | --- |
| 일기 화면·색상·테마 | [DiaryWindow.Shell.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/UI/DiaryWindow.Shell.cs), [Design.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/UI/Design.cs) |
| 사진·자유 배치·기록 | [수정 가이드](../docs/EDITING-GUIDE.md) |
| 게임 메뉴 | [DiaryWindow.Game.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/UI/DiaryWindow.Game.cs), [GameHubWindow.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/UI/GameHubWindow.cs) |
| 상몬 블록 그림·배경·키 입력 | [BlockGameWindow.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/UI/BlockGameWindow.cs) |
| 블록 충돌·줄 삭제·속도 | [BlockGame.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/BlockGame.cs), [공통 규칙](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/mini-games.json) |
| 점수 저장·이전 기록 가져오기 | [GameRecords.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/GameRecords.cs) |
| 생일·기념일과 백업 | [통합 안내](../docs/BRANCH-INTEGRATION-2026-11-14.md) |
| 팀원 변경 출처 | [브랜치/푸시 확인](../docs/TEAM-UPDATES-2026-11-16.md) |

일기는 `%LOCALAPPDATA%/MyDay/Windows`에 저장합니다. X는 창만 숨기며 상몬이나 트레이에서 다시 엽니다. 앱 종료는 **모두 종료**입니다. 게임 기록은 별도 `Games/scores.json`이고 이전 `tetris-record.json`은 원본을 읽기만 하며 가져옵니다. 업데이트 전 공통 백업을 만들어 두세요.
