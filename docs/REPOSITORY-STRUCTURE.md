# 파일·폴더 안내

Windows·Mac·Android 앱 소스, 세 앱 공통 원본, 서버, 팀 문서, 미리보기를 나눠 관리합니다. Android Studio 표준 구조를 유지하므로 Gradle은 루트에서 실행합니다.

```text
myday-diary/
├── app/                       Android 앱 (Kotlin / Compose)
│   └── src/
│       ├── main/
│       │   ├── java/com/myday/diary/
│       │   │   ├── chat/      라운지 채팅 연결
│       │   │   ├── data/      기록 모델·저장·사진·백업
│       │   │   ├── device/    기기 간 Wi-Fi 전송
│       │   │   ├── diary/     성장·뽑기·일기 동작 (Notebook*)
│       │   │   ├── platform/  햅틱·알림·홈 위젯·배경화면 연결
│       │   │   ├── social/    친구 공유 미리보기 (기본 꺼짐)
│       │   │   └── ui/        화면(Phone*)·디자인·캐릭터·배경화면
│       │   ├── assets/        공통 64종 애니메이션·300종 소품·100종 형식 (생성)
│       │   └── res/           색·크기·위젯 레이아웃·설정
│       ├── test/              단위 테스트
│       └── androidTest/       실제 UI·사진·저장 검사
├── windows/                   Windows 앱 (C# / WinForms)
│   ├── Chat/                  채팅 HTTP·JSON·취소
│   ├── Core/                  기록·저장·성장·뽑기 (Shared/ 는 생성 코드)
│   ├── UI/                    일기 창·블록·디자인
│   ├── Character/             상몬 그림·행동·외형·게임 재질
│   ├── Tests/                 --self-test 검사와 미리보기 생성기
│   ├── Program.cs             시작·세션·테스트 명령 연결
│   ├── build.ps1              .NET Framework 빌드
│   └── 실행.cmd               소스에서 빌드 후 실행
├── macos/                     Mac 앱 (SwiftUI)
│   ├── Sources/MyDay/         화면·저장·공통 생성 코드(*.g.swift)
│   │   └── Verification/      실행 파일 내장 자체 검사
│   ├── verification/          Windows 호환성 검사
│   └── build.command          SwiftPM 빌드·앱 번들 생성
├── shared/                    세 앱 공통 원본 (형식 100종·색상·규칙·전송 화면)
├── server/                    로컬 채팅 서버(Python)·Cloudflare 무료 서버 원본·통신 검사
├── supabase/                  친구 공유 미리보기용 DB·함수·검사
├── tools/                     공통 코드 생성·자산 내보내기·호환성·백업 도구
├── docs/                      기능 안내·수정 가이드·팀·발표 문서
│   ├── builds/                버전별 검증 빌드 기록
│   ├── manual/                사용자 설명서 Word·생성 원본
│   └── presentation/          발표 자료
├── previews/                  실제 앱 화면 캡처 (기능·버전별 폴더)
├── .github/                   CI·이슈·PR 양식
├── README.md                  소개·설치·빌드·수정 위치
├── CHANGELOG.md               버전별 변경 내역
├── CONTRIBUTING.md            협업 방법
└── LICENSE                    MIT 라이선스
```

## 기능별 수정 위치

[기능 → 실제 파일 → 수정할 부분을 찾는 가이드](EDITING-GUIDE.md)에서 모든 파일을 클릭해서 열 수 있습니다. 각 폴더의 첫 화면에도 안내를 붙였습니다.

- [Windows 화면](../windows/UI/README.md)
- [Windows 기능·저장](../windows/Core/README.md)
- [상몬·아이템 그림과 동작](../windows/Character/README.md)
- [분야별 일기 형식](../windows/Core/Templates/README.md)
- [Android 코드](../app/src/main/java/com/myday/diary/README.md)
- [문서 목록](README.md)

| 원하는 작업 | Android | Windows |
| --- | --- | --- |
| 색·글꼴·간격 | `ui/design/`, `res/values/` | `UI/Design.cs` |
| 일기 화면·블록 | `ui/diary/`, `ui/components/` | `UI/DiaryWindow.Shell.cs`, `UI/DiaryWindow.cs`, `UI/BlockCard.cs`, `UI/DiaryBoard.cs` |
| 자유 배치·크기 조절 | `ui/diary/PhoneFreeBoard.kt` | `Core/DiaryLayout.cs`, `UI/DiaryBoard.cs` |
| 기록·저장 | `data/`, `diary/` | `Core/DiaryData.cs` |
| 사진 첨부 | `data/NotebookPhoto.kt`, `ui/diary/PhoneBlockEditor.kt` | `Core/DiaryPhoto.cs`, `UI/PhotoView.cs`, `UI/BlockCard.cs` |
| 달력·일기 검색 | `ui/diary/PhoneCalendar.kt` | `Core/DiaryBrowse.cs`, `UI/JournalBrowser.cs`, `UI/MonthCalendar.cs` |
| 내 레이아웃 저장·재사용 | `diary/NotebookRules.kt`, `ui/diary/PhoneDialogs.kt` | `Core/SavedLayouts.cs`, `UI/MyLayoutsWindow.cs`, `UI/SavedLayoutDiagram.cs` |
| 캐릭터 그림 | `ui/character/` | `Character/MonsterPainter.cs`, `MonsterAdditions.cs` |
| 아이템 장비함 | `ui/diary/PhoneCompanion.kt`, `diary/NotebookController.kt` | `Core/SangmonItems.cs`, `UI/EquipmentWindow.cs`, `UI/DiaryWindow.Items.cs` |
| 아이템 그림·광채 | `ui/character/SangmonSprites.kt`, 생성 assets | `Character/ItemPainter.cs`, `UI/ItemView.cs` |
| 상몬 성장·해금 | `diary/NotebookRules.kt`, 생성 `data/SangmonIdentity.kt` | `Core/SangmonGrowth.cs`, `UI/GrowthWindow.cs`, `UI/DiaryWindow.Growth.cs` |
| 성장 보상 의상 | 공통 Windows 원본 → 생성 assets | `Character/RewardSkins.cs` |
| 게임 속성 재질 | 공통 Windows 원본 → 생성 assets | `Character/GameSkins.cs` |
| 움직임 | `ui/character/CharacterAnimation.kt` | `Character/PetBehavior.cs` |
| 홈/바탕화면 | 위젯 Provider, Wallpaper Service, `ui/wallpaper/` | `Character/DesktopPet.cs` |
| 검증 | `app/src/test/`, `app/src/androidTest/` | `Tests/` |

Android 표의 Java 경로는 `app/src/main/java/com/myday/diary/` 기준입니다. Windows 경로는 `windows/` 기준입니다. 문서와 미리보기는 각 플랫폼 코드의 실행 파일이 아닙니다.

## 실행 파일 배포

GitHub Releases에 플랫폼별로 배포합니다.

- Windows: `MyDay-Windows-v0.3.10-preview.zip`에 `MyDay.exe`, 실행 안내, 아이템·상몬 성장·내 레이아웃·달력/검색·사진 안내, 100종 목록, 라이선스
- Android: `MyDay-Android-v0.4.0-preview.apk` 개발용 debug 빌드
- GitHub에서 자동 제공하는 Source code ZIP/TAR: 같은 태그의 전체 소스

빌드 출력은 소스 폴더에 커밋하지 않습니다. `windows/bin/`, `app/build/`는 로컬 빌드가 생성하고, CI 결과와 Releases에서 실행 파일을 받습니다. 캐시·기기별 SDK 경로·개인 기록·참고 사진·인증 파일도 저장소에 포함하지 않습니다.

## 팀 문서

- [Android 실행과 기능](ANDROID.md)
- [Windows 실행과 개발](../windows/README.md)
- [Windows 디자인 수정](WINDOWS-DESIGN.md)
- [일기장 형식 100종 사용](JOURNAL-FORMATS.md)
- [100종 전체 목록·참고 자료](JOURNAL-CATALOG.md)
- [Android 디자인 수정](DESIGN.md)
- [상몬 외형·게임 스타일](SANGMON-DESIGN.md)
- [일기 자유 배치](FREE-LAYOUT.md)
- [사진 첨부·저장·백업](PHOTOS.md)
- [월간 달력·일기 검색](CALENDAR-SEARCH.md)
- [내 레이아웃 저장·재사용](MY-LAYOUTS.md)
- [상몬 성장·보상 외형](SANGMON-GROWTH.md)
- [상몬 아이템·장착](SANGMON-ITEMS.md)
- [5명 팀 역할](TEAM.md)
- [미리보기 설명](../previews/README.md)
