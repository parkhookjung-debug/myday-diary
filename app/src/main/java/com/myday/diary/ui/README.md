# Android 디자인·그림을 바꾸려면

| 바꾸려는 것 | 파일/폴더 |
| --- | --- |
| 일기·달력·상몬·설정의 화면 구성 | [diary/README.md](diary/README.md) |
| Material 색·버튼 기본 색 | [design/MyDayTheme.kt](design/MyDayTheme.kt) |
| 글꼴·글자 크기·줄 간격 | [design/DiaryTypography.kt](design/DiaryTypography.kt) |
| 공통 색상 리소스 | [colors.xml](../../../../../res/values/colors.xml) |
| 앱 안 상몬 표시·탭 반응 | [components/SangmonAvatar.kt](components/SangmonAvatar.kt) |
| 상몬 애니메이션 프레임·장비 위치 | [character/SangmonSprites.kt](character/SangmonSprites.kt) |
| 라이브 배경화면 배경·상몬 그리기 | [wallpaper/CharacterWallpaperRenderer.kt](wallpaper/CharacterWallpaperRenderer.kt) |

상몬과 장비의 그림 자체는 Windows 원본에서 생성합니다. [공통 자산 수정 순서](../../../../../../../../tools/README.md)를 따릅니다. 캐릭터 ID와 저장 ID를 바꾸면 기존 기록 호환성도 확인합니다.

앱 화면은 **Phone UI·SangmonAvatar·SangmonSprites**를 사용합니다. `AnimatedCharacterPainter`·`SketchMonsterPainter`는 홈 위젯과 라이브 배경화면 렌더링에 쓰입니다.

[Android 전체 파일 안내](../README.md) · [현재 화면 안내](diary/README.md)
