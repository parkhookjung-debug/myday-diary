# 마이데이 (MyDay)

[![Android CI](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/android.yml/badge.svg)](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/android.yml)
[![Windows CI](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/windows.yml/badge.svg)](https://github.com/parkhookjung-debug/myday-diary/actions/workflows/windows.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

갤럭시·Windows용 커스터마이즈 일기 앱입니다. Windows에서는 일기 블록을 자유롭게 배치하고, 바탕화면을 돌아다니는 상몬을 키우며 검과 마법 보주를 장착합니다. MIT 라이선스로 공개하며 5명이 화면·저장·캐릭터·통합·검증을 나눠 개발합니다.

**코드를 수정하려면 [기능별 수정 가이드](docs/EDITING-GUIDE.md)를 먼저 열어주세요. 파일 이름을 클릭하면 해당 코드로 바로 이동합니다.**

현재 최신 통합 소스는 [codex/windows-app 브랜치](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app)에 있습니다. GitHub **Code → 브랜치 선택 → codex/windows-app**으로 이동하면 아래 파일이 보입니다. [통합 PR #7](https://github.com/parkhookjung-debug/myday-diary/pull/7)의 `main` 반영에는 팀원 리뷰 승인 1개가 필요합니다.

## 실행 파일 받기

| 사용할 환경 | 다운로드 | 실행·개발 안내 |
| --- | --- | --- |
| Windows 10/11 | [MyDay.exe 바로 받기](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.3.9-preview/MyDay.exe) · [전체 ZIP과 안내서](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.3.9-preview) | [Windows 안내](windows/README.md) |
| 갤럭시 / Android 8 이상 | [Android APK 받기](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.3.0-preview) (개발용 debug) | [Android 안내](docs/ANDROID.md) |
| macOS | 현재 macOS 실행 파일은 제공하지 않습니다. | Windows EXE는 Windows에서 실행합니다. |

GitHub 저장소 오른쪽 **Releases → 해당 버전 → Assets**에서도 받습니다. Windows는 .NET Framework 4.8 이상에서 사용하며, 이전 앱이 실행 중이면 알림 영역 MyDay → **모두 종료** 후 새 EXE를 실행합니다. 현재 Windows 배포는 v0.3.9-preview입니다.

## 이 기능을 바꾸려면 여기부터

| 하고 싶은 수정 | 먼저 열 파일 | 자세한 안내 |
| --- | --- | --- |
| 전체 색·글꼴·버튼 모양 | [windows/UI/Design.cs](windows/UI/Design.cs) | [Windows 디자인](docs/WINDOWS-DESIGN.md) |
| 일기 창·메뉴·버튼 위치 | [windows/UI/DiaryWindow.cs](windows/UI/DiaryWindow.cs) | [화면 폴더 안내](windows/UI/README.md) |
| 글·할 일·감정·사진 카드 모양 | [windows/UI/BlockCard.cs](windows/UI/BlockCard.cs) | [화면 폴더 안내](windows/UI/README.md) |
| 일기 형식의 이름·질문 | [windows/Core/Templates/README.md](windows/Core/Templates/README.md) | [100종 형식 목록](docs/JOURNAL-CATALOG.md) |
| 사진 입력·압축·회전 | [windows/Core/DiaryPhoto.cs](windows/Core/DiaryPhoto.cs) | [사진 안내](docs/PHOTOS.md) |
| 일기 데이터·저장·백업 | [windows/Core/DiaryData.cs](windows/Core/DiaryData.cs) | [기능·저장 폴더 안내](windows/Core/README.md) |
| 상몬 몸·눈·입 그림 | [windows/Character/MonsterPainter.cs](windows/Character/MonsterPainter.cs) | [상몬 그림 폴더 안내](windows/Character/README.md) |
| 상몬 걷기·쉬기·하품 | [windows/Character/PetBehavior.cs](windows/Character/PetBehavior.cs) | [상몬 디자인](docs/SANGMON-DESIGN.md) |
| 경험치·레벨·보상 조건 | [windows/Core/SangmonGrowth.cs](windows/Core/SangmonGrowth.cs) | [성장 안내](docs/SANGMON-GROWTH.md) |
| 검·보주 이름·속성·설명 | [windows/Core/SangmonItems.cs](windows/Core/SangmonItems.cs) | [아이템 안내](docs/SANGMON-ITEMS.md) |
| 검·보주 모양·빛나는 효과 | [windows/Character/ItemPainter.cs](windows/Character/ItemPainter.cs) | [아이템 안내](docs/SANGMON-ITEMS.md) |
| 장비함 디자인·장착 버튼 | [windows/UI/EquipmentWindow.cs](windows/UI/EquipmentWindow.cs) | [아이템 안내](docs/SANGMON-ITEMS.md) |
| 갤럭시 화면·위젯·배경화면 | [Android 코드 안내](app/src/main/java/com/myday/diary/README.md) | [Android 디자인](docs/DESIGN.md) |

블록 드래그·달력·검색·내 레이아웃·캐릭터 의상 등 나머지 기능도 [전체 수정 가이드](docs/EDITING-GUIDE.md)에서 찾습니다. **화면(UI) / 기능·저장(Core) / 그림·동작(Character)**으로 나눠 수정합니다. 각 폴더에 들어가면 README가 수정 위치를 설명합니다.

## 처음 개발에 참여한다면

```sh
git clone https://github.com/parkhookjung-debug/myday-diary.git
cd myday-diary
git switch codex/windows-app
```

1. [기능별 수정 가이드](docs/EDITING-GUIDE.md)에서 바꿀 파일을 고릅니다.
2. [공동 작업 안내](CONTRIBUTING.md)를 따라 별도 브랜치에서 수정합니다.
3. 해당 플랫폼을 다시 빌드하고 동작을 확인한 뒤 PR을 올립니다.

Windows는 C# / Windows Forms이며 [windows/build.ps1](windows/build.ps1)로 빌드합니다. 코드 수정 뒤에는 다시 빌드해야 변경이 EXE에 반영됩니다. Android는 Kotlin / Jetpack Compose이며 Android Studio, JDK 17 이상, Android SDK 35를 사용합니다.

[폴더 구조](docs/REPOSITORY-STRUCTURE.md) · [문서 목록](docs/README.md) · [5명 역할·공동 작업자](docs/TEAM.md) · [미리보기 목록](previews/README.md)

## 현재 기능

| 플랫폼 | 구현한 기능 |
| --- | --- |
| Windows | 날짜별 글·할 일·습관·감정·사진, 자유 배치, 내 레이아웃 저장·재사용, 일기 형식 100종, 월간 달력·본문/사진 설명 검색, 자동 저장·JSON 백업 |
| Windows 상몬 | 바탕화면 이동·클릭으로 일기 열기·숨기기/재표시, 외형 64종, 하루 경험치·성장 보상, 검·보주 12종의 두 슬롯 장착 |
| Android | 날짜별 블록 일기·자동 저장, 홈 위젯, 움직이는 캐릭터 라이브 배경화면 |

Windows의 사진·자유 배치·성장·장비 기능은 현재 Android에 구현하지 않았습니다. 각 기기에 기록을 저장하며 플랫폼 사이의 자동 동기화는 없습니다. 영상 첨부와 AI 감정 대화는 후속 기능입니다.

<details>
<summary>실제 화면과 아이템 예시 보기</summary>

예시는 격리된 기록으로 만든 Windows 창과 코드로 렌더링한 아이템입니다. [더 많은 화면·움직임 예시](previews/README.md)

![Windows 일기 예시](previews/windows/windows-example.png)

![상몬 아이템 12종](previews/windows/windows-item-catalog.png)

</details>

## 검증과 저장 호환성

Android CI는 빌드·Lint·단위 테스트를, Windows CI는 빌드·저장·레이아웃·사진·검색·성장·장비·캐릭터 렌더링 검사를 실행합니다. v0.3.9-preview의 Windows 자동 검사는 162개이며 배포 EXE의 네이티브 화면도 격리된 기록으로 확인했습니다. 갤럭시 실기기, 여러 모니터·DPI·절전 복귀는 추가 검증 대상입니다.

장비를 저장한 기록은 버전 4이며 v0.3.9-preview 이상으로 열어주세요. 이전 버전 1·2·3 기록과 백업은 읽고 보존합니다. 자세한 기준은 [Windows 기록·빌드 안내](windows/README.md)에 있습니다.
