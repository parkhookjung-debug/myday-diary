// Isolated local Workers runtime. No Cloudflare account, network deployment or real diary.
import { Miniflare, convertV4MiniflareOptions } from "miniflare";
import { mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, resolve, relative, isAbsolute } from "node:path";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";
const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const exe = process.argv[2];
if (!exe || process.argv.length > 4) throw new Error("Usage: npm run test:native -- /absolute/path/to/MyDay[.exe] [screenshot-directory]");
const temporary = await mkdtemp(resolve(tmpdir(), "myday-free-native-"));
const code = "local-test-code-only";
const server = new Miniflare(convertV4MiniflareOptions({ modules: true, scriptPath: resolve(root, ".test-worker/index.js"),
  compatibilityDate: "2026-10-17", compatibilityFlags: ["nodejs_compat"],
  host: "127.0.0.1", port: 0, durableObjects: { ROOMS: { className: "ChatRoom", useSQLite: true } },
  bindings: { MYDAY_CHAT_JOIN_CODE: code }, durableObjectsPersist: temporary,
}));
async function native(args) {
  await new Promise((resolveRun, reject) => {
    const child = spawn(resolve(exe), args, { windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
    const timer = setTimeout(() => { child.kill(); reject(new Error("Native chat test timed out")); }, 65_000);
    child.stdout.on("data", chunk => process.stdout.write(chunk));
    child.stderr.on("data", chunk => process.stderr.write(chunk));
    child.on("error", error => { clearTimeout(timer); reject(error); });
    child.on("exit", status => { clearTimeout(timer); status === 0 ? resolveRun() : reject(new Error("Native chat test failed: " + status)); });
  });
}
try {
  const address = (await server.ready).toString();
  const before = await fetch(address + "health");
  if (!before.ok) throw new Error("Local Workers server is not ready");
  await native(["--chat-test", address, code]);
  if (process.platform === "win32" && process.argv[3])
    await native(["--chat-smoke", address, code, resolve(process.argv[3])]);
  console.log("Native app + local Cloudflare Workers contract passed (no deployment).");
} finally {
  await server.dispose();
  const remaining = relative(resolve(tmpdir()), resolve(temporary));
  if (remaining.startsWith("..") || isAbsolute(remaining) || !remaining.startsWith("myday-free-native-"))
    throw new Error("Refusing cleanup outside the native test temporary directory");
  await rm(temporary, { recursive: true, force: true });
}
