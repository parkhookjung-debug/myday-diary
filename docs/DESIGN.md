# 팀 디자인 적용하기

디자인과 기능을 나눴습니다. 색상·글꼴·여백·카드·배치는 UI 파일에서 수정하고,
저장·날짜 이동·홈 화면 설정·캐릭터 이동은 기능 파일에서 관리합니다.
현재 화면의 기본 모양과 기록 저장 형식은 유지했습니다.

## 무엇을 어디서 수정하나요?

경로는 저장소 기준입니다.

| 변경할 내용 | 파일 |
| --- | --- |
| 앱·위젯·배경화면의 공통 색상 | `app/src/main/res/values/colors.xml` |
| 앱 여백·간격·모서리·입력창 높이 | `app/src/main/java/com/myday/diary/ui/design/DiaryDimensions.kt` |
| 앱 글꼴·글자 크기·줄 간격 | `app/src/main/java/com/myday/diary/ui/design/DiaryTypography.kt` |
| Material 기본 스타일 | `app/src/main/java/com/myday/diary/ui/design/MyDayTheme.kt` |
| 테마 이름·캐릭터 선택 목록 | `app/src/main/java/com/myday/diary/ui/design/DiaryOptions.kt` |
| 공통 카드·버튼 모양 | `app/src/main/java/com/myday/diary/ui/components/DiaryComponents.kt` |
| 전체 화면의 섹션 배치 | `app/src/main/java/com/myday/diary/ui/diary/DiaryScreen.kt` |
| 제목·날짜·테마 선택·홈 설정 영역 | `app/src/main/java/com/myday/diary/ui/diary/DiarySections.kt` |
| 일기·투두·습관·감정 카드의 내부 배치 | `app/src/main/java/com/myday/diary/ui/diary/DiaryBlockCard.kt` |
| 추가·삭제 확인 창 | `app/src/main/java/com/myday/diary/ui/diary/DiaryDialogs.kt` |
| 홈 위젯 배치 | `app/src/main/res/layout/diary_widget.xml` |
| 위젯과 배경화면의 크기·간격 | `app/src/main/res/values/dimens.xml` |
| 배경화면의 캐릭터·배경 그리기 | `app/src/main/java/com/myday/diary/ui/wallpaper/CharacterWallpaperRenderer.kt` |
| 캐릭터의 얼굴·몸·귀·팔다리 그림 | `app/src/main/java/com/myday/diary/ui/character/AnimatedCharacterPainter.kt` |
| 눈 깜박임·숨 쉬기·걷기·인사 동작 | `app/src/main/java/com/myday/diary/ui/character/CharacterAnimation.kt` |
| 앱 안 캐릭터 표시·탭 반응 | `app/src/main/java/com/myday/diary/ui/components/CharacterAvatar.kt` |

## 빠른 수정 예시

1. `colors.xml`의 `myday_primary`를 팀의 대표 색상으로 바꿉니다. 앱 주요 버튼과 위젯·배경화면의 강조 색에 반영됩니다.
2. `DiaryDimensions.kt`의 `cardCorner`로 카드 모서리를, `screenPadding`으로 화면 양쪽 여백을 조절합니다.
3. `DiaryTypography.kt`에서 `headlineSmall`의 `fontSize`와 `lineHeight`를 조절해 화면 제목 크기를 바꿉니다.
4. `DiaryScreen.kt`의 `item { ... }` 순서를 바꿔 화면 섹션을 옮깁니다.
5. `DiaryBlockCard.kt`에서 카드 내부의 제목·입력창·완료 체크·버튼 배치를 바꿉니다.

새 색상을 추가하는 경우 `MyDayTheme.kt`에 필요한 Material 색상 역할도 연결합니다.
기본 Material 카드와 필터 칩은 Material 색상표를 사용하고, 일기 블록 표면은 `myday_card`를 사용합니다.
레이아웃 변경은 Compose 코드 수정이며 Figma에서 자동 동기화하는 기능은 없습니다.

## 휴대폰 없이 미리 보기

Android Studio에서 **debug** 빌드 변형을 선택하고
`app/src/debug/java/com/myday/diary/ui/preview/DiaryPreviews.kt`를 엽니다.
**Split** 또는 **Design**에서 크림·숲·라벤더·빈 하루·개별 일기 카드를 봅니다.
필요하면 Preview를 다시 빌드합니다.

Interactive Preview에서는 블록 추가·수정·삭제·순서 변경과 날짜 이동을 확인할 수 있습니다.
예시 기록은 메모리에만 있으며 실제 기록 파일을 읽거나 쓰지 않습니다.
날짜 선택 대화상자·홈 위젯 추가·배경화면 설정 버튼은 미리보기에서 시스템 화면을 열지 않습니다.
실제 홈 위젯·배경화면 동작은 갤럭시에서 확인합니다.

캐릭터 미리보기 3종(토끼 쉬기·고양이 걷기·곰 인사)도 제공합니다.
정지 미리보기에서는 한 프레임만 보이므로 **Interactive Preview**에서 움직임을 확인합니다.
앱과 배경화면은 같은 `AnimatedCharacterPainter`의 동작을 사용합니다.
홈 위젯은 같은 그림에서 만든 정지 이미지를 표시하고, 탭할 때 포즈를 바꿉니다.
기존 기록의 이모지 값은 캐릭터 종류를 구분하는 저장 ID로만 유지하여 예전 기록을 계속 읽습니다.

## 기능 담당자가 관리하는 파일

- `data/DiaryModels.kt`: 기록 데이터와 저장 인터페이스
- `data/DiaryStore.kt`: 날짜별 JSON 저장, 저장 후 위젯 갱신
- `diary/DiaryController.kt`: 저장·날짜 이동·블록 추가/편집/이동/삭제
- `ui/diary/DiaryRoute.kt`: 화면 콜백과 기능 연결, 날짜 선택창
- `platform/HomeScreenActions.kt`: 위젯 추가와 배경화면 설정 요청
- `DiaryWidgetProvider.kt`: 위젯 갱신과 클릭 처리
- `CharacterWallpaperService.kt`: 캐릭터 이동·터치·애니메이션 수명주기

기존 기록에서 `theme` 값 0·1·2와 `type` 값 `text`·`todo`·`habit`·`emotion`을 사용합니다.
테마 이름과 색상은 바꿔도 되지만 ID를 바꾸거나 재배정할 때는 기록 호환성을 별도로 처리해야 합니다.
저장 파일 이름 `diary`, 날짜 키, JSON 필드, applicationId는 이 분리 작업에서 바꾸지 않았습니다.

## 팀 작업과 확인

디자인 담당자는 색상·크기 설정이나 UI 요소를 수정하고,
기능 담당자는 저장·시스템 동작을 수정합니다. 화면 콜백을 제거할 때는 서로 확인합니다.
PR에는 변경한 화면과 수정 파일을 적습니다.

```sh
./gradlew :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
```

Windows에서는 `./gradlew.bat`를 사용합니다.
Windows의 한글 프로젝트 경로에서는 Gradle 테스트 실행기가 클래스를 찾지 못할 수 있습니다.
이 작업에서는 같은 커밋을 영문 임시 경로로 복사해 테스트했습니다.
팀원은 처음 clone할 때 `C:\Projects\myday-diary`처럼 영문 경로를 사용하면 이 문제를 피할 수 있습니다.
단위 테스트는 저장 실패 시 날짜 이동 차단·재시도, 블록 재정렬 후 기록 보존,
날짜별 삭제 분리를 확인합니다. UI 픽셀과 실제 휴대폰의 시스템 동작을 검증하는 테스트는 아닙니다.

참고: [Compose 테마](https://developer.android.com/develop/ui/compose/designsystems/material3),
[Android Studio Preview](https://developer.android.com/develop/ui/compose/tooling/previews).
