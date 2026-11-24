import {
  createAccessTokenStore,
  createApiClient,
  createAuthService,
  createErpApi,
  extractProblemDetails,
  type AuthenticationResponse,
} from "@almomiaa/api-client";
import type { Locale } from "@almomiaa/ui";

const DEFAULT_API_BASE_URL = "/api/v1";

let currentLocale: Locale = "ar-SA";
let onSessionRefreshedListener: ((session: AuthenticationResponse) => void) | null = null;
let onAuthFailureListener: (() => void) | null = null;

export const accessTokenStore = createAccessTokenStore();

function normalizeBaseUrl(raw: string | undefined): string {
  const trimmed = raw?.trim();
  if (!trimmed) return DEFAULT_API_BASE_URL;
  if (trimmed.endsWith("/api/v1")) return trimmed;
  if (trimmed.startsWith("http://") || trimmed.startsWith("https://")) {
    return `${trimmed.replace(/\/$/, "")}/api/v1`;
  }
  return trimmed;
}

const apiBaseUrl = normalizeBaseUrl(process.env.NEXT_PUBLIC_API_BASE_URL);

const { http, refreshSingleFlight } = createApiClient({
  baseURL: apiBaseUrl,
  tokenStore: accessTokenStore,
  getLocale: () => currentLocale,
  onSessionRefreshed: (session) => {
    onSessionRefreshedListener?.(session);
  },
  onAuthFailure: () => {
    onAuthFailureListener?.();
  },
});

export const apiHttpClient = http;
export const authService = createAuthService(http, accessTokenStore, refreshSingleFlight);
export const erpApi = createErpApi(http);

export function setApiClientLocale(locale: Locale): void {
  currentLocale = locale;
}

export function subscribeAuthClientEvents(handlers: {
  onSessionRefreshed: (session: AuthenticationResponse) => void;
  onAuthFailure: () => void;
}): () => void {
  onSessionRefreshedListener = handlers.onSessionRefreshed;
  onAuthFailureListener = handlers.onAuthFailure;

  return () => {
    if (onSessionRefreshedListener === handlers.onSessionRefreshed) {
      onSessionRefreshedListener = null;
    }
    if (onAuthFailureListener === handlers.onAuthFailure) {
      onAuthFailureListener = null;
    }
  };
}

export function formatApiError(error: unknown): { title: string; message: string; details: string[] } {
  const problem = extractProblemDetails(error);
  if (problem) {
    const details: string[] = [];
    if (problem.errors) {
      for (const [field, messages] of Object.entries(problem.errors)) {
        for (const msg of messages) {
          details.push(`${field}: ${msg}`);
        }
      }
    }
    return {
      title: problem.title ?? `HTTP ${problem.status ?? "Error"}`,
      message: problem.detail ?? problem.title ?? "Request failed.",
      details,
    };
  }
  if (error instanceof Error) {
    return { title: "Error", message: error.message, details: [] };
  }
  return { title: "Error", message: "An unexpected error occurred.", details: [] };
}
