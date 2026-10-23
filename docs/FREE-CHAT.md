# 비용 0원으로 시작하는 Mac·Windows 채팅

**현재는 로컬 개발 단계입니다. 외부 서버를 배포하지 않았고, 유료 구독·결제수단·도메인을 만들지 않았습니다.** 사용자가 무료 계정을 준비하기 전에는 로컬 검사만 진행합니다.

Windows 기존 일기 앱과 `macos/`의 기존 SwiftUI 일기 앱에서 **상몬 라운지 · 채팅**을 엽니다. 같은 서버 주소와 초대코드를 사용하면 OS와 관계없이 같은 방에 참여합니다. Windows EXE를 Mac에서 실행하는 구조가 아닙니다.

## 로컬에서 실행하기

Node.js 24와 npm이 있는 컴퓨터에서:

```sh
cd server/cloudflare
npm ci --no-audit --no-fund
npm run dev
```

서버 주소는 `http://127.0.0.1:8787/`입니다. 첫 실행에 로컬 전용 초대코드를 `.dev.vars`에 만듭니다. 해당 파일의 `MYDAY_CHAT_JOIN_CODE=` 뒤 값을 앱의 초대코드 칸에 넣습니다. 파일은 Git에서 제외하며 코드값을 로그에 출력하지 않습니다. 앱마다 다른 닉네임을 사용하세요. 서버 종료는 Ctrl+C입니다.

기본 모드는 같은 컴퓨터에서만 연결할 수 있습니다. **Windows와 Mac 두 대를 같은 Wi-Fi에서 시험하려면** 서버 컴퓨터에서 대신 다음을 실행합니다.

```sh
npm run dev:lan
```

다른 컴퓨터는 `http://서버PC의사설IPv4:8787/`을 입력합니다. 예: `http://192.168.0.10:8787/`. 개인 네트워크 접속 허용이 필요하면 운영자가 방화벽 설정을 확인합니다. 이 도구는 방화벽·공유기·포트 포워딩을 변경하지 않습니다. 카페 등 공용 Wi-Fi에서 테스트용 HTTP 채팅을 열지 마세요. 인터넷 접속을 추가할 때는 HTTPS만 사용합니다.

실행 도구는 개발자용 데이터 탐색 API와 로컬 요청 추적을 끄고, 디버거 주소를 이 컴퓨터에 한정합니다. 서버 설정에 추가 서비스가 들어가면 로컬 실행도 중단합니다.

로컬 메시지는 `.wrangler/state/`의 에뮬레이터 SQLite에 저장됩니다. 같은 폴더로 다시 실행하면 대화를 유지합니다. 자동 검사는 임시 DB와 가짜 초대코드를 사용하며 실제 메시지·개인 일기는 읽지 않습니다.

## 비용이 생기지 않게 하는 구성

| 항목 | 선택 |
| --- | --- |
| 현재 실행 | 이 컴퓨터의 로컬 Workers 에뮬레이터. 클라우드 서버 요금 없음 |
| 향후 공개 서버 | **결제수단 없는 별도의 Workers Free 전용 계정** |
| 저장 | Free에서 지원하는 SQLite Durable Object 한 채팅방 |
| 주소 | 제공되는 `workers.dev` 사용. 도메인 구매 없음 |
| 무료 한도 초과 | 서비스 요청 실패·중단. 자동 유료 전환 코드 없음 |
| 추가 서비스 | R2·유료 DB·AI API·유료 인증·메일·SMS 사용 안 함 |
| CI | 공개 저장소의 표준 GitHub 호스팅 러너만 사용. 외부 배포 없음 |

앱 안에는 유료 전환·결제·재충전 기능이 없습니다. **유료 계정의 무료 사용량은 ‘청구 상한 0원’과 다릅니다.** 따라서 계정이 유료이거나 무료인지 검증할 수 없으면 배포 도구가 중단합니다. 예산 알림을 과금 차단 장치로 취급하지 않습니다.

공개 배포용 `npm run deploy`는 계정 구독 목록을 읽기 전용으로 검사하고 배포 직전에 다시 확인합니다. 유료·종량제·알 수 없는 구독, 읽기 권한 부족, 일부만 반환된 구독 목록, 추가 서비스 설정은 거부합니다. 결제·플랜 변경 API는 호출하지 않습니다. CLI 직접 실행이나 계정의 향후 수동 유료 전환까지 코드가 막는 것은 아니므로, 무료 전용 계정을 유지해야 합니다.

## 사용량과 동작

앱 자체 제한은 공용방 1개, 동시 세션 16개, 하루 API 30,000회·새 메시지 1,500개·입장 500회입니다. 날짜는 UTC 기준이며 한국 시간 오전 9시에 바뀝니다. DB에는 최근 1,000개 메시지를 남깁니다. 5초 간격으로 새 메시지를 확인합니다. 5명이 8시간 동안 접속하면 조회만 약 28,800회이므로 다른 요청까지 더하면 하루 제한에 먼저 도달할 수 있습니다. 이는 무료 범위를 보수적으로 쓰기 위한 제품 제한이며 Cloudflare 실제 사용량과 같지 않습니다. 플랫폼 한도가 먼저 소진되면 그때 중단될 수도 있습니다.

출석·일기·장비·뽑기 데이터는 서버로 보내지 않습니다. 채팅에 직접 보낸 텍스트만 저장합니다. 세션 토큰은 앱 메모리에 두고 서버 DB에는 해시로 보관합니다. 닉네임은 계정 소유를 보증하지 않습니다. 초대코드를 바꾸면 이전 세션으로 새 요청을 할 수 없습니다.

## 계정 준비 후의 별도 배포 단계

지금 실행할 단계가 아닙니다. 무료 계정을 준비한 뒤 로그인·무료 상태 검증부터 진행합니다.

```sh
npx wrangler login
npm run check:free
npm run deploy
```

여러 계정이 있으면 무료 계정의 `CLOUDFLARE_ACCOUNT_ID`를 명시합니다. 로그인 권한으로 구독 조회를 할 수 없는 경우 필요한 읽기 권한을 갖춘 API 토큰을 로컬 환경변수로 제공해야 합니다. 토큰을 채팅이나 GitHub에 올리지 마세요. 확인이 안 되면 배포하지 않습니다.

실제 배포 초대코드는 `.wrangler/deploy-secrets.json`에 새로 생성하며 로컬 `.dev.vars`와 다릅니다. 외부 서버와 클라이언트 검사를 마친 후에만 서버 주소를 공유합니다. GitHub Actions에는 배포 단계나 Cloudflare 인증키를 넣지 않습니다.

## Mac에서 실행하기

macOS 13 이상에서 Command Line Tools를 설치하고 `zsh macos/build.command`로 기존 일기 앱을 빌드합니다. 결과는 `macos/build/MyDay.app`입니다. 일기 화면과 메뉴에 **상몬 라운지 · 채팅** 버튼이 있습니다. 보내기는 버튼 또는 ⌘+Enter이며 Enter는 줄바꿈입니다. 창을 닫으면 채팅 연결을 정리하고 일기는 계속 사용할 수 있습니다.

2026-10-23 공통화에서 Android 라운지도 같은 API에 연결합니다. Mac의 성장·소품·공방·버전 8 백업도 함께 보강했습니다. [세 플랫폼 수정 위치·기기별 접속 주소](THREE-PLATFORMS.md)를 확인하세요.

## 수정할 파일과 검사

| 변경 | 파일 |
| --- | --- |
| 무료 서버·초대·저장·사용량 제한 | [index.ts](../server/cloudflare/src/index.ts) |
| 무료 계정 확인·배포 차단 | [free-deploy.mjs](../server/cloudflare/tools/free-deploy.mjs) |
| 로컬 실행·LAN 모드 | [local-dev.mjs](../server/cloudflare/tools/local-dev.mjs) |
| Mac 채팅 통신 | [ChatClient.swift](../macos/Sources/MyDay/ChatClient.swift) |
| Mac 채팅 화면 | [ChatView.swift](../macos/Sources/MyDay/ChatView.swift) |
| Mac 일기에서 열기 | [MyDay.swift](../macos/Sources/MyDay/MyDay.swift) |
| Windows 채팅 통신 | [ChatClient.cs](../windows/Chat/ChatClient.cs) |
| 실제 앱과 로컬 서버 연결 검사 | [test-native.mjs](../server/cloudflare/tools/test-native.mjs) |
| 향후 두 OS 빌드 검사 | [chat-compatibility.yml](../.github/workflows/chat-compatibility.yml) |

서버 폴더에서 `npm run check`, `npm test`, `npm run build`를 실행합니다. build는 **dry-run**이므로 외부에 배포하지 않습니다. Windows 앱 빌드 뒤 `npm run test:native -- ../../windows/bin/MyDay.exe ../../.bootstrap/free-chat-smoke`로 실제 EXE와 로컬 Workers 서버의 통신·창 동작을 검사합니다. Mac에서는 `npm run test:native -- ../../macos/build/MyDay.app/Contents/MacOS/MyDay`를 실행합니다.

서버 기능·배포 차단 테스트는 계정 없이 실행할 수 있습니다. 2026-10-17 소스 커밋 `1b99609`의 [자동 검사](https://github.com/parkhookjung-debug/myday-diary/actions/runs/39037445085)에서 서버 검사와 Windows·Mac의 빌드·실제 앱 통신·채팅 화면 렌더링이 통과했습니다. 해당 화면의 **Artifacts**에는 Windows EXE와 Apple Silicon Mac 앱 ZIP을 7일간 보관합니다. Intel Mac과 사용자 기기에서의 실행은 추가 확인 대상입니다. 외부 서버는 배포하지 않았습니다.

공식 근거(2026-10-17 확인): [Workers 요금](https://developers.cloudflare.com/workers/platform/pricing/) · [Durable Objects Free 한도 초과 시 실패](https://developers.cloudflare.com/durable-objects/platform/pricing/) · [GitHub 공개 저장소 표준 러너 무료](https://docs.github.com/en/actions/concepts/billing-and-usage).
