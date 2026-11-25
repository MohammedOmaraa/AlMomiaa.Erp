#!/usr/bin/env node
/**
 * AlMomiaa ERP — End-to-End Live Cloud Deployment Verifier (Zero Hardcoded Secrets)
 *
 * Usage:
 *   node scripts/verify-deployed-apps.mjs
 *
 * Optional environment overrides:
 *   BACKEND_URL=https://almomiaa-erp.onrender.com
 *   WEB_ERP_URL=https://almomiaa-erp.vercel.app
 *   REP_PWA_URL=https://almomiaa-rep.vercel.app
 *   DEMO_IDENTIFIER=ADMIN-DEMO
 *   DEMO_PASSWORD=<your-demo-password>
 */

const BACKEND_URL = (process.env.BACKEND_URL || "https://almomiaa-erp.onrender.com").replace(/\/$/, "");
const WEB_ERP_URL = (process.env.WEB_ERP_URL || "https://almomiaa-erp.vercel.app").replace(/\/$/, "");
const REP_PWA_URL = (process.env.REP_PWA_URL || "https://almomiaa-rep.vercel.app").replace(/\/$/, "");
const DEMO_IDENTIFIER = (process.env.DEMO_IDENTIFIER || "").trim();
const DEMO_PASSWORD = (process.env.DEMO_PASSWORD || "").trim();

const REQUEST_TIMEOUT_MS = 45_000;

const results = [];

function record(name, ok, detail) {
  results.push({ name, ok, detail });
  const icon = ok ? "PASS" : "FAIL";
  console.log(`[${icon}] ${name} — ${detail}`);
}

async function fetchWithTimeout(url, options = {}) {
  return fetch(url, {
    ...options,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });
}

async function checkEndpoint(name, url, expectedCheck) {
  const started = Date.now();
  try {
    const res = await fetchWithTimeout(url, { redirect: "follow" });
    const ms = Date.now() - started;
    const text = await res.text();
    const verdict = expectedCheck ? expectedCheck(res, text) : { ok: res.ok, note: `HTTP ${res.status}` };
    record(name, verdict.ok, `${verdict.note} (${ms}ms)`);
  } catch (err) {
    const ms = Date.now() - started;
    record(name, false, `${err instanceof Error ? err.message : String(err)} (${ms}ms)`);
  }
}

async function checkAuthRoundTrip(label, appBaseUrl) {
  const loginUrl = `${appBaseUrl}/api/v1/auth/login`;
  const refreshUrl = `${appBaseUrl}/api/v1/auth/refresh`;
  const usersUrl = `${appBaseUrl}/api/v1/users?page=1&pageSize=5`;

  try {
    const loginRes = await fetchWithTimeout(loginUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        Origin: appBaseUrl,
        Referer: `${appBaseUrl}/login`,
      },
      body: JSON.stringify({ identifier: DEMO_IDENTIFIER, password: DEMO_PASSWORD }),
    });

    const loginJson = await loginRes.json().catch(() => null);
    if (!loginRes.ok || !loginJson?.accessToken) {
      record(`${label} Login`, false, `HTTP ${loginRes.status} (${loginJson?.detail || loginJson?.title || "no accessToken"})`);
      return;
    }

    const setCookies = typeof loginRes.headers.getSetCookie === "function"
      ? loginRes.headers.getSetCookie()
      : [loginRes.headers.get("set-cookie")].filter(Boolean);
    const refreshCookie = setCookies.find((c) => c.startsWith("almomiaa_refresh="));

    record(
      `${label} Login + Set-Cookie`,
      Boolean(refreshCookie),
      refreshCookie
        ? `HTTP 200, user=${loginJson.user?.employeeCode}, refresh cookie present`
        : `HTTP 200, user=${loginJson.user?.employeeCode}, MISSING almomiaa_refresh cookie`,
    );

    if (refreshCookie) {
      const cookiePair = refreshCookie.split(";")[0];
      const refreshRes = await fetchWithTimeout(refreshUrl, {
        method: "POST",
        headers: {
          Accept: "application/json",
          Cookie: cookiePair,
          Origin: appBaseUrl,
          Referer: `${appBaseUrl}/`,
        },
      });
      const refreshJson = await refreshRes.json().catch(() => null);
      record(
        `${label} Same-Origin Cookie Refresh`,
        refreshRes.ok && Boolean(refreshJson?.accessToken),
        refreshRes.ok
          ? `HTTP ${refreshRes.status}, rotated JWT issued`
          : `HTTP ${refreshRes.status} (${refreshJson?.detail || refreshJson?.title || "refresh failed"})`,
      );

      const token = refreshJson?.accessToken || loginJson.accessToken;
      const apiRes = await fetchWithTimeout(usersUrl, {
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },
      });
      record(
        `${label} Authenticated API (/users)`,
        apiRes.status === 200 || apiRes.status === 403,
        `HTTP ${apiRes.status}`,
      );
    }
  } catch (err) {
    record(`${label} Auth Round-Trip`, false, err instanceof Error ? err.message : String(err));
  }
}

async function main() {
  console.log("=== AlMomiaa ERP Live Deployment Verification ===");
  console.log(`Backend : ${BACKEND_URL}`);
  console.log(`Web ERP : ${WEB_ERP_URL}`);
  console.log(`Rep PWA : ${REP_PWA_URL}\n`);

  // 1. Backend Health
  await checkEndpoint("Backend /health", `${BACKEND_URL}/health`, (res, text) => ({
    ok: res.ok && text.includes("Healthy"),
    note: `HTTP ${res.status} ${text.slice(0, 80)}`,
  }));

  // 2. Web ERP Routes & Proxy
  for (const path of ["/", "/login", "/users", "/customers", "/catalog"]) {
    await checkEndpoint(`Web ERP ${path}`, `${WEB_ERP_URL}${path}`, (res, text) => ({
      ok: res.status === 200 && text.includes("<html"),
      note: `HTTP ${res.status}`,
    }));
  }

  await checkEndpoint("Web ERP /manifest.webmanifest", `${WEB_ERP_URL}/manifest.webmanifest`, (res, text) => ({
    ok: res.status === 200 && text.includes("AlMomiaa ERP"),
    note: `HTTP ${res.status}`,
  }));

  await checkEndpoint("Web ERP Proxy /health", `${WEB_ERP_URL}/health`, (res, text) => ({
    ok: res.status === 200 && text.includes("Healthy"),
    note: `HTTP ${res.status} ${text.slice(0, 80)}`,
  }));

  // 3. Rep PWA Routes, Manifest, Service Worker & Proxy
  for (const path of ["/", "/login"]) {
    await checkEndpoint(`Rep PWA ${path}`, `${REP_PWA_URL}${path}`, (res, text) => ({
      ok: res.status === 200 && text.toLowerCase().includes("<!doctype html"),
      note: `HTTP ${res.status}`,
    }));
  }

  await checkEndpoint("Rep PWA /manifest.webmanifest", `${REP_PWA_URL}/manifest.webmanifest`, (res, text) => ({
    ok: res.status === 200 && text.includes("AlMomiaa Rep"),
    note: `HTTP ${res.status}`,
  }));

  await checkEndpoint("Rep PWA /sw.js", `${REP_PWA_URL}/sw.js`, (res, text) => ({
    ok: res.status === 200 && text.length > 50,
    note: `HTTP ${res.status} (${text.length} bytes)`,
  }));

  await checkEndpoint("Rep PWA Proxy /health", `${REP_PWA_URL}/health`, (res, text) => ({
    ok: res.status === 200 && text.includes("Healthy"),
    note: `HTTP ${res.status} ${text.slice(0, 80)}`,
  }));

  // 4. Optional Authenticated Cookie + Refresh Round-Trip (only if env vars supplied)
  if (DEMO_IDENTIFIER && DEMO_PASSWORD) {
    console.log("\nRunning authenticated cookie + refresh checks via supplied DEMO_IDENTIFIER...");
    await checkAuthRoundTrip("Web ERP", WEB_ERP_URL);
    await checkAuthRoundTrip("Rep PWA", REP_PWA_URL);
  } else {
    console.log("\nTip: Set DEMO_IDENTIFIER and DEMO_PASSWORD env vars to also run full login + refresh-cookie round-trip checks.");
  }

  const passed = results.filter((r) => r.ok).length;
  const failed = results.length - passed;
  console.log(`\nSummary: ${passed}/${results.length} passed, ${failed} failed.`);
  process.exit(failed > 0 ? 1 : 0);
}

main();
