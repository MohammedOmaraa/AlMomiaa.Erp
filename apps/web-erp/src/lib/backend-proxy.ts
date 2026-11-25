import { NextResponse, type NextRequest } from "next/server";

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
  const raw = (process.env.BACKEND_API_ORIGIN ?? "http://localhost:5000").trim();
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

export async function proxyToBackend(request: NextRequest, targetPathname: string): Promise<NextResponse> {
  const backendOrigin = resolveBackendOrigin();
  if (!backendOrigin) {
    return NextResponse.json(
      {
        title: "Backend proxy not configured",
        status: 500,
        detail: "BACKEND_API_ORIGIN environment variable must be a valid http(s) URL.",
      },
      { status: 500 },
    );
  }

  const isAllowedPath =
    targetPathname === "/health" ||
    targetPathname === "/api/v1" ||
    targetPathname.startsWith("/api/v1/");

  if (!isAllowedPath) {
    return NextResponse.json(
      {
        title: "Route not found",
        status: 404,
        detail: "Only /api/v1/* and /health routes are proxied.",
      },
      { status: 404 },
    );
  }

  const upstreamUrl = new URL(`${targetPathname}${request.nextUrl.search}`, backendOrigin);
  const upstreamHeaders = new Headers();

  for (const headerName of FORWARDED_REQUEST_HEADERS) {
    const value = request.headers.get(headerName);
    if (value) {
      upstreamHeaders.set(headerName, value);
    }
  }

  const method = request.method.toUpperCase();
  const hasBody = method !== "GET" && method !== "HEAD";

  try {
    const body = hasBody ? await request.arrayBuffer() : undefined;
    const upstreamResponse = await fetch(upstreamUrl, {
      method,
      headers: upstreamHeaders,
      body: body && body.byteLength > 0 ? body : undefined,
      redirect: "manual",
      cache: "no-store",
    });

    const responseHeaders = new Headers();
    const contentType = upstreamResponse.headers.get("content-type");
    if (contentType) {
      responseHeaders.set("content-type", contentType);
    }

    const cacheControl = upstreamResponse.headers.get("cache-control");
    responseHeaders.set("cache-control", cacheControl ?? "no-store");

    const location = upstreamResponse.headers.get("location");
    if (location) {
      responseHeaders.set("location", location);
    }

    // Explicitly preserve all HttpOnly; Secure; SameSite=Strict Set-Cookie headers
    const headersWithCookies = upstreamResponse.headers as Headers & { getSetCookie?: () => string[] };
    const setCookies = headersWithCookies.getSetCookie?.() ?? [];
    if (setCookies.length > 0) {
      for (const cookie of setCookies) {
        responseHeaders.append("set-cookie", cookie);
      }
    } else {
      const singleCookie = upstreamResponse.headers.get("set-cookie");
      if (singleCookie) {
        responseHeaders.append("set-cookie", singleCookie);
      }
    }

    if (upstreamResponse.status === 204 || upstreamResponse.status === 304) {
      return new NextResponse(null, {
        status: upstreamResponse.status,
        headers: responseHeaders,
      });
    }

    const responseBody = await upstreamResponse.arrayBuffer();
    return new NextResponse(responseBody, {
      status: upstreamResponse.status,
      headers: responseHeaders,
    });
  } catch {
    return NextResponse.json(
      {
        title: "Upstream backend unreachable",
        status: 502,
        detail: "The backend API is starting up or temporarily unreachable. Please retry in a few seconds.",
      },
      { status: 502 },
    );
  }
}
