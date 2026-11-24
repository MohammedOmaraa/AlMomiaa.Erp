import type { IncomingMessage, ServerResponse } from "node:http";

const FORWARDED_REQUEST_HEADERS = [
  "accept",
  "accept-language",
  "authorization",
  "content-type",
  "cookie",
  "origin",
  "referer",
  "sec-fetch-site",
  "x-idempotency-key",
] as const;

function resolveBackendOrigin(): URL | null {
  const raw = process.env.BACKEND_API_ORIGIN?.trim();
  if (!raw) return null;
  try {
    const parsed = new URL(raw);
    if (parsed.protocol !== "https:" && parsed.protocol !== "http:") {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

async function readRequestBody(req: IncomingMessage): Promise<Buffer | undefined> {
  const method = (req.method ?? "GET").toUpperCase();
  if (method === "GET" || method === "HEAD") {
    return undefined;
  }

  const chunks: Buffer[] = [];
  for await (const chunk of req) {
    chunks.push(typeof chunk === "string" ? Buffer.from(chunk) : Buffer.from(chunk));
  }
  return chunks.length > 0 ? Buffer.concat(chunks) : undefined;
}

export default async function handler(req: IncomingMessage, res: ServerResponse): Promise<void> {
  const backendOrigin = resolveBackendOrigin();
  if (!backendOrigin) {
    res.statusCode = 500;
    res.setHeader("Content-Type", "application/problem+json");
    res.end(
      JSON.stringify({
        title: "Backend proxy not configured",
        status: 500,
        detail: "BACKEND_API_ORIGIN environment variable must be set to a valid http(s) URL on Vercel.",
      }),
    );
    return;
  }

  const incomingUrl = new URL(req.url ?? "/", "https://rep-pwa.local");
  const targetPathname = incomingUrl.pathname === "/api/health" ? "/health" : incomingUrl.pathname;

  // Strict SSRF allowlist: only forward canonical AlMomiaa API routes and /health
  const isAllowedPath =
    targetPathname === "/health" ||
    targetPathname === "/api/v1" ||
    targetPathname.startsWith("/api/v1/");

  if (!isAllowedPath) {
    res.statusCode = 404;
    res.setHeader("Content-Type", "application/problem+json");
    res.end(
      JSON.stringify({
        title: "Route not found",
        status: 404,
        detail: "Only /api/v1/* and /health routes are proxied.",
      }),
    );
    return;
  }

  const upstreamUrl = new URL(`${targetPathname}${incomingUrl.search}`, backendOrigin);
  const upstreamHeaders = new Headers();

  for (const headerName of FORWARDED_REQUEST_HEADERS) {
    const value = req.headers[headerName];
    if (typeof value === "string" && value.length > 0) {
      upstreamHeaders.set(headerName, value);
    } else if (Array.isArray(value) && value.length > 0) {
      upstreamHeaders.set(headerName, value.join(", "));
    }
  }

  try {
    const body = await readRequestBody(req);
    const upstreamResponse = await fetch(upstreamUrl, {
      method: req.method ?? "GET",
      headers: upstreamHeaders,
      body: body ? new Uint8Array(body) : undefined,
      redirect: "manual",
    });

    res.statusCode = upstreamResponse.status;

    const contentType = upstreamResponse.headers.get("content-type");
    if (contentType) {
      res.setHeader("Content-Type", contentType);
    }

    const cacheControl = upstreamResponse.headers.get("cache-control");
    if (cacheControl) {
      res.setHeader("Cache-Control", cacheControl);
    }

    const location = upstreamResponse.headers.get("location");
    if (location) {
      res.setHeader("Location", location);
    }

    // Preserve HttpOnly; Secure; SameSite=Strict refresh cookies on the PWA's own HTTPS origin
    const headersWithCookies = upstreamResponse.headers as Headers & { getSetCookie?: () => string[] };
    const setCookies = headersWithCookies.getSetCookie?.() ?? [];
    if (setCookies.length > 0) {
      res.setHeader("Set-Cookie", setCookies);
    } else {
      const singleCookie = upstreamResponse.headers.get("set-cookie");
      if (singleCookie) {
        res.setHeader("Set-Cookie", singleCookie);
      }
    }

    const responseBuffer = Buffer.from(await upstreamResponse.arrayBuffer());
    res.end(responseBuffer);
  } catch {
    res.statusCode = 502;
    res.setHeader("Content-Type", "application/problem+json");
    res.end(
      JSON.stringify({
        title: "Upstream backend unreachable",
        status: 502,
        detail: "The backend API is starting up or temporarily unreachable. Retry in a few seconds.",
      }),
    );
  }
}
