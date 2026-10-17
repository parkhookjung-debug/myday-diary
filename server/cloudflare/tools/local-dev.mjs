import { readFile, writeFile } from "node:fs/promises";
import { randomBytes } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";
import { verifyConfig } from "./free-deploy.mjs";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
if (process.argv.slice(2).some(value => value !== "--lan")) throw new Error("지원하는 옵션은 --lan뿐입니다.");
verifyConfig(JSON.parse(await readFile(resolve(root, "wrangler.jsonc"), "utf8")));
const file = resolve(root, ".dev.vars");
try { await readFile(file); }
catch { await writeFile(file, "MYDAY_CHAT_JOIN_CODE=" + randomBytes(24).toString("base64url") + "\n", { flag: "wx", mode: 0o600 }); }
console.log("로컬 서버만 실행합니다. Cloudflare 계정·배포·결제가 필요하지 않습니다.");
console.log("서버 주소: http://127.0.0.1:8787/ · 초대코드: server/cloudflare/.dev.vars 파일에서 확인하세요.");
const lan = process.argv.includes("--lan");
if (lan) console.log("같은 Wi-Fi 기기는 이 컴퓨터의 사설 IPv4:8787로 접속하세요. 방화벽은 자동으로 변경하지 않습니다.");
const child = spawn(process.execPath, [resolve(root, "node_modules/wrangler/bin/wrangler.js"), "dev", "--local",
  "--ip", lan ? "0.0.0.0" : "127.0.0.1", "--port", "8787", "--inspector-ip", "127.0.0.1", "--show-interactive-dev-session=false"], {
  cwd: root, stdio: "inherit", windowsHide: true, env: { ...process.env,
    WRANGLER_SEND_METRICS: "false", WRANGLER_LOG_PATH: resolve(root, ".wrangler/logs"),
    X_LOCAL_EXPLORER: "false", X_LOCAL_OBSERVABILITY: "false" },
});
child.on("error", () => { console.error("로컬 서버를 시작하지 못했습니다. Node.js와 npm ci 실행 여부를 확인하세요."); process.exitCode = 1; });
child.on("exit", code => { process.exitCode = code ?? 1; });
for (const signal of ["SIGINT", "SIGTERM"]) process.on(signal, () => child.kill(signal));
