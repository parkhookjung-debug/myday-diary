# Android 디자인 수정

현재 화면은 `PhoneNotebookRoute → PhoneNotebookScreen`입니다. Windows의 크림·숲·라벤더와 갈색·올리브 색을 휴대폰 네 탭으로 적용했습니다. 기능·저장 규칙은 `diary/Notebook*`, 시스템 연결은 `platform/`에 있습니다.

[클릭 가능한 전체 파일 표](../app/src/main/java/com/myday/diary/README.md) · [기능 통일·햅틱](ANDROID-PARITY.md)

| 바꾸려는 것 | 먼저 열 파일 |
| --- | --- |
| Material 색·글꼴 | `ui/design/MyDayTheme.kt`, `DiaryTypography.kt`, `res/values/colors.xml` |
| 탭·전체 여백·날짜·완료 현황·설정 카드 | `ui/diary/PhoneNotebookScreen.kt` |
| 블록 카드·입력창·모서리 | `ui/diary/PhoneBlockEditor.kt` |
| 자유 배치 화면·확대·손잡이 | `ui/diary/PhoneFreeBoard.kt` |
| 달력·검색 결과 | `ui/diary/PhoneCalendar.kt` |
| 상몬·성장·검/보주 카드 | `ui/diary/PhoneCompanion.kt` |
| 형식 목록·배치 미리보기·내 레이아웃 | `ui/diary/PhoneDialogs.kt` |
| 상몬 8가지 포즈·프레임 속도·장착 위치 | `ui/character/SangmonSprites.kt` |
| 앱 안 상몬 크기·터치 반응 | `ui/components/SangmonAvatar.kt` |
| 위젯 배치·배경색 | `res/layout/diary_widget.xml`, `res/drawable/widget_*.xml` |
| 배경화면 패턴·크기 | `ui/wallpaper/CharacterWallpaperRenderer.kt`, `res/values/dimens.xml` |

경로는 `app/src/main/java/com/myday/diary/` 기준이며 res 경로는 `app/src/main/` 기준입니다. 화면에 직접 지정한 카드 색·테마 배경·간격은 해당 Phone 파일에서 조절합니다. 모든 새 화면의 간격이 이전 `DiaryDimensions.kt` 한 곳에 연결된 것은 아닙니다.

## 수정 예

- 카드 모서리: `PhoneBlockEditor.kt`의 `RoundedCornerShape(22.dp)`와 입력창 14.dp를 고칩니다.
- 하단 메뉴: `PhoneNotebookScreen.kt`의 NavigationBar와 탭별 when 영역을 고칩니다.
- 전체 강조색: `colors.xml`의 `myday_primary`, `MyDayTheme.kt`의 Material 역할을 고칩니다.
- 상몬 그림·아이템·질문: Windows 원본을 수정하고 `./tools/export-android-assets.ps1`를 실행합니다. 생성된 PNG·catalog.json·SangmonIdentity.kt는 직접 편집하지 않습니다.
- 햅틱: `platform/DiaryHaptics.kt`에서 이벤트별 시스템 상수를 고칩니다. 시스템 설정을 우회하지 않습니다.

현재 debug `DiaryPreviews.kt`는 **초기 화면**을 위한 Preview입니다. 최신 Phone 화면은 Android Studio에서 에뮬레이터/갤럭시로 실행해 확인합니다. Figma 자동 동기화는 없습니다. 저장 규칙·ID를 바꾸지 않는 디자인 변경은 UI 파일에서 처리합니다.

## 확인

```powershell
./gradlew.bat :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
./gradlew.bat :app:connectedDebugAndroidTest
```

UI 검사는 격리된 예시 기록을 사용하고 스크린샷을 만듭니다. 실제 사용자 일기를 예시로 커밋하지 않습니다. Windows 한글 경로에서 테스트 실행기 오류가 나면 영문 경로에 clone합니다. 진동 느낌·런처 터치·위젯·절전 복귀는 갤럭시에서 확인합니다.
