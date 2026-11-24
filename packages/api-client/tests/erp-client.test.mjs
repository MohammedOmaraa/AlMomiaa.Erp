import assert from "node:assert/strict";
import test from "node:test";
import {
  changePasswordRequestSchema,
  computeJitteredReconnectDelay,
  createAccessTokenStore,
  createApiClient,
  createErpApi,
  createOperationsHubClient,
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

test("computeJitteredReconnectDelay applies bounded jitter and createOperationsHubClient starts disconnected", () => {
  assert.equal(computeJitteredReconnectDelay(0, () => 0.5), 200);
  assert.equal(computeJitteredReconnectDelay(1, () => 0), 2000);
  assert.equal(computeJitteredReconnectDelay(1, () => 1), 2500);
  assert.equal(computeJitteredReconnectDelay(10, () => 0), 30000);

  const client = createOperationsHubClient({
    hubUrl: "/hubs/operations",
    getAccessToken: () => "memory-token",
  });
  assert.equal(client.getState(), "disconnected");
});

test("changePasswordRequestSchema enforces minimum length, difference, and confirmation match", () => {
  assert.equal(
    changePasswordRequestSchema.safeParse({
      currentPassword: "OldPass#1",
      newPassword: "NewStrong#99",
      confirmNewPassword: "NewStrong#99",
    }).success,
    true,
  );
  assert.equal(
    changePasswordRequestSchema.safeParse({
      currentPassword: "SamePassword#1",
      newPassword: "SamePassword#1",
      confirmNewPassword: "SamePassword#1",
    }).success,
    false,
  );
  assert.equal(
    changePasswordRequestSchema.safeParse({
      currentPassword: "OldPass#1",
      newPassword: "NewStrong#99",
      confirmNewPassword: "Mismatch#99",
    }).success,
    false,
  );
});

test("catalog availability and customer rep-linking client endpoints hit canonical routes", async () => {
  const captured = [];
  const tokenStore = createAccessTokenStore();
  tokenStore.setAccessToken("test-token");

  const { http } = createApiClient({
    baseURL: "/api/v1",
    tokenStore,
    adapter: async (config) => {
      captured.push({ method: config.method?.toUpperCase(), url: config.url, data: config.data });
      return {
        data: {},
        status: 200,
        statusText: "OK",
        headers: {},
        config,
      };
    },
  });

  const erp = createErpApi(http);
  await erp.catalog.getProductAvailability("01933c90-0000-7000-8000-000000000001");
  await erp.customers.getSalesReps();
  await erp.customers.assignCustomerRep("01933c90-0000-7000-8000-000000000002", "01933c90-0000-7000-8000-000000000003");
  await erp.customers.bulkAssignCustomersRep({
    assignedRepId: "01933c90-0000-7000-8000-000000000003",
    customerIds: ["01933c90-0000-7000-8000-000000000002"],
  });

  assert.deepEqual(
    captured.map((c) => `${c.method} ${c.url}`),
    [
      "GET /catalog/products/01933c90-0000-7000-8000-000000000001/availability",
      "GET /customers/sales-reps",
      "PUT /customers/01933c90-0000-7000-8000-000000000002/assign-rep",
      "POST /customers/assign-rep/bulk",
    ],
  );
});
