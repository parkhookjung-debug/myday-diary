# 기념일·게임 메뉴·테트리스 최고기록 통합

2026-11-14. `codex/anniversary-calendar`의 `fe3ae6a`까지 검토하여 `codex/windows-app`에 필요한 기능을 반영했습니다. [반영 기준과 원본 커밋](../shared/branch-integrations.json). 원본 브랜치를 통째로 병합하지 않고 기존 일기·여섯 게임·공놀이와 맞춰 이식했습니다. 공개 0.15.0 Release와 별도인 작업 브랜치 개발 버전입니다.

## 상몬 테트리스

사용자가 고른 **상몬 실루엣 블록·파스텔색·기존 배치**를 유지합니다. 별도 사각 블록 테트리스 실행 파일/게임을 추가하지 않습니다. 기존 [상몬 블록 쌓기](SANGMON-BLOCKS.md)의 일곱 모양·양방향 회전·착지 표시·다음 세 블록·줄 삭제·점수·속도·결과 카드는 그대로 이용합니다.

오른쪽 아래에 **나의 최고 기록** 카드를 따로 표시하고 현재 점수는 위에 표시합니다. 완료한 판의 기록을 저장합니다. 예전 브랜치 실행 파일이 `%LOCALAPPDATA%/MyDay/Windows/tetris-record.json`에 남긴 `HighScore`가 있으면 기존 `Games/scores.json`의 `blocks` 최고점수와 비교해 높은 값을 가져옵니다. 원본 파일은 읽기만 하며 플레이 횟수나 최근 판 점수를 만들어내지 않습니다. 반복 실행 시 가져온 기록을 다시 쓰지 않고 현재의 더 높은 기록도 보존합니다.

이전 파일이 손상되었거나 1KB를 넘거나 점수가 0~1,000,000 범위를 벗어나면 안내하고 원본을 보존합니다. 기존 여섯 게임 기록은 계속 사용할 수 있습니다. 일기 경험치·출석과 게임 점수는 별도입니다. `--blocks-game`과 예전 브랜치의 `--tetris-game` 모두 상몬 디자인의 같은 게임으로 연결합니다.

## 상몬 게임 선택 메뉴

일기 화면의 **상몬 게임 ▾**를 누르면 여섯 게임을 바로 선택합니다. **오늘의 같은 배치 도전**, **플레이룸 전체 보기**, **바탕화면 공놀이**도 유지합니다. 바탕화면 상몬의 기존 플레이룸 진입은 그대로 이용합니다. 모든 게임은 같은 게임별 최고점수 파일을 사용하며, 별도 일반 테트리스 메뉴는 만들지 않습니다.

## 생일·기념일

**달력·일기 검색 → 생일·기념일 관리**에서 이름·날짜와 매년 반복 여부를 고르고 **추가**합니다. 달력 오른쪽 위의 ★와 마우스 도움말, 보조기술의 날짜 이름에 표시합니다. 기록·사진 표식과 겹치지 않습니다. 관리 창에서 항목을 선택하고 **선택 삭제**로 지웁니다. 목록 정렬 순서와 무관하게 해당 ID의 항목만 삭제합니다.

이름은 최대 40자, 목록은 최대 500개입니다. 매년 반복은 같은 월·일에 표시하고, 한 번만 표시는 지정한 해에만 나타납니다. 2월 29일은 윤년에만 표시합니다. 추가·삭제마다 저장하며 실패하면 메모리와 화면을 이전 상태로 돌립니다. 달력·검색 창을 닫은 뒤 일기 화면의 작은 달력도 갱신합니다.

기념일은 백업 버전 9의 `Studio.Anniversaries`에 담습니다. 새 공통 원본 제한을 생성하여 Windows·Android·Mac 모델에서 같은 항목과 제한으로 읽고 보존합니다. 기념일이 없는 예전 백업은 빈 목록으로 읽습니다. 가져올 때는 ID로 합치고 같은 ID는 들어오는 항목으로 갱신하며 다른 기존 기념일은 보존합니다. Android·Mac은 이번 변경에서 백업 데이터 보존만 맞췄고 기념일 편집 화면은 Windows에 먼저 제공합니다. 예전 앱으로 새 백업을 다시 저장하면 새 항목을 모를 수 있으므로 최신 개발 앱을 사용하세요.

## 수정 파일

| 바꿀 내용 | 파일 |
| --- | --- |
| 기념일 공통 제한 | `shared/studio.json` → `python tools/sync-studio.py` |
| 날짜 자료·검증·가져오기 | `windows/Core/StudioData.cs`, `Anniversaries.cs`, Android/Mac `StudioData` |
| 기념일 관리 화면·달력 표식 | `windows/UI/AnniversaryWindow.cs`, `MonthCalendar.cs`, `JournalBrowser.cs` |
| 간단한 게임 메뉴·직접 실행 | `windows/UI/DiaryWindow.Game.cs`, `DiaryWindow.Shell.cs`, `GameHubWindow.cs` |
| 상몬 블록 최고기록 카드 | `windows/UI/BlockGameWindow.cs` |
| 기존 테트리스 기록 가져오기 | `windows/Core/GameRecords.cs` |
| 동작·저장·실제 화면 검사 | `windows/Tests/BranchIntegrationTests.cs` |
| 실제 OS 간 데이터 왕복 검사 | Android `StudioTest.kt`, Mac `StudioVerification.swift`, `tools/WindowsStudioInterop.cs` |

## 검증

Windows는 기존 검사에 더해 기념일 반복·윤년·구형 자료·병합·저장 실패 복구·이전 테트리스 기록의 무손실 가져오기를 검사합니다. `--branch-integration-preview <폴더>`는 개인 자료 없이 실제 관리 창·달력·검색·게임 메뉴와 상몬 블록 연결을 확인합니다. 전체 `--smoke-test`에도 포함합니다. Android와 Mac은 실제 네이티브 모델의 백업 왕복으로 기념일 보존을 확인합니다.

[실제 화면](../previews/branch-integration-20261114/README.md). 현재 공개 Release 첨부 파일과 main 앱 소스는 교체하지 않습니다. 공개 서버나 유료 서비스를 추가하지 않습니다.

최종 검증: [Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43566437095)의 **491개 검사·실제 화면/조작·로컬 채팅**, [Android CI](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43566439349)의 **JVM 57·API35 기기 19·lint 오류 0·Windows 백업 왕복**, [Mac 공통 검증](https://github.com/parkhookjung-debug/myday-diary/actions/runs/43567082444)의 **Apple Silicon 빌드·기념일/가족 이모지 보존·Windows 백업 왕복·공통 계약·로컬 채팅**이 통과했습니다. Windows/Android 검증 후 변경은 Mac의 문자열 검증에만 한정하며 각 실행 파일의 실제 소스 커밋을 구분합니다. [파일별 소스·크기·SHA256](builds/integration-2026-11-14.json).

개발 실행 파일은 각 CI의 Artifacts에서 받습니다. Windows는 `myday-windows-79`의 `bin/MyDay.exe`, Android는 `myday-debug-77`의 `outputs/apk/debug/app-debug.apk`, Mac은 `myday-chat-macOS-ARM64`의 `macos/build/MyDay-macOS-arm64-v0.15.0-preview.zip`입니다. GitHub 로그인이 필요하며 개발 아티팩트는 보관 기간이 있으므로 만료되면 같은 소스를 다시 빌드합니다. 기존 공개 Release 파일은 교체하지 않았습니다.
