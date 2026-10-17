// Never enables billing or creates a subscription. All preflight API calls are GET.
import { readFile, mkdir, writeFile } from "node:fs/promises";
import { homedir } from "node:os";
import { resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";
import { spawnSync } from "node:child_process";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const FREE_PLANS = new Set(["free", "WORKERS_FREE", "workers_free", "TEAMS_FREE"]);
export function verifySubscriptions(data) {
  if (data?.success !== true || !Array.isArray(data.result)) throw new Error("무료 계정 확인 실패: 구독 목록을 읽지 못했습니다.");
  const info = data.result_info;
  if (info && ((info.total_pages ?? 1) > 1 || (info.total_count ?? data.result.length) > data.result.length))
    throw new Error("무료 계정 확인 실패: 구독 목록 일부만 반환됐습니다.");
  // No subscriptions is the default Free account. Unknown or metered plans fail closed,
  // even if the advertised base price happens to be zero.
  for (const subscription of data.result) {
    if (!FREE_PLANS.has(subscription?.rate_plan?.id) || subscription.price !== 0 || subscription.state !== "active")
      throw new Error("배포 중단: 유료 또는 확인할 수 없는 구독이 있는 계정입니다. 결제수단 없는 무료 전용 계정을 사용하세요.");
  }
}
export function verifyConfig(config) {
  const allowed = new Set(["$schema", "name", "main", "compatibility_date", "compatibility_flags", "workers_dev",
    "preview_urls", "observability", "secrets", "durable_objects", "migrations"]);
  if (Object.keys(config).some(k => !allowed.has(k)) || config.name !== "myday-free-chat" || config.main !== "src/index.ts"
    || config.workers_dev !== true || config.preview_urls !== false || JSON.stringify(config.observability) !== JSON.stringify({ enabled: false })
    || JSON.stringify(config.durable_objects) !== JSON.stringify({ bindings: [{ name: "ROOMS", class_name: "ChatRoom" }] })
    || JSON.stringify(config.migrations) !== JSON.stringify([{ tag: "v1", new_sqlite_classes: ["ChatRoom"] }])
    || JSON.stringify(config.secrets) !== JSON.stringify({ required: ["MYDAY_CHAT_JOIN_CODE"] }))
    throw new Error("배포 중단: 무료 채팅 전용 설정이 변경됐습니다. 유료 서비스·도메인·원격 바인딩을 추가하지 마세요.");
}
async function credential() {
  if (process.env.CLOUDFLARE_API_TOKEN) return process.env.CLOUDFLARE_API_TOKEN;
  const candidates = [
    resolve(homedir(), ".wrangler/config/default.toml"),
    resolve(process.env.APPDATA || homedir(), "xdg.config/.wrangler/config/default.toml"),
    resolve(process.env.XDG_CONFIG_HOME || resolve(homedir(), ".config"), ".wrangler/config/default.toml"),
  ];
  for (const path of candidates) {
    let content;
    try { content = await readFile(path, "utf8"); } catch { continue; }
    const token = content.match(/^oauth_token\s*=\s*"([^"\r\n]+)"/m)?.[1];
    const expires = content.match(/^expiration_time\s*=\s*"([^"\r\n]+)"/m)?.[1];
    if (token && expires && Date.parse(expires) > Date.now() + 120_000) return token;
  }
  throw new Error("Cloudflare 로그인이 필요합니다. npx wrangler login 후 다시 실행하세요. 인증키를 채팅·GitHub에 올리지 마세요.");
}
async function get(path, token) {
  const response = await fetch("https://api.cloudflare.com/client/v4" + path, {
    headers: { Authorization: "Bearer " + token }, signal: AbortSignal.timeout(15_000), redirect: "error",
  });
  if (!response.ok) throw new Error("무료 계정 확인 실패: 읽기 API 권한 또는 로그인 상태를 확인하세요. 배포하지 않았습니다.");
  return response.json();
}
export async function main(args = process.argv.slice(2)) {
  if (args.some(a => a !== "--check")) throw new Error("지원하는 옵션은 --check뿐입니다. 배포 인자 우회는 허용하지 않습니다.");
  const config = JSON.parse(await readFile(resolve(root, "wrangler.jsonc"), "utf8"));
  verifyConfig(config);
  const token = await credential();
  let account = process.env.CLOUDFLARE_ACCOUNT_ID;
  if (!account) {
    const accounts = await get("/accounts?per_page=50", token);
    if (!accounts.success || accounts.result?.length !== 1 || (accounts.result_info?.total_count ?? 1) !== 1)
      throw new Error("무료 전용 계정의 CLOUDFLARE_ACCOUNT_ID를 지정하세요. 계정을 자동 선택하지 않습니다.");
    account = accounts.result[0].id;
  }
  if (!/^[a-f0-9]{32}$/.test(account)) throw new Error("Cloudflare 계정 ID 형식이 올바르지 않습니다.");
  const check = async () => verifySubscriptions(await get(`/accounts/${account}/subscriptions`, token));
  await check();
  console.log("무료 구독 확인 완료. 유료 전환·결제·도메인 구매 API는 사용하지 않습니다.");
  if (args.includes("--check")) return;
  await mkdir(resolve(root, ".wrangler"), { recursive: true });
  const secretFile = resolve(root, ".wrangler/deploy-secrets.json");
  // Keep the same invitation across deployments; never print its value.
  try { await readFile(secretFile); }
  catch { await writeFile(secretFile, JSON.stringify({ MYDAY_CHAT_JOIN_CODE: randomBytes(24).toString("base64url") }), { flag: "wx", mode: 0o600 }); }
  let saved;
  try { saved = JSON.parse(await readFile(secretFile, "utf8")); }
  catch { throw new Error("초대코드 파일의 JSON 형식을 확인하세요. 값은 출력하지 않습니다."); }
  if (typeof saved.MYDAY_CHAT_JOIN_CODE !== "string" || saved.MYDAY_CHAT_JOIN_CODE.length < 12)
    throw new Error("초대코드 파일이 올바르지 않습니다. 기존 파일을 확인하세요.");
  // Recheck immediately before the only remote mutation (Worker deployment).
  await check();
  const result = spawnSync(process.execPath, [resolve(root, "node_modules/wrangler/bin/wrangler.js"),
    "deploy", "--config", resolve(root, "wrangler.jsonc"), "--secrets-file", secretFile, "--no-autoconfig"], {
    cwd: root, stdio: "inherit", env: { ...process.env, CLOUDFLARE_API_TOKEN: token,
      CLOUDFLARE_ACCOUNT_ID: account, WRANGLER_SEND_METRICS: "false", WRANGLER_LOG_PATH: resolve(root, ".wrangler/logs") },
  });
  if (result.status !== 0) throw new Error("배포에 실패했습니다. 유료 플랜으로 변경하지 말고 원인을 확인하세요.");
  console.log("초대코드는 로컬 .wrangler/deploy-secrets.json에 보관했습니다. GitHub에 올리지 마세요.");
}
if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href)
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
