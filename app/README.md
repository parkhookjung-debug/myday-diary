# Android 앱 · 0.16.0 공통 시험판

[**Android APK 받기**](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.16.0-preview/MyDay-Android.apk) · [Galaxy 설치·권한·업데이트 안내](../docs/DOWNLOADS.md#android) · [세 버전 공통 기능](../docs/THREE-PLATFORMS.md)

일반 사용자는 Android 8 이상 휴대폰에서 APK를 열어 설치합니다. Android Studio는 소스를 수정할 때만 필요합니다. 기존 APK와 서명 충돌이 있으면 먼저 백업하고, 복원을 확인하기 전 앱을 삭제하지 마세요.

영상 첨부, 항목별 할 일과 중요도, 기념일 편집, 일곱 게임·간식 도전·공놀이를 Mac·Windows와 함께 제공합니다. **상몬 성장 → 상몬 놀이터**에서 게임을 엽니다. 햅틱과 위젯·라이브 배경화면은 Android 시스템 설정을 따릅니다. 변신·코디·뽑기 화면은 제공하지 않습니다.

## 수정할 파일

영상은 [PhoneVideo.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhoneVideo.kt), 게임은 [PhonePlayroom.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhonePlayroom.kt)와 [공통 원본](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/shared/playroom), 체크리스트는 [PhoneTodoList.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhoneTodoList.kt), 기념일은 [PhoneAnniversaries.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/PhoneAnniversaries.kt)부터 봅니다. 배경화면 움직임은 [PetMotion.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/diary/PetMotion.kt)와 [CharacterWallpaperService.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/CharacterWallpaperService.kt)를 고칩니다.

| 할 일 | 들어갈 곳 |
| --- | --- |
| 일기·달력·상몬·설정 화면 | [ui/diary](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/diary/README.md) |
| 색·글꼴·그림·공통 UI | [ui](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/ui/README.md) |
| 날짜·입력·성장·저장 요청 | [diary](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/diary/README.md) |
| 데이터 모델·JSON·파일·사진 | [data](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/data/README.md) |
| 햅틱·위젯/배경화면 시스템 연결 | [platform](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/platform/README.md) |
| 앱 시작 | [MainActivity.kt](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/MainActivity.kt) |
| 권한·서비스·진입점 | [AndroidManifest.xml](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/AndroidManifest.xml) |
| 앱 버전·SDK·라이브러리 | [build.gradle.kts](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/build.gradle.kts) |
| 단위 검사 | [src/test](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/app/src/test/java/com/myday/diary) |
| 에뮬레이터 UI·사진·저장 검사 | [src/androidTest](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/app/src/androidTest/java/com/myday/diary) |

Android Studio에서는 **저장소 루트**를 Open하고 app 모듈을 실행합니다. Windows 코드와 공통으로 사용하는 그림·질문 원본은 [생성 도구 안내](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/tools/README.md)를 따릅니다.

[실행·설치 안내](../docs/ANDROID.md) · [Android 전체 파일 표](https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/app/src/main/java/com/myday/diary/README.md) · [저장소 첫 화면](../README.md)
