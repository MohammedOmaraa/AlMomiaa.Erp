import {
  createAccessTokenStore,
  createApiClient,
  createAuthService,
  createErpApi,
  extractProblemDetails,
} from "@almomiaa/api-client";
import type { Locale } from "@almomiaa/ui";

const rawBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim();
export const apiBaseUrl = rawBaseUrl ? rawBaseUrl.replace(/\/$/, "") : "/api/v1";

const rawHubUrl = (import.meta.env.VITE_SIGNALR_HUB_URL as string | undefined)?.trim();
export const signalRHubUrl = rawHubUrl || "/hubs/operations";

export const accessTokenStore = createAccessTokenStore();

const authFailureListeners = new Set<() => void>();

export function subscribeAuthFailure(listener: () => void): () => void {
  authFailureListeners.add(listener);
  return () => {
    authFailureListeners.delete(listener);
  };
}

let currentLocale: Locale = "ar-SA";
export function setApiLocale(locale: Locale): void {
  currentLocale = locale;
}

const { http, refreshSingleFlight } = createApiClient({
  baseURL: apiBaseUrl,
  tokenStore: accessTokenStore,
  getLocale: () => currentLocale,
  onAuthFailure: () => {
    for (const listener of authFailureListeners) {
      listener();
    }
  },
});

export const repHttp = http;
export const authService = createAuthService(http, accessTokenStore, refreshSingleFlight);
export const erpApi = createErpApi(http);

export function formatApiError(error: unknown, fallback: string): string {
  const problem = extractProblemDetails(error);
  if (problem?.errors) {
    const details = Object.entries(problem.errors)
      .flatMap(([field, msgs]) => msgs.map((m) => `${field}: ${m}`))
      .join(" | ");
    if (details) return details;
  }
  return problem?.detail ?? problem?.title ?? fallback;
}
