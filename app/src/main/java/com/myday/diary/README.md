# Android 코드 찾기

갤럭시용 Kotlin / Compose 코드입니다. 앱 시작은 `MainActivity → PhoneNotebookRoute → PhoneNotebookScreen`입니다. 아래 파일 이름을 클릭해 수정 위치로 이동합니다.

[전체 수정 가이드](../../../../../../../docs/EDITING-GUIDE.md) · [Android 사용 안내](../../../../../../../docs/ANDROID.md) · [플랫폼 통일·햅틱](../../../../../../../docs/ANDROID-PARITY.md)

**폴더별로 보기:** [화면](ui/diary/README.md) · [디자인·그림](ui/README.md) · [데이터·사진](data/README.md) · [기능·저장 요청](diary/README.md) · [시스템·햅틱](platform/README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 앱 시작·권한 | [MainActivity.kt](MainActivity.kt), [AndroidManifest.xml](../../../../AndroidManifest.xml) | 진입점·서비스·진동 권한 |
| 네 탭·전체 메뉴·완료 현황 | [PhoneNotebookScreen.kt](ui/diary/PhoneNotebookScreen.kt) | 휴대폰 화면 구성 |
| 카드·입력·체크·순서 이동 | [PhoneBlockEditor.kt](ui/diary/PhoneBlockEditor.kt) | 블록 UI·길게 잡기 |
| 자유 배치·확대·크기 조절 | [PhoneFreeBoard.kt](ui/diary/PhoneFreeBoard.kt) | 유한 화면에서 논리 좌표 편집 |
| 달력·본문/사진 설명 검색 | [PhoneCalendar.kt](ui/diary/PhoneCalendar.kt) | 월간 화면·전체 검색 |
| 상몬·성장·장비함 UI | [PhoneCompanion.kt](ui/diary/PhoneCompanion.kt) | 64종·장비 300종·8슬롯·출석/미션 |
| 형식·내 레이아웃·이름 창 | [PhoneDialogs.kt](ui/diary/PhoneDialogs.kt) | 검색·배치 미리보기·확인 |
| 사진 선택·백업·화면 연결 | [PhoneNotebookRoute.kt](ui/diary/PhoneNotebookRoute.kt) | 시스템 선택기·IO·확인 |
| 저장·날짜·입력·장착 동작 | [NotebookController.kt](diary/NotebookController.kt) | UI 상태·실패 보존 |
| 회전 보존·백그라운드 저장 | [NotebookViewModel.kt](diary/NotebookViewModel.kt) | 순서대로 저장·마지막 편집 보존 |
| 좌표·성장·레이아웃·검증 규칙 | [NotebookRules.kt](diary/NotebookRules.kt) | Windows와 같은 규칙 |
| 데이터 모델·원자적 파일 저장 | [DiaryModels.kt](data/DiaryModels.kt), [DiaryStore.kt](data/DiaryStore.kt) | notebook.json·이전 기록 이전 |
| Windows JSON 호환 | [NotebookJson.kt](data/NotebookJson.kt) | 버전 1~4·사진·성장·장비 |
| 사진 회전·크기·JPEG 검증 | [NotebookPhoto.kt](data/NotebookPhoto.kt) | EXIF·1600px·메타데이터 제거 |
| 위젯·배경화면용 작은 캐시 | [DiaryHomeState.kt](data/DiaryHomeState.kt) | 본문/사진 없는 설정과 완료 수 |
| 화면·배경화면 햅틱 | [DiaryHaptics.kt](platform/DiaryHaptics.kt), [WallpaperHaptics.kt](platform/WallpaperHaptics.kt) | 시스템 설정을 따르는 짧은 피드백 |
| 공통 색·글꼴 | [MyDayTheme.kt](ui/design/MyDayTheme.kt), [DiaryTypography.kt](ui/design/DiaryTypography.kt), [colors.xml](../../../../res/values/colors.xml) | Material 테마·기본 팔레트 |
| 상몬 프레임·장비 렌더링 | [SangmonSprites.kt](ui/character/SangmonSprites.kt), [SangmonAvatar.kt](ui/components/SangmonAvatar.kt) | 8개 동작·탭 반응 |
| 홈 위젯 | [DiaryWidgetProvider.kt](DiaryWidgetProvider.kt), [diary_widget.xml](../../../../res/layout/diary_widget.xml) | 갱신·클릭·정지 포즈 |
| 라이브 배경화면 | [CharacterWallpaperService.kt](CharacterWallpaperService.kt), [CharacterWallpaperRenderer.kt](ui/wallpaper/CharacterWallpaperRenderer.kt) | 이동·터치·수명주기 |
| 시스템 설정 연결 | [HomeScreenActions.kt](platform/HomeScreenActions.kt) | 위젯·배경화면 설정 요청 |

## 공통 그림과 형식 수정

Windows의 [상몬 그림 코드](../../../../../../../windows/Character/README.md), [아이템 목록](../../../../../../../windows/Core/SangmonItems.cs), [20개 형식](../../../../../../../windows/Core/Templates/README.md)이 원본입니다. 수정 후 [export-android-assets.ps1](../../../../../../../tools/export-android-assets.ps1)를 실행하면 Android 자산과 해금 목록이 갱신됩니다. [NotebookCatalog.kt](data/NotebookCatalog.kt)는 생성된 자산 목록을 읽습니다. ID는 기존 기록을 유지하도록 바꾸지 않습니다.

## 검증·기존 파일

[NotebookTest.kt](../../../../../test/java/com/myday/diary/diary/NotebookTest.kt)는 성장·저장 실패·백업·메타데이터를 검사합니다. [PhoneNotebookDeviceTest.kt](../../../../../androidTest/java/com/myday/diary/PhoneNotebookDeviceTest.kt), [NotebookStorageDeviceTest.kt](../../../../../androidTest/java/com/myday/diary/NotebookStorageDeviceTest.kt)는 실제 Android UI·사진·저장을 격리된 예시 기록으로 검사합니다. [NotebookWriterDeviceTest.kt](../../../../../androidTest/java/com/myday/diary/NotebookWriterDeviceTest.kt)는 실제 ViewModel 저장기의 연속 입력·실패/재시도·앱 종료 직전 저장을 확인합니다.

휴대폰 화면 수정은 `PhoneNotebook*`에서 시작합니다. 최신 네 탭 화면은 에뮬레이터/기기로 실행해 확인합니다.
