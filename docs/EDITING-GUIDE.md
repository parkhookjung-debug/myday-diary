# 무엇을 바꾸려면 어느 파일을 열까요?

[프로젝트 첫 화면](../README.md) · [전체 폴더 구조](REPOSITORY-STRUCTURE.md) · [공동 작업 순서](../CONTRIBUTING.md)

아래 파일 이름을 클릭하면 GitHub에서 해당 코드를 바로 열 수 있습니다. Windows와 Android는 별도의 구현이므로 수정할 플랫폼을 먼저 고릅니다.

## GitHub에서 찾는 순서

현재 최신 Windows·Android 통합 코드는 [codex/windows-app 브랜치](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app)에 있습니다. [PR #7](https://github.com/parkhookjung-debug/myday-diary/pull/7)이 `main`에 반영되기 전에는 저장소 **Code → 브랜치 선택 → codex/windows-app**으로 이동합니다.

예를 들어 아이템 그림을 바꾸려면 **windows → Character → ItemPainter.cs**를 엽니다. 색·이름·장착 화면은 서로 다른 파일에 있으므로 아래 표에서 변경할 부분을 고릅니다. 다운로드한 EXE는 배포 파일이고 수정할 소스는 이 브랜치에 있습니다.

## Windows: 화면과 일기

| 바꾸고 싶은 것 | 먼저 열 파일 | 수정하는 부분 |
| --- | --- | --- |
| 전체 색·글꼴·버튼·카드 | [Design.cs](../windows/UI/Design.cs) | `Accent`, `Backgrounds`, `Font`, `RoundedButton`, `CardPanel` |
| 일기 창의 메뉴·사이드바·버튼 위치 | [DiaryWindow.cs](../windows/UI/DiaryWindow.cs) | 창 생성자와 컨트롤 배치, 화면 이벤트 연결 |
| 날짜 표지 장식 | [JournalCover.cs](../windows/UI/JournalCover.cs) | 표지 그림과 색 |
| 왼쪽 기록 목록 모양 | [HistoryRow.cs](../windows/UI/HistoryRow.cs) | 날짜·본문 미리보기·선택 상태 |
| 글·할 일·습관·감정·사진 블록 | [BlockCard.cs](../windows/UI/BlockCard.cs) | 제목·입력창·체크·사진 교체 버튼 |
| 블록 드래그·크기 조절 | [DiaryBoard.cs](../windows/UI/DiaryBoard.cs), [DiaryLayout.cs](../windows/Core/DiaryLayout.cs) | 마우스 입력과 좌표·크기 계산 |
| 일기 형식의 제목·질문 | [분야별 Templates 폴더](../windows/Core/Templates/README.md) | 분야에 맞는 `*Formats.cs`의 형식·질문 |
| 일기 형식 100종 선택 화면 | [TemplateGallery.cs](../windows/UI/TemplateGallery.cs), [TemplateOption.cs](../windows/UI/TemplateOption.cs) | 분류·검색·형식 카드·배치 그림 |
| 코넬·편지·타임라인 등 기본 배치 | [JournalLayouts.cs](../windows/Core/JournalLayouts.cs) | 형식별 블록 위치 계산 |
| 내 레이아웃 목록·이름·미리보기 | [MyLayoutsWindow.cs](../windows/UI/MyLayoutsWindow.cs), [LayoutNameDialog.cs](../windows/UI/LayoutNameDialog.cs), [SavedLayoutDiagram.cs](../windows/UI/SavedLayoutDiagram.cs) | 저장 목록·이름 입력·배치 그림 |
| 내 레이아웃 저장·재사용 규칙 | [SavedLayouts.cs](../windows/Core/SavedLayouts.cs) | 캡처·이름 검사·빈 칸으로 적용·병합 |
| 달력·검색 창 디자인 | [JournalBrowser.cs](../windows/UI/JournalBrowser.cs), [MonthCalendar.cs](../windows/UI/MonthCalendar.cs) | 검색 입력·결과·월간 달력 |
| 검색 대상과 날짜 계산 | [DiaryBrowse.cs](../windows/Core/DiaryBrowse.cs) | 본문 검색·월별 날짜·결과 미리보기 |
| 사진 표시 | [PhotoView.cs](../windows/UI/PhotoView.cs) | 사진 비율·썸네일·빈 사진 자리 |
| 사진 입력·회전·압축·용량 | [DiaryPhoto.cs](../windows/Core/DiaryPhoto.cs) | `FromFile`, `MaxEdge`, `MaxInputBytes`, 사진 검증 |
| 일기 데이터·자동 저장·백업 | [DiaryData.cs](../windows/Core/DiaryData.cs), [DiaryWindow.cs](../windows/UI/DiaryWindow.cs) | 모델·형식 버전·파일 저장, `QueueSave`·`FlushSave`·가져오기 연결 |

## Windows: 상몬·성장·아이템

| 바꾸고 싶은 것 | 먼저 열 파일 | 수정하는 부분 |
| --- | --- | --- |
| 상몬의 기본 몸·눈·입·포즈 | [MonsterPainter.cs](../windows/Character/MonsterPainter.cs) | 공통 몸 경로와 `MonsterPose` |
| 꼬리·귀·날개·목도리 등 추가 외형 | [MonsterAdditions.cs](../windows/Character/MonsterAdditions.cs) | 몸 뒤/앞에 덧붙이는 그림 |
| RPG 속성 외형의 색·명암·효과 | [GameSkins.cs](../windows/Character/GameSkins.cs) | 화염·빙결 등 몸 재질 |
| 캐릭터 이름·저장 ID·이동 속도 | [MonsterVariants.cs](../windows/Character/MonsterVariants.cs) | `Names`, `Ids`, `SpeedFactor` |
| 걷기·쉬기·하품·클릭 반응 | [PetBehavior.cs](../windows/Character/PetBehavior.cs) | 행동 전환과 `TouchFireSeconds`, `YawnCooldownSeconds` |
| 바탕화면 이동·드래그·우클릭 메뉴 | [DesktopPet.cs](../windows/Character/DesktopPet.cs) | 투명 창·화면 경계·메뉴·장비 표시 |
| 앱 안 상몬 미리보기 | [MonsterView.cs](../windows/UI/MonsterView.cs) | 앱 안 렌더링과 클릭 반응 |
| 하루 경험치·레벨·해금 조건 | [SangmonGrowth.cs](../windows/Core/SangmonGrowth.cs) | `DailyXP`, `LevelXP`, `Rewards`, `Levels` |
| 성장 화면 디자인 | [GrowthWindow.cs](../windows/UI/GrowthWindow.cs) | 경험치·다음 보상·외형 카드 |
| 성장 보상 의상 그림 | [RewardSkins.cs](../windows/Character/RewardSkins.cs) | 8종 보상 의상·색·소품 |
| 성장과 일기·바탕화면 연결 | [DiaryWindow.Growth.cs](../windows/UI/DiaryWindow.Growth.cs) | 성장 창 열기·보상 장착·상태 갱신 |
| 검·보주의 이름·속성·설명 | [SangmonItems.cs](../windows/Core/SangmonItems.cs) | `All` 목록과 슬롯·장착·백업 규칙 |
| 검·보주의 모양·광채·장착 위치 | [ItemPainter.cs](../windows/Character/ItemPainter.cs) | `Sword`, `Orb`, `Tone`, `DrawEquipped` |
| 장비함 디자인·분류·장착 버튼 | [EquipmentWindow.cs](../windows/UI/EquipmentWindow.cs) | 남색·금색 화면, 카드, 슬롯 상태 |
| 아이템의 움직이는 미리보기 | [ItemView.cs](../windows/UI/ItemView.cs) | 미리보기 타이머와 그리기 |
| 장비 저장·일기/바탕화면 연결 | [DiaryWindow.Items.cs](../windows/UI/DiaryWindow.Items.cs) | 장착 저장·실패 복구·장비 적용 |

## Android: 갤럭시 앱

Android 기능은 [패키지 안내](../app/src/main/java/com/myday/diary/README.md)에서 시작합니다. Windows의 사진·자유 배치·성장·장비 기능은 현재 Android에 구현하지 않았습니다.

| 바꾸고 싶은 것 | 먼저 열 파일 | 수정하는 부분 |
| --- | --- | --- |
| 화면 색 | [colors.xml](../app/src/main/res/values/colors.xml), [MyDayTheme.kt](../app/src/main/java/com/myday/diary/ui/design/MyDayTheme.kt) | 테마 색상과 Material 색 연결 |
| 글꼴·글자 크기 | [DiaryTypography.kt](../app/src/main/java/com/myday/diary/ui/design/DiaryTypography.kt) | `DiaryTypography` |
| 여백·카드 크기 | [DiaryDimensions.kt](../app/src/main/java/com/myday/diary/ui/design/DiaryDimensions.kt) | `screenPadding`, `cardCorner`, `sectionGap` |
| 일기 화면과 섹션 배치 | [DiaryScreen.kt](../app/src/main/java/com/myday/diary/ui/diary/DiaryScreen.kt), [DiarySections.kt](../app/src/main/java/com/myday/diary/ui/diary/DiarySections.kt) | Compose 화면·섹션 순서 |
| 일기 블록·공통 버튼 | [DiaryBlockCard.kt](../app/src/main/java/com/myday/diary/ui/diary/DiaryBlockCard.kt), [DiaryComponents.kt](../app/src/main/java/com/myday/diary/ui/components/DiaryComponents.kt) | 카드 내부와 재사용 UI |
| 날짜·입력·블록 동작 | [DiaryController.kt](../app/src/main/java/com/myday/diary/diary/DiaryController.kt) | 날짜 이동·추가·수정·순서·삭제 |
| 기록 모델·저장 | [DiaryModels.kt](../app/src/main/java/com/myday/diary/data/DiaryModels.kt), [DiaryStore.kt](../app/src/main/java/com/myday/diary/data/DiaryStore.kt) | 기록 데이터와 JSON 저장 |
| 화면 버튼과 기능 연결 | [DiaryRoute.kt](../app/src/main/java/com/myday/diary/ui/diary/DiaryRoute.kt) | 화면 콜백·날짜 선택 연결 |
| 홈 화면 위젯 | [DiaryWidgetProvider.kt](../app/src/main/java/com/myday/diary/DiaryWidgetProvider.kt), [diary_widget.xml](../app/src/main/res/layout/diary_widget.xml) | 위젯 갱신·클릭·위젯 배치 |
| 캐릭터 그림·동작 | [SketchMonsterPainter.kt](../app/src/main/java/com/myday/diary/ui/character/SketchMonsterPainter.kt), [CharacterAnimation.kt](../app/src/main/java/com/myday/diary/ui/character/CharacterAnimation.kt) | 상몬 도형과 애니메이션 |
| 라이브 배경화면 | [CharacterWallpaperService.kt](../app/src/main/java/com/myday/diary/CharacterWallpaperService.kt), [CharacterWallpaperRenderer.kt](../app/src/main/java/com/myday/diary/ui/wallpaper/CharacterWallpaperRenderer.kt) | 캐릭터 이동·터치·배경화면 렌더링 |
| 화면 Preview | [DiaryPreviews.kt](../app/src/debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt) | Android Studio debug Preview의 예시 |

## 바로 따라 할 수 있는 수정 예

**Windows 강조색 바꾸기:** [Design.cs](../windows/UI/Design.cs)의 `Accent`를 원하는 색으로 바꿉니다. 장비함의 남색·금색은 [EquipmentWindow.cs](../windows/UI/EquipmentWindow.cs)의 `Night`, `PanelColor`, `Gold`에서 따로 관리합니다.

**검 이름이나 설명 바꾸기:** [SangmonItems.cs](../windows/Core/SangmonItems.cs)의 `All`에서 `frost-sword` 항목의 이름·설명을 고칩니다. 그림은 [ItemPainter.cs](../windows/Character/ItemPainter.cs)의 `Sword`와 `Tone`에서 바꿉니다. 저장된 선택을 유지하려면 기존 ID는 유지합니다.

**공부 일기의 질문 바꾸기:** [LearningFormats.cs](../windows/Core/Templates/LearningFormats.cs)의 해당 형식에서 제목과 질문을 수정합니다. 형식 목록을 추가·삭제하면 [Tests.cs](../windows/Tests.cs)의 개수 검증과 [100종 목록](JOURNAL-CATALOG.md)도 함께 갱신합니다.

**경험치 규칙 바꾸기:** [SangmonGrowth.cs](../windows/Core/SangmonGrowth.cs)의 `DailyXP`·`LevelXP`와 보상 레벨 `Levels`를 수정하고 [GrowthTests.cs](../windows/GrowthTests.cs)를 확인합니다. 경험치는 지급 날짜 수에서 계산하므로 값 변경은 기존 사용자의 경험치·레벨에도 영향을 줍니다.

**Android 카드 모서리 바꾸기:** [DiaryDimensions.kt](../app/src/main/java/com/myday/diary/ui/design/DiaryDimensions.kt)의 `cardCorner`를 수정하고 Android Studio에서 [DiaryPreviews.kt](../app/src/debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt)를 엽니다.

## 수정한 뒤 확인하기

Windows는 [build.ps1](../windows/build.ps1)로 다시 빌드하고 새 `windows/bin/MyDay.exe`로 확인합니다. `실행.cmd`는 EXE가 이미 있으면 재사용하므로 코드 수정 뒤에는 먼저 빌드합니다. 실행 중인 앱은 알림 영역에서 **모두 종료**한 뒤 빌드합니다.

```powershell
./windows/build.ps1
$result = Start-Process windows/bin/MyDay.exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($result.ExitCode -ne 0) { throw 'Windows 검사 실패' }
```

화면 흐름을 확인하려면 `--smoke-test windows/test-output`을 사용합니다. 예시 기록은 격리된 임시 폴더를 쓰고 창 그림을 출력 폴더에 생성합니다. 자세한 실행·검증은 [Windows 안내](../windows/README.md)에 있습니다.

Android는 Windows PowerShell에서 `./gradlew.bat :app:assembleDebug :app:lintDebug :app:testDebugUnitTest`를 실행하고 갤럭시에서 위젯·배경화면을 확인합니다. [Android 안내](ANDROID.md)와 [디자인 Preview 안내](DESIGN.md)를 참고합니다.

문서만 수정한 경우 파일 링크와 설명을 확인합니다. 기능을 수정한 경우 관련 검증 파일도 함께 확인합니다. 사진은 [PhotoTests.cs](../windows/PhotoTests.cs), 검색은 [BrowseTests.cs](../windows/BrowseTests.cs), 내 레이아웃은 [SavedLayoutTests.cs](../windows/SavedLayoutTests.cs), 장비는 [ItemTests.cs](../windows/ItemTests.cs)가 담당합니다.
