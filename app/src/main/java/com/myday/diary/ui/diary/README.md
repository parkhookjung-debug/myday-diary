# Android 화면을 바꾸려면

현재 앱은 `MainActivity → PhoneNotebookRoute → PhoneNotebookScreen` 순서로 열립니다. 색·간격·배치는 UI에서, 저장·성장 규칙은 [diary 폴더](../../diary/README.md)에서 수정합니다.

| 바꾸려는 것 | 먼저 열 파일 |
| --- | --- |
| 네 탭·날짜·메뉴·완료 현황·설정 | [PhoneNotebookScreen.kt](PhoneNotebookScreen.kt) |
| 카드·글 입력·체크·삭제·순서 이동 | [PhoneBlockEditor.kt](PhoneBlockEditor.kt) |
| 자유 배치·확대·화면 이동·크기 조절 | [PhoneFreeBoard.kt](PhoneFreeBoard.kt) |
| 달력·전체 본문/사진 설명 검색 | [PhoneCalendar.kt](PhoneCalendar.kt) |
| 상몬 64종·성장·장비 12종 화면 | [PhoneCompanion.kt](PhoneCompanion.kt) |
| 일기 100종·내 레이아웃·이름 입력 창 | [PhoneDialogs.kt](PhoneDialogs.kt) |
| 사진 선택·백업·화면과 기능 연결 | [PhoneNotebookRoute.kt](PhoneNotebookRoute.kt) |
| 공통 Material 색·글꼴 | [MyDayTheme.kt](../design/MyDayTheme.kt) · [DiaryTypography.kt](../design/DiaryTypography.kt) |

예: 카드 모서리는 `PhoneBlockEditor.kt`의 RoundedCornerShape, 메뉴 위치는 `PhoneNotebookScreen.kt`의 Scaffold/NavigationBar, 자유 배치 손잡이는 `PhoneFreeBoard.kt`에서 바꿉니다. 화면에 직접 지정한 색·간격도 해당 Phone 파일에서 확인합니다.

`DiaryScreen/DiaryBlockCard/DiaryDialogs/DiaryRoute/DiarySections`는 초기 Preview·호환 검사용 코드입니다. 현재 앱 화면을 고치려면 위의 **Phone 파일**부터 봅니다. 기존 debug Preview는 초기 화면이므로 최신 화면은 에뮬레이터/갤럭시에서 실행합니다.

[Android 전체 파일 안내](../../README.md) · [디자인 가이드](../../../../../../../../../docs/DESIGN.md)
