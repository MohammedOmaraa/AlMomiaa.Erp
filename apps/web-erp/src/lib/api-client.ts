import {
  createAccessTokenStore,
  createApiClient,
  createAuthService,
  type AuthenticationResponse,
} from "@almomiaa/api-client";
import type { Locale } from "@almomiaa/ui";

const DEFAULT_API_BASE_URL = "/api/v1";

let currentLocale: Locale = "ar-SA";
let onSessionRefreshedListener: ((session: AuthenticationResponse) => void) | null = null;
let onAuthFailureListener: (() => void) | null = null;

export const accessTokenStore = createAccessTokenStore();

const apiBaseUrl =
  (process.env.NEXT_PUBLIC_API_BASE_URL && process.env.NEXT_PUBLIC_API_BASE_URL.trim().length > 0
    ? process.env.NEXT_PUBLIC_API_BASE_URL.trim()
    : DEFAULT_API_BASE_URL);

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
