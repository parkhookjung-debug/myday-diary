# Android 시스템 연결·햅틱을 바꾸려면

| 바꾸려는 것 | 먼저 열 파일 |
| --- | --- |
| 체크·잡기·놓기·장착·거절 피드백 | [DiaryHaptics.kt](DiaryHaptics.kt) |
| 라이브 배경화면의 단발 진동 | [WallpaperHaptics.kt](WallpaperHaptics.kt) |
| 위젯 추가·배경화면 시스템 설정 열기 | [HomeScreenActions.kt](HomeScreenActions.kt) |
| 위젯 그림·완료 수·클릭 처리 | [DiaryWidgetProvider.kt](../DiaryWidgetProvider.kt) |
| 홈 캐릭터 이동·터치·화면 경계·절전 | [CharacterWallpaperService.kt](../CharacterWallpaperService.kt) |

햅틱은 앱의 끄기 설정과 휴대폰의 터치 피드백 설정을 따릅니다. 드래그의 모든 프레임이나 입력하는 모든 글자마다 진동하지 않습니다. 배경화면 진동은 View가 없으므로 별도 코드에서 지원 OS와 시스템 설정을 확인합니다.

[Android 전체 안내](../README.md) · [플랫폼 차이·햅틱 기준](../../../../../../../../docs/ANDROID-PARITY.md)
