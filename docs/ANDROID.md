# Android 개발·설치 안내

일반 설치는 [Android APK 다운로드와 Galaxy 설치 안내](DOWNLOADS.md#android)를 따르세요. 현재 공통 버전은 0.16.0이며 예전 0.4/0.8 APK 안내 대신 최신 Release를 사용합니다. iOS 앱은 이번 범위에 없습니다.

## Android Studio로 열기

1. 저장소를 clone하고 `codex/windows-app` 브랜치를 선택합니다.
2. Android Studio의 Open에서 app 폴더가 아닌 **저장소 루트**를 엽니다.
3. JDK 17, Android SDK 35, Build Tools 35를 준비합니다. 최소 실행 버전은 Android 8/API 26입니다.
4. 로컬 SDK 경로는 local.properties 또는 Android Studio SDK 설정에 넣습니다. 개인 설정·키·비밀번호는 Git에 올리지 않습니다.
5. Gradle Sync 후 app 모듈을 실행합니다. 에뮬레이터와 USB 디버깅을 켠 실제 Galaxy를 선택할 수 있습니다.

```sh
python tools/sync-desktop.py
python tools/sync-playroom.py
./gradlew :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
./gradlew :app:connectedDebugAndroidTest
```

Windows PowerShell에서는 `./gradlew.bat`를 사용합니다. APK 출력은 `app/build/outputs/apk/debug/app-debug.apk`이며 GitHub 배포 APK와 서명이 다를 수 있습니다. 테스트용 빌드를 기존 사용자 앱에 무작정 덮어 설치하거나 앱 데이터를 삭제하지 마세요.

## 기능과 파일

[세 플랫폼 기능표](THREE-PLATFORMS.md) · [Android 폴더별 수정 안내](../app/README.md) · [단위 검사](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/app/src/test/java/com/myday/diary) · [실제 화면·저장 검사](https://github.com/parkhookjung-debug/myday-diary/tree/codex/windows-app/app/src/androidTest/java/com/myday/diary)

Compose 화면은 Kotlin, 게임은 번들된 오프라인 공통 모듈을 WebView에서 실행합니다. 외부 URL 탐색·파일/콘텐츠 직접 접근은 차단하고 점수·PNG 저장만 제한된 브리지로 연결합니다. 게임과 로컬 영상은 새 서버 없이 실행합니다. 영상은 시스템 파일 선택기에서 사용자가 고른 참조만 저장하며 다른 기기로 옮긴 뒤 원본을 다시 선택합니다.

햅틱은 앱 설정과 시스템 설정을 따릅니다. 라이브 배경화면의 터치 지원은 제조사 런처별로 실제 기기 확인이 필요합니다. Android 에뮬레이터의 화면 검사와 Galaxy의 실제 진동 강도 검사는 구분합니다.
