# Android 코드 찾기

갤럭시 앱의 Kotlin/Compose 코드입니다. 아래 이름을 클릭해서 기능별 폴더와 파일로 이동합니다. Windows 전용 성장·사진·아이템 기능과는 구현이 다릅니다.

[전체 수정 가이드](../../../../../../../docs/EDITING-GUIDE.md) · [프로젝트 첫 화면](../../../../../../../README.md)

| 바꾸려는 것 | 파일 | 역할 |
| --- | --- | --- |
| 앱 시작 | [MainActivity.kt](MainActivity.kt) | 진입점과 화면 열기 |
| 전체 화면·블록·대화상자 | [DiaryScreen.kt](ui/diary/DiaryScreen.kt), [DiaryBlockCard.kt](ui/diary/DiaryBlockCard.kt), [DiaryDialogs.kt](ui/diary/DiaryDialogs.kt) | Compose UI |
| 색·간격·글꼴 | [MyDayTheme.kt](ui/design/MyDayTheme.kt), [DiaryDimensions.kt](ui/design/DiaryDimensions.kt), [DiaryTypography.kt](ui/design/DiaryTypography.kt), [colors.xml](../../../../res/values/colors.xml) | 디자인 값 |
| 공통 버튼·캐릭터 표시 | [DiaryComponents.kt](ui/components/DiaryComponents.kt), [CharacterAvatar.kt](ui/components/CharacterAvatar.kt) | 재사용 컴포넌트 |
| 날짜·블록 동작 | [DiaryController.kt](diary/DiaryController.kt) | 추가·수정·이동·삭제 |
| 기록 모델·파일 저장 | [DiaryModels.kt](data/DiaryModels.kt), [DiaryStore.kt](data/DiaryStore.kt) | JSON 데이터 |
| 화면 버튼과 기능 연결 | [DiaryRoute.kt](ui/diary/DiaryRoute.kt) | 콜백·날짜 선택 연결 |
| 홈 위젯 | [DiaryWidgetProvider.kt](DiaryWidgetProvider.kt), [diary_widget.xml](../../../../res/layout/diary_widget.xml) | 갱신·클릭과 위젯 배치 |
| 배경화면 | [CharacterWallpaperService.kt](CharacterWallpaperService.kt), [CharacterWallpaperRenderer.kt](ui/wallpaper/CharacterWallpaperRenderer.kt) | 이동·터치·렌더링 |
| 상몬 그림·움직임 | [SketchMonsterPainter.kt](ui/character/SketchMonsterPainter.kt), [CharacterAnimation.kt](ui/character/CharacterAnimation.kt), [AnimatedCharacterPainter.kt](ui/character/AnimatedCharacterPainter.kt) | 공통 캐릭터 애니메이션 |
| 시스템 설정 화면 연결 | [HomeScreenActions.kt](platform/HomeScreenActions.kt) | 위젯·배경화면 설정 요청 |
| 휴대폰 없이 Preview | [DiaryPreviews.kt](../../../../../debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt) | debug Design/Interactive Preview |
