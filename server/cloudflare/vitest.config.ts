import { defineConfig } from "vitest/config";
import { cloudflareTest } from "@cloudflare/vitest-plugin";

export default defineConfig({
  plugins: [cloudflareTest({
    remoteBindings: false,
    wrangler: { configPath: "./wrangler.jsonc" },
    miniflare: { bindings: { MYDAY_CHAT_JOIN_CODE: "local-test-code-only" } },
  })],
  test: { include: ["test/**/*.test.ts"] },
});
