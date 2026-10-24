import { test } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { verifySubscriptions, verifyConfig } from "../tools/free-deploy.mjs";

test("only a complete, authenticated Free subscription list passes", () => {
  assert.doesNotThrow(() => verifySubscriptions({ success: true, result: [] }));
  assert.doesNotThrow(() => verifySubscriptions({ success: true, result: [{ rate_plan: { id: "WORKERS_FREE" }, price: 0, state: "active" }] }));
  for (const data of [null, { result: [] }, { success: false, result: [] },
    { success: true, result: [], result_info: { total_count: 1 } },
    { success: true, result: [{ rate_plan: { id: "WORKERS_PAID" }, price: 5, state: "active" }] },
    { success: true, result: [{ rate_plan: { id: "unknown-metered" }, price: 0, state: "active" }] },
    { success: true, result: [{ rate_plan: { id: "WORKERS_FREE" }, state: "active" }] }])
    assert.throws(() => verifySubscriptions(data));
});
test("extra paid bindings, routes, environments and remote configuration are refused", async () => {
  const config = JSON.parse(await readFile(new URL("../wrangler.jsonc", import.meta.url), "utf8"));
  assert.doesNotThrow(() => verifyConfig(config));
  for (const key of ["r2_buckets", "ai", "env", "routes", "containers", "account_id", "usage_model"])
    assert.throws(() => verifyConfig({ ...config, [key]: [] }));
  assert.throws(() => verifyConfig({ ...config, migrations: [{ tag: "v1", new_classes: ["ChatRoom"] }] }));
});
