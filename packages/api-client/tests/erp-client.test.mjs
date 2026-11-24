import assert from "node:assert/strict";
import test from "node:test";
import {
  createAccessTokenStore,
  createApiClient,
  createErpApi,
  redactSensitiveData,
} from "../dist/index.js";

test("redactSensitiveData masks password, accessToken, and authorization recursively", () => {
  const redacted = redactSensitiveData({
    identifier: "+966576171935",
    password: "secret-value",
    nested: { accessToken: "jwt-token", safe: "visible" },
  });

  assert.equal(redacted.identifier, "+966576171935");
  assert.equal(redacted.password, "[REDACTED]");
  assert.equal(redacted.nested.accessToken, "[REDACTED]");
  assert.equal(redacted.nested.safe, "visible");
});

test("idempotent ERP endpoints auto-attach X-Idempotency-Key GUID and emit redacted telemetry", async () => {
  const capturedCalls = [];
  const capturedHeaders = [];

  const tokenStore = createAccessTokenStore();
  tokenStore.setAccessToken("test-token");

  const { http } = createApiClient({
    baseURL: "/api/v1",
    tokenStore,
    onApiCall: (log) => capturedCalls.push(log),
    adapter: async (config) => {
      capturedHeaders.push({
        url: config.url,
        idempotencyKey: config.headers?.get?.("X-Idempotency-Key") ?? config.headers?.["X-Idempotency-Key"],
      });
      return {
        data: { id: "01933c90-9999-7000-8000-abcdef123456", status: "PendingApproval" },
        status: 201,
        statusText: "Created",
        headers: {},
        config,
      };
    },
  });

  const erp = createErpApi(http);
  await erp.sales.createOrder({
    customerId: "01933c90-9999-7000-8000-abcdef123456",
    warehouseId: 1,
    items: [{ masterProductId: "p1", productUnitId: "u1", orderedQty: 2, unitPrice: 20 }],
  });
  await erp.finance.recordCashCollection({
    customerId: "01933c90-9999-7000-8000-abcdef123456",
    amount: 50,
  });

  assert.equal(capturedHeaders.length, 2);
  assert.match(String(capturedHeaders[0].idempotencyKey), /^[0-9a-f-]{36}$/i);
  assert.match(String(capturedHeaders[1].idempotencyKey), /^[0-9a-f-]{36}$/i);
  assert.equal(capturedCalls.length, 2);
  assert.equal(capturedCalls[0].method, "POST");
  assert.equal(capturedCalls[0].status, 201);
});
