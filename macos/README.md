# MyDay Mac · 0.16.0 공통 시험판

[**Mac 앱 다운로드**](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.16.0-preview/MyDay-macOS-arm64.zip) · [설치와 첫 실행 메시지 해결](../docs/DOWNLOADS.md#mac) · [세 버전 기능](../docs/THREE-PLATFORMS.md)

**Apple Silicon M1 이상·macOS 13 이상**용입니다. ZIP 압축을 풀고 MyDay.app을 응용 프로그램 폴더로 옮겨 엽니다. 기존 앱은 종료/⌘Q한 뒤 교체하세요. 로컬 개발 서명 시험판으로 Apple 공증은 적용하지 않았습니다. Intel용 실행 파일은 배포하지 않습니다.

## 사용할 수 있는 기능

글·사진·로컬 영상 연결·할 일 항목과 중요도·습관·감정·자유 배치·100종 일기 형식·기념일·달력·검색·앨범·기록 관리·출석·Lv.100 성장·백업/복구·Wi-Fi 전송·로컬 채팅을 제공합니다.

**상몬 성장 → 상몬 놀이터**에서 일곱 게임·간식 도전·공놀이를 엽니다. 바탕화면 상몬은 잡아 늘리고 던질 수 있으며, 메뉴 막대에서 숨기거나 다시 표시합니다. 공놀이는 앱 안에서 실행합니다. 영상은 원본을 연결하므로 기기 이동 때 다시 선택합니다.

변신·코디·뽑기·재화·합성 화면은 제공하지 않습니다. 과거 메타데이터는 백업 호환용으로 보존합니다. 게임 점수는 일기/경험치와 별도입니다. 공개 서버·카카오 공개 로그인·자동 클라우드 동기화는 제공하지 않습니다.

## 소스에서 빌드

Mac에 Xcode Command Line Tools와 Swift 5.9 이상을 준비한 뒤 저장소 루트에서 실행합니다.

```sh
git switch codex/windows-app
python3 tools/sync-desktop.py
python3 tools/sync-playroom.py
zsh macos/build.command
```

`macos/build/MyDay.app`과 버전이 붙은 ZIP이 생성됩니다. 앱 자체 사용에는 Xcode·Python이 필요하지 않습니다. 빌드 스크립트는 프로젝트의 생성 출력만 교체하며 사용자 일기 저장 폴더를 사용하지 않습니다.

```sh
macos/build/MyDay.app/Contents/MacOS/MyDay --self-test
macos/build/MyDay.app/Contents/MacOS/MyDay --playroom-test
macos/build/MyDay.app/Contents/MacOS/MyDay --render-preview
```

## 수정할 파일

| 변경 내용 | 파일 |
| --- | --- |
| 앱 화면·성장·설정 | [UnifiedViews.swift](Sources/MyDay/UnifiedViews.swift) |
| 일기 카드·영상 플레이어 | [DiaryBoard.swift](Sources/MyDay/DiaryBoard.swift) |
| 항목별 할 일·중요도 | [TodoListView.swift](Sources/MyDay/TodoListView.swift), [TodoRules.swift](Sources/MyDay/TodoRules.swift) |
| 달력·기념일 | [JournalSearch.swift](Sources/MyDay/JournalSearch.swift), [AnniversaryView.swift](Sources/MyDay/AnniversaryView.swift) |
| 게임 연결·점수·결과 저장 | [Playroom.swift](Sources/MyDay/Playroom.swift), 공통 [shared/playroom](../shared/playroom) |
| 바탕화면 상몬·늘어남·던지기 | [Companion.swift](Sources/MyDay/Companion.swift), [PetMotion.swift](Sources/MyDay/PetMotion.swift) |
| 저장·공통 백업·영상 참조 변환 | [MyDay.swift](Sources/MyDay/MyDay.swift), [WindowsCompatibility.swift](Sources/MyDay/WindowsCompatibility.swift) |
| 네이티브 검사 | [Verification](Sources/MyDay/Verification) |

개인 데이터는 `~/Library/Application Support/MyDay/`에 저장합니다. 업데이트 전 **설정·백업 → 백업 다운로드**로 공통 JSON을 앱 폴더 밖에 보관하세요. 설정의 Mac 전용 백업보다 공통 백업을 사용하면 Windows·Android로 옮기기 편합니다.
