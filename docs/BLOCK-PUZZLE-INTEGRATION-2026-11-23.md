# 상몬 블록 퍼즐 Windows 통합

2026-11-23. cms6727의 [PR #11](https://github.com/parkhookjung-debug/myday-diary/pull/11), 원본 커밋 `06045194dae05bef139b45bc3606f61b9903f996`을 검토해 `codex/windows-app`에 통합했습니다. 0.15.2 시험판의 Windows 일곱 게임을 구성합니다. Mac·Android도 같은 버전과 다운로드 안내로 다시 빌드하며 게임 화면은 Windows에서 제공합니다.

## 무엇이 추가됐나요

- 일기 앱의 **상몬 게임 → 상몬 블록 퍼즐**, 또는 **플레이룸 전체 보기 → 일곱 번째 카드**에서 엽니다.
- 8×8 보드에 제시된 세 블록을 놓습니다. 가로·세로 줄을 채우면 동시에 지우고, 세 블록을 다 쓰면 새 블록을 받습니다. 회전과 시간 제한은 없고 남은 블록을 놓을 공간이 없으면 끝납니다.
- 마우스 드래그 또는 블록 선택 후 빈칸 클릭을 사용합니다. 키보드는 `1/2/3` 선택, 방향키 이동, `Enter/Space` 배치, `Esc` 취소입니다.
- 연속 줄 삭제 콤보와 전체 비우기 보너스, 완료한 판의 최고 기록과 PNG 결과 카드를 제공합니다.
- 기존 낙하형 상몬 블록 쌓기와 다른 여섯 게임, 공놀이는 그대로 사용합니다. 게임으로 일기 경험치나 출석 보상을 지급하지 않습니다.

## 기록 보존

일기·사진·자유 배치·상몬 성장·기념일·공통 백업 1~9는 변경하지 않습니다. 앱 안의 새 퍼즐은 `%LOCALAPPDATA%/MyDay/Windows/Games/block-puzzle.json`에 저장해 기존 `Games/scores.json`을 바꾸지 않습니다. 독립 실행 `SangmonBlockPuzzle.exe`는 기존 검토 도구와 같은 `%LOCALAPPDATA%/MyDay/BlockPuzzleReview/scores.json`을 사용하며 일기 저장소를 읽지 않습니다. 두 실행 방식의 점수 경로는 서로 다릅니다.

## 통합본 받기

[0.15.2 공개 다운로드](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.2-preview)에서 `MyDay-Windows.exe`를 받습니다. `SangmonGame.exe`는 일곱 게임 플레이룸을, `SangmonBlockPuzzle.exe`는 퍼즐 단독 모드를, `SangmonFetch.exe`는 공놀이를 바로 엽니다. GitHub 로그인과 개발 도구 없이 받을 수 있습니다.

기존 앱을 완전히 종료하고 현재 기록의 공통 JSON 백업을 별도로 보관한 뒤 새 파일을 실행하세요. Mac·Android 파일은 같은 Release에서 운영체제별로 선택합니다. 공개 채팅 서버나 유료 서비스를 추가하지 않습니다.

## 수정할 파일

| 수정 내용 | 파일 |
| --- | --- |
| 보드·블록·줄 삭제·콤보·종료 | `windows/Core/BlockPuzzle.cs` |
| 퍼즐 화면·마우스·키보드 | `windows/UI/BlockPuzzleWindow.cs` |
| 일곱 게임 선택 카드 | `windows/UI/GameHubWindow.cs` |
| 일기 안 게임 메뉴 | `windows/UI/DiaryWindow.Game.cs` |
| 별도 점수 파일과 호환성 | `windows/Core/GameRecords.cs` |
| 단독 실행과 빌드 산출물 | `windows/Program.cs`, `windows/build.ps1` |
| 규칙·화면·메뉴 검증 | `windows/Tests/BlockPuzzleTests.cs`, `BranchIntegrationTests.cs` |

## 로컬 검증

- 0.15.2 Windows 빌드와 512개 self-test 통과.
- 실제 WinForms 전체 smoke: 일기·자유 배치·성장·상몬 탄성·기존 여섯 게임·새 퍼즐·기념일·메뉴·작은 창 검증 통과.
- 퍼즐의 1,800회 배치와 가로·세로 동시 삭제, 기존 게임 점수 파일의 바이트 단위 보존 확인.
- 공통 생성 검사 14개, 간소화 제품 원본 계약, 로컬 채팅 서버 8개와 실제 Windows 클라이언트 검사 통과.
- Windows CI: 512개 검사와 실제 화면·로컬 채팅 통과.
- Android CI: JVM 57개·API35 기기 19개·Lint 오류 0·Windows 백업 왕복 통과. 외부 카카오 Maven 연결 시간 초과 후 같은 소스로 재시도해 성공했습니다.
- Mac CI: Apple Silicon 빌드·화면·로컬 채팅·공통 계약과 Windows 백업 왕복 통과.
- Android APK와 Mac ZIP에 기본 상몬 자산만 포함하는지 확인했습니다.

[파일 출처·해시](builds/release-0.15.2-2026-11-23.json) · [Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143148077) · [Android CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143153663) · [Mac CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/45143160441).

[실제 통합 화면](../previews/block-puzzle-integration-20261123/README.md).
