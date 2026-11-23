import test from "node:test";
import assert from "node:assert/strict";
import { AxiosError, AxiosHeaders } from "axios";
import {
  authenticationResponseSchema,
  createAccessTokenStore,
  createApiClient,
  createAuthService,
  loginRequestSchema,
} from "../dist/index.js";

const sampleSession = {
  accessToken: "jwt-access-token-new",
  tokenType: "Bearer",
  expiresIn: 600,
  user: {
    id: "01933c90-9999-7000-8000-abcdef123456",
    employeeCode: "EMP-0001",
    fullNameAr: "اسم المستخدم",
    fullNameEn: "User Name",
    roleCodes: ["SALES_MGR"],
    permissions: ["orders:approve"],
    repType: null,
    defaultWarehouseId: 1,
    assignedWarehouseIds: [1, 2],
  },
};

test("Zod schemas validate canonical contracts and reject refreshToken in JSON", () => {
  const validLogin = loginRequestSchema.parse({ identifier: " EMP-0001 ", password: "secret" });
  assert.equal(validLogin.identifier, "EMP-0001");

  const validResponse = authenticationResponseSchema.parse(sampleSession);
  assert.equal(validResponse.expiresIn, 600);

  assert.throws(() => {
    authenticationResponseSchema.parse({
      ...sampleSession,
      refreshToken: "must-never-appear-in-json",
    });
  });
});

test("Access token store keeps tokens only in memory", () => {
  const store = createAccessTokenStore();
  assert.equal(store.getAccessToken(), null);
  store.setAccessToken("abc.def.ghi");
  assert.equal(store.getAccessToken(), "abc.def.ghi");
  store.clearAccessToken();
  assert.equal(store.getAccessToken(), null);
});

test("Refresh interceptor single-flights concurrent 401s and retries once with fresh token", async () => {
  const store = createAccessTokenStore();
  store.setAccessToken("expired-token");

  let refreshCalls = 0;
  let protectedCalls = 0;

  const { http, refreshSingleFlight } = createApiClient({
    baseURL: "https://api.example.test/api/v1",
    tokenStore: store,
    adapter: async (config) => {
      if (config.url === "/auth/refresh") {
        refreshCalls += 1;
        await new Promise((r) => setTimeout(r, 25));
        return {
          data: sampleSession,
          status: 200,
          statusText: "OK",
          headers: {},
          config,
        };
      }

      protectedCalls += 1;
      const authHeader = AxiosHeaders.from(config.headers).getAuthorization();
      if (authHeader === "Bearer expired-token") {
        throw new AxiosError(
          "Unauthorized",
          "ERR_BAD_REQUEST",
          config,
          null,
          {
            data: { status: 401, title: "Unauthorized" },
            status: 401,
            statusText: "Unauthorized",
            headers: {},
            config,
          }
        );
      }

      return {
        data: { ok: true, usedHeader: authHeader },
        status: 200,
        statusText: "OK",
        headers: {},
        config,
      };
    },
  });

  const [r1, r2, r3] = await Promise.all([
    http.get("/protected-a"),
    http.get("/protected-b"),
    http.get("/protected-c"),
  ]);

  assert.equal(refreshCalls, 1, "Concurrent 401s must coalesce into exactly 1 refresh call");
  assert.equal(protectedCalls, 6, "3 initial 401 calls + 3 retried calls");
  assert.equal(r1.data.usedHeader, "Bearer jwt-access-token-new");
  assert.equal(r2.data.usedHeader, "Bearer jwt-access-token-new");
  assert.equal(r3.data.usedHeader, "Bearer jwt-access-token-new");
  assert.equal(store.getAccessToken(), "jwt-access-token-new");

  // Also ensure authService.logout clears token cleanly
  const authService = createAuthService(http, store, refreshSingleFlight);
  await authService.logout();
  assert.equal(store.getAccessToken(), null);
});

test("Terminal refresh failure clears in-memory token, calls onAuthFailure, and never loops", async () => {
  const store = createAccessTokenStore();
  store.setAccessToken("stale-token");

  let refreshCalls = 0;
  let failureCallbacks = 0;

  const { http } = createApiClient({
    baseURL: "https://api.example.test/api/v1",
    tokenStore: store,
    onAuthFailure: () => {
      failureCallbacks += 1;
    },
    adapter: async (config) => {
      if (config.url === "/auth/refresh") {
        refreshCalls += 1;
      }
      throw new AxiosError(
        "Unauthorized",
        "ERR_BAD_REQUEST",
        config,
        null,
        {
          data: { status: 401, title: "Session invalid" },
          status: 401,
          statusText: "Unauthorized",
          headers: {},
          config,
        }
      );
    },
  });

  await assert.rejects(() => http.get("/protected-resource"));
  assert.equal(refreshCalls, 1, "Must not loop when /auth/refresh returns 401");
  assert.equal(failureCallbacks, 1);
  assert.equal(store.getAccessToken(), null);
});
