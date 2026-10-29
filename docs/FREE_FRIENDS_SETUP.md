통합판 안내 2026-10-29

이 문서는 cms6727의 외부 서비스 실험 원본 자료입니다. 현재 codex/windows-app 통합 빌드는 외부 연결·카카오 SDK 초기화·콜백·로그인·업로드를 비활성화하고 공개 키와 서버 주소를 APK에 넣지 않습니다. 아래에 나오는 배포 상태나 별도 앱 ID는 이 통합판에 적용되지 않습니다. 이 작업에서는 로그인·클라우드 배포·서버 설정 변경·실제 서버 검사를 수행하지 않았습니다. 현재 사용법은 docs/INTEGRATION-2026-10-29.md를 참고하세요.

---

# 무료 친구 초대 · 일기 공유 (0.14.0)

## 현재 상태

앱과 Supabase 데이터베이스 마이그레이션을 준비하고 **서울 지역의 Free 프로젝트 `fvdwubqmeaaqaltscfwh`**에 접근 규칙과 두 인증 함수를 배포했습니다. 카카오 연결 해제 웹훅도 등록했고, 제공 APK에 공개 서버 설정을 넣었습니다. 실제 서버에서 비공개 사진 설정, 로그인 전 접근 차단, 관리자 기능 차단을 확인했습니다. **두 휴대폰에서의 카카오 로그인·공유 검증은 아직 완료 전입니다.**

Render 유료 서버 설정 파일은 제거했습니다. 아래 작업은 Supabase **Free** 조직·프로젝트에서만 진행합니다. 결제수단, Pro 업그레이드, 유료 추가 기능, 별도 도메인은 필요하지 않습니다. 무료 한도를 넘으면 기능이 제한될 수 있으며, 유료 요금제로 자동 전환시키는 코드는 없습니다.

## 사용 순서

1. 두 사람이 같은 버전의 **마이데이 친구** 앱을 설치합니다.
2. 친구 일기에서 카카오로 로그인합니다.
3. A가 **친구 초대 → 카톡으로 보내기**로 코드를 보냅니다. 코드는 64비트 난수이며, 24시간 동안 한 번 사용합니다. 새로 만들면 이전 코드는 무효가 됩니다.
4. B가 **코드 입력**으로 요청합니다. 아직 A의 글이나 사진은 열리지 않습니다.
5. A가 **친구 관리 → 새로고침 → 수락**을 선택합니다.
6. 일기에서 공개할 글·사진을 직접 체크하고 미리보기를 확인한 다음 공개합니다. 기분은 별도 선택합니다.
7. 공개 내리기 또는 친구 끊기 후에는 서버가 다음 글·사진 조회부터 차단합니다. 이미 상대 기기에 표시되거나 저장된 사본까지 회수하는 기능은 아닙니다.

기존 개인 일기의 자동 저장·사진 편집·백업과 강아지 아이콘을 유지합니다. 개인 일기 전체를 서버에 동기화하지 않으며, 공개 사본은 다시 공개하기 전까지 자동 갱신되지 않습니다.

## 무료 프로젝트 연결

1. Supabase 계정에 로그인하고 **Free** 조직에서 `myday-friends` 프로젝트를 만듭니다. 가능한 경우 서울 지역을 선택합니다. DB 비밀번호를 채팅이나 소스 저장소에 적지 않습니다.
2. SQL Editor에서 `supabase/migrations/`의 `202610290001_friend_invites.sql`, `202610290002_native_kakao_auth.sql`을 순서대로 한 번씩 적용합니다. 테이블은 외부 API에 직접 노출하지 않고, 로그인한 사용자용 RPC와 비공개 사진 저장소 정책만 제공합니다.
3. 카카오 개발자 앱 `1626827`의 로그인 ON, 닉네임 선택 동의, Android 패키지 `com.myday.diary.friends.preview`와 키 해시 `RV1ddFGASPyCUBtL2te84qm2KDQ=`를 등록합니다. 이미 이 PC의 테스트 APK에 맞춰 등록했습니다. 친구 목록 권한은 사용하지 않습니다.
4. `supabase/functions/myday-kakao-login`과 `myday-kakao-events`를 배포합니다. CLI는 `supabase/config.toml` 설정을 사용합니다. Dashboard Editor에서는 `tools/bundle-friends-functions.py`로 만든 단일 파일을 배포할 수 있습니다. 두 함수는 각각 카카오 액세스 토큰·카카오 서명을 자체 검증하므로 해당 함수에 한해 legacy JWT 확인을 끕니다. DB·Storage의 사용자 인증과 권한 검사는 유지합니다.
5. 함수 전용 환경변수에 `MYDAY_KAKAO_APP_ID=1626827`, 대표 REST API 키 `MYDAY_KAKAO_REST_KEY`, 충분히 긴 임의의 `MYDAY_AUTH_PEPPER`를 설정합니다. Pepper는 내부 계정 별칭을 만드는 서버 전용 값입니다. Supabase가 제공하는 서버 키는 Edge Function 안에서만 사용하며, 이 값을 APK·문서·Git에 넣지 않습니다.
6. 카카오 콘솔의 **앱 → 웹훅 → 계정 상태 변경 웹훅**에 `https://fvdwubqmeaaqaltscfwh.supabase.co/functions/v1/myday-kakao-events`를 등록하고 OAUTH **User Unlinked** 이벤트만 선택합니다. 앱 외부에서 카카오 연결을 끊으면 서버 접근을 차단하고 공개 기록·사진·온라인 계정을 정리합니다. 실패한 정리는 재전송 또는 운영자 점검이 필요합니다.
7. Supabase Project Settings → API Keys의 **publishable key**, 프로젝트 HTTPS URL, 카카오 네이티브 앱 키를 이 PC의 `local.properties`에 넣습니다. 이 파일은 Git 및 소스 배포 파일에서 제외합니다.

Supabase의 일반 회원가입·익명 로그인은 끕니다. Email provider와 Confirm email은 유지합니다. 검증된 카카오 로그인만 서버 관리자 API로 내부 계정을 생성하며, 사용자 이메일이나 가입 메일을 요구하지 않습니다.

연결 해제 웹훅 URL과 User Unlinked 이벤트는 등록했습니다. 카카오 테스트 도구에서는 먼저 계정을 앱에 연결해야 하므로, 실제 서명 알림의 종단 확인은 휴대폰에서 첫 로그인 후 진행합니다. 이 알림은 공개 기록과 온라인 계정을 삭제하는 기능이므로 실제 사용 기록으로 임의 테스트하지 않습니다.

```properties
MYDAY_SUPABASE_URL=https://fvdwubqmeaaqaltscfwh.supabase.co
MYDAY_SUPABASE_KEY=sb_publishable_발급받은공개키
MYDAY_KAKAO_NATIVE_KEY=발급받은32자리네이티브앱키
```

**secret key, service_role 키, DB 비밀번호, 카카오 클라이언트 시크릿은 APK에 넣지 않습니다.** 빌드는 `sb_publishable_` 키만 허용합니다. 카카오 Android SDK로 받은 액세스 토큰을 함수가 카카오의 토큰 정보·회원 정보 API로 검증한 다음 Supabase 세션을 발급합니다. 앱 ID·만료·회원번호 일치를 모두 확인하고 검증 전에는 관리자 API를 호출하지 않습니다. 세션은 Android Keystore로 암호화하며 카카오 토큰은 세션 연결 후 로컬 SDK 저장소에서 지웁니다.

Supabase 기본 Kakao OAuth에는 비즈 앱 미등록 상태에서 이메일 scope가 강제로 요청되는 [알려진 문제](https://github.com/supabase/auth/issues/2574)가 있어 사용하지 않습니다. 이 버전은 이메일·전화번호·카카오 친구 목록·OAuth 클라이언트 시크릿을 요구하지 않습니다. 서버에서만 생성·소비하는 일회성 토큰으로 내부 Supabase 계정을 연결하며 이메일을 발송하지 않습니다.

## 빌드와 검사

```powershell
.\gradlew.bat :app:assembleDebug :app:testDebugUnitTest :app:lintDebug
```

한글 경로에서 Gradle 테스트 클래스 로딩 문제가 있으면 빌드 출력 폴더를 영문 경로로 지정합니다. 이 PC에는 `C:/Users/cms67/.cache/myday-friends-build/ascii-build.init.gradle`을 준비했습니다.

```powershell
node 경로/pnpm.cjs --dir supabase/tests install --frozen-lockfile
node 경로/pnpm.cjs --dir supabase/tests test
```

정책 검사는 실제 PostgreSQL 엔진인 PGlite에서 Supabase 역할·인증·Storage 테이블 구조를 구성하고 실행합니다. 초대 재사용·만료·추측 제한, 수락 전/후 권한, 타인 접근 차단, 사진 경로 추측, 게시자 권한, 공개 철회, 친구 해제, 원본 일기 전송 차단을 확인합니다. 카카오 인증 검사에서는 다른 앱·만료·위조 회원번호를 차단하고 관리자 일회성 토큰이 앱 응답으로 유출되지 않는지 확인합니다. 배포한 Supabase 서비스와 휴대폰에서의 종단 검사를 대체하지는 않습니다.

## 보관과 삭제

- 글·기분·블록은 선택한 공개 사본만 DB에 저장합니다. 한 번에 기록 30개·사진 6장, 공개 보관은 최대 366일입니다.
- 사진은 비공개 `diary-photos` 버킷의 JPEG이며 한 장에 최대 2MB입니다. 사진 파일 주소를 알아도 로그인 및 수락된 친구 관계가 없으면 내려받을 수 없습니다. 공개 URL이나 장기간 유효한 서명 URL을 발급하지 않습니다.
- 공개 내리기·교체 후 더 이상 참조하지 않는 사진은 삭제합니다. 연결 실패 시 앱에 삭제 목록을 보관하고 다음 연결에서 재시도합니다.
- **공개 기록과 친구 연결 모두 지우기**는 공개 글·사진, 친구·초대·요청을 정리합니다. 카카오 로그인 계정 자체의 회원 탈퇴와는 별도이며, 휴대폰의 개인 일기는 유지합니다.
- 일반 사용자 배포 전 실제 운영자의 개인정보 처리방침과 카카오 외부 연결 해제 알림의 실제 동작을 확인해야 합니다. 운영자 정보·약관 동의·카카오 심사 결과를 임의로 작성하지 않습니다.

## 무료 한도

2026-10-29 확인 기준 Supabase Free는 DB 500MB, 전체 사진 저장 공간 1GB, 기본 전송량 월 5GB입니다. 비활성 상태가 일주일 지속되면 프로젝트가 일시 정지될 수 있습니다. 한도를 넘으면 요금제를 올리는 대신 사진·공개 글을 정리하거나 사용을 제한합니다. 개인 일기는 휴대폰에 남으며, 정기적으로 JSON 백업을 내보내는 것이 좋습니다.

- [무료 요금제](https://supabase.com/pricing)
- [무료 한도 초과 시 동작](https://supabase.com/docs/guides/platform/billing-faq)
- [Kakao OAuth](https://supabase.com/docs/guides/auth/social-sign-in/auth-kakao)
- [PKCE](https://supabase.com/docs/guides/auth/sessions/pkce-flow)
- [Storage 접근 제어](https://supabase.com/docs/guides/storage/security/access-control)
