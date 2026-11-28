# Android · Mac · Windows 공통 버전

0.16.0은 **세 앱의 사용자 기능과 저장 규칙을 맞추는 버전**입니다. iOS는 이번 개발 대상에 포함하지 않습니다. [설치 안내](DOWNLOADS.md) · [공통 개발 규칙](DESKTOP-SHARED.md).

| 공통 기능 | 세 앱에서 하는 일 |
| --- | --- |
| 글·사진·영상·감정 기록 | 블록 추가·편집·삭제, 사진과 설명, 기기 로컬 영상 연결·재생 |
| 할 일·습관 | 여러 할 일 추가·수정·개별 체크·삭제, 중요도 상/중/하, 전체 완료 |
| 100종 일기 형식·내 레이아웃 | 질문과 배치 선택, 기존 기록을 보존하며 추가, 빈 배치를 저장·재사용 |
| 자유 배치 | 블록 이동·크기 조절·자동 정리. 마우스 또는 터치로 편집 |
| 달력·검색·기념일 | 날짜별 기록, 사진 표시, 생일/일회성 기념일 추가·삭제와 ★ 표시, 본문·설명·감정·태그 검색 |
| 기본 상몬·성장 | 출석·기록 활동, 하루 최대 40 XP와 연속 7일 보너스, Lv.100 / 3,590 XP |
| 상몬과 상호작용 | 기본 상몬 반응, 드래그 늘어남·속도 비례 던지기·가장자리 튕기기 |
| 일곱 게임 | 간식 산책 / 슬링샷 / 느긋한 낚시 / 통통 점프 / 불꽃 요리 / 블록 쌓기 / 8×8 블록 퍼즐 |
| 간식 도전·공놀이 | 같은 날짜의 같은 간식 순서, 공을 던지고 상몬이 가져오는 휴식 놀이 |
| 게임 부가 기능 | 일시 정지·다시 시작·최고 기록, 끝난 게임의 결과 PNG 저장, 일기 XP와 분리 |
| 기록 관리·사진 앨범 | 태그·즐겨찾기·감정 기록·질문, 월별 사진, 기록 도구·변경 이력·휴지통 |
| 보관·기기 이동 | 자동 저장·실패 재시도·백업·복구·자동 백업·Wi-Fi/QR 전송 |
| 로컬 채팅 | 서버 주소·닉네임·초대코드로 같은 로컬 서버에 연결 |

기능을 삭제해 표를 맞추는 방식으로 처리하지 않았습니다. Mac에 있던 영상 연결도 Windows·Android에 추가했습니다. 제거했던 변신·코디·옷장·뽑기·재화·합성 화면과 자산은 복구하지 않았습니다. [간소화 정책](FOCUSED-APP.md).

## 운영체제에 맞는 조작

- **Windows:** WinForms, 마우스·키보드, 바탕화면 상몬과 알림 영역. 상몬 게임 메뉴에서 실행합니다. 공놀이는 바탕화면 위에서도 할 수 있습니다.
- **Mac:** SwiftUI, 마우스·트랙패드, 바탕화면 상몬과 메뉴 막대. 상몬 성장에서 놀이터를 엽니다.
- **Android:** Compose, 터치·햅틱, 홈 위젯과 라이브 배경화면. 상몬 성장에서 놀이터를 엽니다. 배경화면 상몬 터치는 런처의 지원을 따릅니다.

모바일 위젯과 PC 알림 영역처럼 운영체제 연결 방식은 서로 다릅니다. 공놀이는 Mac·Android에서는 앱 안 놀이터에서 제공하고, 상몬의 탄성 드래그와 던지기는 Mac 바탕화면·Android 라이브 배경화면에 연결합니다. Android 앱이 다른 앱 위에 임의로 창을 띄우는 오버레이 권한을 새로 요구하지 않습니다.

## 데이터 호환

이전 백업 1~9는 계속 읽습니다. 영상 블록을 추가하면 공통 버전 10 백업을 사용합니다. 영상은 원본을 연결하는 방식으로, JSON에 영상 전체를 넣지 않습니다. 다른 기기의 경로나 URI는 지우지 않고 보존하며 재생 전에 이 기기의 원본을 다시 고릅니다. 형식/레이아웃에는 개인 글·사진·영상 경로·체크 상태를 넣지 않습니다.

게임 최고 기록은 각 기기에 별도 저장하고 일반 일기 JSON에는 넣지 않습니다. 게임 결과가 출석·일기 경험치를 추가하거나 감소시키지 않습니다. 카카오 연동 원본은 유지하되 공개 서버 연결은 비활성입니다. 클라우드 자동 동기화는 제공하지 않습니다.

## 어느 파일을 수정하나요?

| 바꾸려는 내용 | 공통 원본 / 도구 | OS별 연결 |
| --- | --- | --- |
| 일기 형식·배치·팔레트·제한·상몬 물리 수치 | [shared/desktop.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/desktop.json), [templates.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/catalog/templates.json) → `python tools/sync-desktop.py` | Windows DiaryLayout, Mac DiaryLayout, Android NotebookRules·PetMotion |
| 게임 규칙 | [mini-games.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/mini-games.json), [snack-game.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/snack-game.json) | [Windows Core](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/windows/Core), 공통 [engine.js](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/playroom/engine.js) |
| Mac·Android 게임 화면 | [shared/playroom](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/shared/playroom) → `python tools/sync-playroom.py` | [Playroom.swift](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/macos/Sources/MyDay/Playroom.swift), [PhonePlayroom.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhonePlayroom.kt) |
| Windows 게임 화면 | 같은 공통 규칙 | [windows/UI](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/windows/UI), *GameWindow.cs·BlockPuzzleWindow.cs |
| 할 일 | 같은 항목/중요도/완료 저장 규칙 | [DiaryTodos.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/DiaryTodos.cs), [TodoRules.swift](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/macos/Sources/MyDay/TodoRules.swift), [TodoRules.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/diary/TodoRules.kt) |
| 영상 | Kind=video, VideoPath, 공통 백업 10 | [DiaryVideo.cs](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/windows/Core/DiaryVideo.cs), [DiaryBoard.swift](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/macos/Sources/MyDay/DiaryBoard.swift), [PhoneVideo.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhoneVideo.kt) |
| 기념일 | [shared/studio.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/studio.json)의 제한, 공통 Studio.Anniversaries | Windows AnniversaryWindow, Mac AnniversaryView, Android PhoneAnniversaries |
| 버전·안내 문구 | [journal-tools.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/journal-tools.json), [experience.json](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/shared/experience.json) → 해당 sync 도구 | 생성 코드 직접 수정 금지 |

Windows의 실제 C# 게임과 Mac·Android 엔진은 같은 입력을 넣는 [재생 비교 검사](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/tools/test-playroom.ps1)로 점수·충돌·블록 배치를 대조합니다. 버전별 실행 결과는 배포 파일의 SOURCE.json과 CI에서 확인하세요. 네이티브 화면 테스트와 기기별 하드웨어 느낌 확인은 구분합니다. Galaxy 진동 강도, 런처 배경 터치, Intel Mac 실행은 Apple Silicon/Android 에뮬레이터 검사로 대신 검증했다고 주장하지 않습니다.
