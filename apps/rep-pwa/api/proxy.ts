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

export function resolveSafeTargetPath(rawUrl: string): { pathname: string; search: string } | null {
  const incomingUrl = new URL(rawUrl, "https://rep-pwa.local");
  const rewrittenPath = incomingUrl.searchParams.get("__path");
  const candidatePath = (rewrittenPath ?? incomingUrl.pathname).trim();

  // Reject path traversal, scheme injection, backslashes, or double slashes
  if (
    !candidatePath.startsWith("/") ||
    candidatePath.startsWith("//") ||
    candidatePath.includes("..") ||
    candidatePath.includes("\\") ||
    candidatePath.includes("://")
  ) {
    return null;
  }

  const normalizedPath = candidatePath === "/api/health" ? "/health" : candidatePath;
  const isAllowed =
    normalizedPath === "/health" ||
    normalizedPath === "/api/v1" ||
    normalizedPath.startsWith("/api/v1/");

  if (!isAllowed) {
    return null;
  }

  const forwardedParams = new URLSearchParams(incomingUrl.searchParams);
  forwardedParams.delete("__path");
  const qs = forwardedParams.toString();

  return {
    pathname: normalizedPath,
    search: qs ? `?${qs}` : "",
  };
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

  const resolvedTarget = resolveSafeTargetPath(req.url ?? "/");
  if (!resolvedTarget) {
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

  const upstreamUrl = new URL(`${resolvedTarget.pathname}${resolvedTarget.search}`, backendOrigin);
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
      signal: AbortSignal.timeout(25_000),
    });

    res.statusCode = upstreamResponse.status;

    const contentType = upstreamResponse.headers.get("content-type");
    if (contentType) {
      res.setHeader("Content-Type", contentType);
    }

    const cacheControl = upstreamResponse.headers.get("cache-control");
    res.setHeader("Cache-Control", cacheControl ?? "no-store");

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

    if (upstreamResponse.status === 204 || upstreamResponse.status === 304) {
      res.end();
      return;
    }

    const responseBuffer = Buffer.from(await upstreamResponse.arrayBuffer());
    res.end(responseBuffer);
  } catch (error: unknown) {
    const isTimeout =
      error instanceof Error && (error.name === "TimeoutError" || error.name === "AbortError");
    const status = isTimeout ? 504 : 502;
    res.statusCode = status;
    res.setHeader("Content-Type", "application/problem+json");
    res.end(
      JSON.stringify({
        title: isTimeout ? "Upstream backend timeout" : "Upstream backend unreachable",
        status,
        detail: isTimeout
          ? "The backend API is waking up and took too long to respond. Retry in a few seconds."
          : "The backend API is starting up or temporarily unreachable. Retry in a few seconds.",
      }),
    );
  }
}
