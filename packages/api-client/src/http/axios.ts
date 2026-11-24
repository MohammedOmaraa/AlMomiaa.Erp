import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from "axios";
import type { AccessTokenStore } from "../auth/access-token-store.js";
import { authenticationResponseSchema } from "../auth/auth.schemas.js";
import type { AuthenticationResponse } from "../auth/auth.types.js";
import { createIdempotencyKey, redactSensitiveData, type ApiCallLog } from "../common/types.js";

export interface ApiClientConfig {
  baseURL: string;
  tokenStore: AccessTokenStore;
  getLocale?: () => "ar-SA" | "en-US";
  onSessionRefreshed?: (session: AuthenticationResponse) => void;
  onAuthFailure?: () => void;
  onApiCall?: (log: ApiCallLog) => void;
  timeoutMs?: number;
  adapter?: AxiosRequestConfig["adapter"];
}

type RetryableRequestConfig = InternalAxiosRequestConfig & {
  _authRetried?: boolean;
  _startedAtMs?: number;
};

const AUTH_PATHS = ["/auth/login", "/auth/refresh", "/auth/logout"];

function isAuthPath(url: string | undefined): boolean {
  if (!url) return false;
  return AUTH_PATHS.some((path) => url.includes(path));
}

function parseMaybeJson(data: unknown): unknown {
  if (typeof data === "string") {
    try {
      return JSON.parse(data) as unknown;
    } catch {
      return data;
    }
  }
  return data;
}

export function createApiClient(config: ApiClientConfig): {
  http: AxiosInstance;
  refreshSingleFlight: (signal?: AbortSignal) => Promise<AuthenticationResponse>;
} {
  const http = axios.create({
    baseURL: config.baseURL,
    withCredentials: true,
    timeout: config.timeoutMs ?? 15000,
    adapter: config.adapter,
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
  });

  let inFlightRefresh: Promise<AuthenticationResponse> | null = null;

  const refreshSingleFlight = (signal?: AbortSignal): Promise<AuthenticationResponse> => {
    if (!inFlightRefresh) {
      inFlightRefresh = http
        .post<unknown>("/auth/refresh", undefined, {
          withCredentials: true,
          signal,
        })
        .then((response) => {
          const parsed = authenticationResponseSchema.parse(response.data);
          config.tokenStore.setAccessToken(parsed.accessToken);
          config.onSessionRefreshed?.(parsed);
          return parsed;
        })
        .catch((error: unknown) => {
          config.tokenStore.clearAccessToken();
          config.onAuthFailure?.();
          throw error;
        })
        .finally(() => {
          inFlightRefresh = null;
        });
    }

    return inFlightRefresh;
  };

  http.interceptors.request.use((requestConfig: InternalAxiosRequestConfig) => {
    const timedConfig = requestConfig as RetryableRequestConfig;
    timedConfig._startedAtMs = Date.now();

    const headers = AxiosHeaders.from(requestConfig.headers);
    const locale = config.getLocale?.() ?? "ar-SA";
    headers.set("Accept-Language", locale);

    if (!isAuthPath(requestConfig.url)) {
      const token = config.tokenStore.getAccessToken();
      if (token) {
        headers.set("Authorization", `Bearer ${token}`);
      }
    }

    requestConfig.headers = headers;
    return requestConfig;
  });

  http.interceptors.response.use(
    (response) => {
      if (config.onApiCall) {
        const req = response.config as RetryableRequestConfig;
        config.onApiCall({
          id: createIdempotencyKey(),
          method: (req.method ?? "GET").toUpperCase(),
          url: req.url ?? "",
          status: response.status,
          durationMs: req._startedAtMs ? Math.max(1, Date.now() - req._startedAtMs) : 0,
          timestamp: new Date().toISOString(),
          requestBody: req.data !== undefined ? redactSensitiveData(parseMaybeJson(req.data)) : undefined,
          responseBody: redactSensitiveData(response.data),
        });
      }
      return response;
    },
    async (error: unknown) => {
      if (!(error instanceof AxiosError)) {
        throw error;
      }

      const originalRequest = error.config as RetryableRequestConfig | undefined;
      const status = error.response?.status;

      if (config.onApiCall && originalRequest) {
        config.onApiCall({
          id: createIdempotencyKey(),
          method: (originalRequest.method ?? "GET").toUpperCase(),
          url: originalRequest.url ?? "",
          status,
          durationMs: originalRequest._startedAtMs ? Math.max(1, Date.now() - originalRequest._startedAtMs) : 0,
          timestamp: new Date().toISOString(),
          requestBody:
            originalRequest.data !== undefined
              ? redactSensitiveData(parseMaybeJson(originalRequest.data))
              : undefined,
          responseBody: redactSensitiveData(error.response?.data),
          errorMessage: error.message,
        });
      }

      if (
        status !== 401 ||
        !originalRequest ||
        originalRequest._authRetried ||
        isAuthPath(originalRequest.url)
      ) {
        throw error;
      }

      originalRequest._authRetried = true;

      const refreshed = await refreshSingleFlight(originalRequest.signal as AbortSignal | undefined);
      const retryHeaders = AxiosHeaders.from(originalRequest.headers);
      retryHeaders.set("Authorization", `Bearer ${refreshed.accessToken}`);
      originalRequest.headers = retryHeaders;

      return http.request(originalRequest);
    }
  );

  return { http, refreshSingleFlight };
}
