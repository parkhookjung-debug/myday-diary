# Mac·Windows 공통 채팅 · 무료 플랜용 서버

**현재는 계정 연결 없이 로컬에서만 개발합니다.** [실행 순서·0원 구성·무료 계정 배포 차단·수정 파일](../../docs/FREE-CHAT.md)을 먼저 확인하세요.

```sh
npm ci --no-audit --no-fund
npm run dev
```

이 컴퓨터에서는 `http://127.0.0.1:8787/`을 사용합니다. 초대코드는 처음 실행할 때 만드는 `.dev.vars`에서 확인합니다. 두 대를 같은 Wi-Fi로 연결할 때는 `npm run dev:lan`을 사용합니다. 일기 앱의 **상몬 라운지 · 채팅**에서 같은 주소와 초대코드, 서로 다른 닉네임으로 입장하세요.

`src/index.ts`: 공통 API·SQLite·세션·보관·이용량 제한. `tools/free-deploy.mjs`: 무료 상태를 검증하지 못하면 배포 중단. `tools/local-dev.mjs`: 계정 없는 로컬 실행. `test/`: 실제 Workers 런타임과 배포 차단 검사.

비밀 값·`.dev.vars`·`.wrangler/`·대화 DB·`node_modules/`는 Git에 올리지 않습니다. `npm run build`는 dry-run이며 외부 배포가 아닙니다. 공개 서버 배포는 무료 전용 계정을 준비한 후 별도 단계입니다.
