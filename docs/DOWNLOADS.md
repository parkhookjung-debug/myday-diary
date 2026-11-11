# MyDay 0.15.0 다운로드와 설치

2026-11-11 · 세 플랫폼 시험판. [다운로드 페이지](https://github.com/parkhookjung-debug/myday-diary/releases/tag/v0.15.0-preview)에서 운영체제에 맞는 파일 **하나**를 받습니다. 공개 Release 첨부 파일이므로 GitHub Actions 로그인이나 개발 도구 없이 다운로드할 수 있습니다. `Source code (zip)`과 `Source code (tar.gz)`는 실행 앱이 아닌 개발 소스입니다.

| 운영체제 | 다운로드 | 실행 방법 |
| --- | --- | --- |
| Windows 10/11 · .NET Framework 4.8+ | [Windows 실행 파일 (.exe)](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.0-preview/MyDay-Windows.exe) | 기존 앱 종료 후 EXE 실행 |
| Mac · Apple Silicon(M1 이상) · macOS 13+ | [Mac 앱 (.zip)](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.0-preview/MyDay-macOS-arm64.zip) | 압축을 풀고 `MyDay.app` 실행 |
| Android 8 이상 · Galaxy 등 | [Android 설치 파일 (.apk)](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.0-preview/MyDay-Android.apk) | 휴대폰에서 APK 다운로드 후 설치 |

## 처음 실행하기

### Windows

1. `MyDay-Windows.exe`를 내려받습니다. 별도 설치 프로그램 없이 실행합니다. Windows 10/11과 .NET Framework 4.8 이상이 필요합니다.
2. 기존 MyDay가 실행 중이면 앱의 종료 기능으로 완전히 종료한 뒤 새 파일을 실행합니다. 일기 창만 닫는 동작은 앱 전체 종료와 다를 수 있습니다.
3. 바탕화면의 상몬을 클릭해 일기 창을 엽니다. 잡아끌면 몸이 늘어나고, 빠르게 놓으면 던진 속도에 비례해 날아가 화면 가장자리에서 튕깁니다.
4. 이 Windows 파일은 상몬 던지기까지 포함한 새 빌드입니다. Mac·Android에는 이번 Windows 탄성 물리가 적용되지 않았습니다.

### Mac

1. Apple Silicon(M1·M2·M3·M4 등) Mac과 macOS 13 이상에서 `MyDay-macOS-arm64.zip`을 받습니다. Intel Mac용 바이너리는 포함하지 않습니다.
2. 압축을 풀고 `MyDay.app`을 응용 프로그램 폴더로 옮겨 실행합니다.
3. 개발용 로컬 서명이 적용된 시험판이며 Apple 공증은 적용하지 않았습니다. 실제 Mac에서 운영체제의 앱 실행 확인이 필요할 수 있습니다. 실행 문제를 보고할 때 Mac 칩·macOS 버전·표시된 메시지를 함께 남겨 주세요.

### Android

1. Android 8 이상 휴대폰에서 `MyDay-Android.apk`를 내려받아 설치합니다. Google Play 배포본이 아닌 테스트용 APK입니다.
2. 기기의 APK 설치 안내에 따라 설치한 뒤 **마이데이**를 엽니다. 홈 위젯·라이브 배경화면·햅틱을 사용할 수 있습니다.
3. 이번 파일은 검증된 CI의 디버그 서명 시험판입니다. 이전 APK와 서명이 다르면 덮어 설치가 안 될 수 있습니다. **기록을 백업하고 복원 가능한지 확인하기 전에는 기존 앱을 삭제하지 마세요.** 앞으로 동일한 배포 서명을 사용하는 정식 업데이트 체계는 별도 작업입니다.

## 기존 기록을 유지하면서 업데이트

**공통 JSON 백업 저장 → 앱 완전 종료 → 새 앱으로 교체 → 기록·사진·경험치 확인** 순서로 진행합니다. 백업 버전 1~9를 지원합니다. 과거 외형·장비·수집 메타데이터는 호환용으로 보존하되 새 앱에서는 기본 상몬을 표시합니다. 기존 설치 파일과 백업은 새 앱에서 확인을 마칠 때까지 보관합니다.

공통 기능은 일기·사진·블록 자유 배치·일기 형식 100종·달력과 검색·기록 관리·기본 상몬·출석과 레벨업·백업/복구·기기 간 전송·로컬 라운지입니다. 변신·코디·옷장·뽑기·재화·합성 화면은 제거했습니다. Windows·Mac·Android의 입력 방식과 일부 시스템 기능은 서로 다릅니다.

기록은 기기 내부에 저장합니다. 세 기기의 자동 동기화는 없고 공통 백업이나 기기 연결로 옮깁니다. 채팅은 기존 로컬 서버를 실행했을 때 사용하며, 외부 인터넷 공개 채팅 서버는 제공하지 않습니다. 카카오 연동 SDK·인증·공유 원본은 보존하지만 공개 로그인·업로드 연결은 비활성입니다.

## 파일 확인과 소스

Release의 `SHA256SUMS.txt`로 세 실행 파일의 SHA256을 확인할 수 있습니다. `SOURCE.json`은 각 운영체제 파일의 실제 빌드 커밋·CI·크기·SHA256을 기록합니다. 검증된 파일을 그대로 게시했으며 Mac ZIP의 실행 권한과 서명을 유지하도록 재압축하지 않았습니다.

- [파일 출처·해시](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.0-preview/SOURCE.json) · [SHA256 목록](https://github.com/parkhookjung-debug/myday-diary/releases/download/v0.15.0-preview/SHA256SUMS.txt)
- [배포 소스](https://github.com/parkhookjung-debug/myday-diary/tree/v0.15.0-preview) · [간소화 기능·호환성](https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.0-preview/docs/FOCUSED-APP.md)
- [Windows 상몬 사용법·조절 파일](https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.0-preview/docs/DESKTOP-PET-MOTION.md)
- [전체 사용자 설명서](https://github.com/parkhookjung-debug/myday-diary/blob/v0.15.0-preview/docs/USER-MANUAL.md): 이전 기능 설명도 포함하므로 현재 제공 범위는 간소화 안내를 우선합니다.

검증 완료: Windows 346 assertions·네이티브 일기/포인터/렌더링·로컬 채팅, Android JVM 56개·API35 기기 19개·lint 오류 0개, Mac Apple Silicon 빌드·공통 계약·Windows 백업 교환·로컬 채팅. Intel Mac 실행과 실제 Galaxy의 햅틱 느낌은 별도 실기기 확인 범위입니다.
