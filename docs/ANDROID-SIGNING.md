# Android APK 서명과 기존 앱 이전

현재 v0.4.0 공개 APK는 개발용 debug 서명입니다. 이전 v0.3.0의 인증서 SHA-256은 `529cd6d1b1e92d9f7ec0eef70ab80ba56766b2e83010679cb2fdff5705797b14`입니다. CI 개발 인증서는 실행마다 달라질 수 있어 업데이트 설치가 거절될 수 있습니다. 기존 앱 삭제는 기록도 삭제하므로 [백업 이전 안내](ANDROID.md#apk-설치와-기존-기록)를 먼저 따릅니다.

## 준비한 전용 배포 방식

[android-release.yml](../.github/workflows/android-release.yml)은 수동 실행만 가능합니다. PR #7을 리뷰하여 main에 반영한 뒤 Actions 목록에서 사용할 수 있습니다. 소유자 승인 후 전용 키를 만들고 아래 GitHub Actions 비밀 저장소 항목을 설정하면 같은 키로 release APK를 빌드합니다. 현재 키를 생성·등록하지 않았으며 이 워크플로는 비밀 값이 없으면 실패합니다. PR 검사에 배포 키를 전달하지 않습니다.

- `MYDAY_ANDROID_KEYSTORE_BASE64`: 전용 keystore의 Base64
- `MYDAY_ANDROID_KEYSTORE_PASSWORD`: 전용 비밀번호
- alias: `myday-preview`

로컬 빌드는 `MYDAY_ANDROID_KEYSTORE`에 절대 파일 경로, `MYDAY_ANDROID_KEYSTORE_PASSWORD`에 비밀번호를 환경 변수로 주고 `:app:assembleRelease`를 실행합니다. 키 없이 빌드한 release APK는 설치용 서명이 없으므로 배포하지 않습니다. 일반 팀 개발은 기존 `assembleDebug`를 사용합니다.

키·비밀번호는 로그·문서·소스·미리보기·공개 Release에 넣지 않습니다. `.gitignore`에 keystore·환경 파일을 제외했습니다. 키는 소유자만 접근할 개인 저장소에 보관하고 암호화된 별도 백업을 남깁니다. GitHub 비밀 등록은 소유자의 명시적 승인 후 진행합니다. 서명 키가 달라지면 이전 앱을 직접 업데이트할 수 없으므로 키를 매번 다시 만들지 않습니다.

전용 서명을 처음 적용하더라도 과거 임시 debug APK와는 서명이 달라서 백업을 거친 한 번의 이전이 필요합니다. 새 키가 과거 debug APK를 덮어 설치할 수 있게 만들지는 않습니다.

[Android 공식 앱 서명 안내](https://developer.android.com/studio/publish/app-signing)
