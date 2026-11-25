"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import {
  extractProblemDetails,
  type AuthenticatedUser,
  type AuthenticationResponse,
  type ChangePasswordRequest,
  type LoginRequest,
} from "@almomiaa/api-client";
import {
  accessTokenStore,
  authService,
  setApiClientLocale,
  subscribeAuthClientEvents,
} from "../lib/api-client";
import { useLocale } from "./LocaleProvider";

export type AuthStatus = "loading" | "authenticated" | "unauthenticated";

export interface AuthContextValue {
  status: AuthStatus;
  user: AuthenticatedUser | null;
  slowBootstrap: boolean;
  bootstrapError: string | null;
  retrySession: () => Promise<void>;
  login: (request: LoginRequest) => Promise<AuthenticationResponse>;
  logout: () => Promise<void>;
  changePassword: (request: ChangePasswordRequest) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const SLOW_BOOTSTRAP_MS = 5_000;
const BOOTSTRAP_TIMEOUT_MS = 18_000;

export function AuthProvider({ children }: Readonly<{ children: ReactNode }>) {
  const router = useRouter();
  const { locale } = useLocale();
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [slowBootstrap, setSlowBootstrap] = useState(false);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
  const mountedRef = useRef(true);
  const localeRef = useRef(locale);
  localeRef.current = locale;

  useEffect(() => {
    setApiClientLocale(locale);
  }, [locale]);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    return subscribeAuthClientEvents({
      onSessionRefreshed: (session) => {
        if (!mountedRef.current) return;
        setUser(session.user);
        setBootstrapError(null);
        setSlowBootstrap(false);
        setStatus("authenticated");
      },
      onAuthFailure: () => {
        if (!mountedRef.current) return;
        setUser(null);
        setSlowBootstrap(false);
        setStatus("unauthenticated");
      },
    });
  }, []);

  const retrySession = useCallback(async (): Promise<void> => {
    if (!mountedRef.current) return;
    setStatus("loading");
    setSlowBootstrap(false);
    setBootstrapError(null);

    const slowTimer = window.setTimeout(() => {
      if (mountedRef.current) {
        setSlowBootstrap(true);
      }
    }, SLOW_BOOTSTRAP_MS);

    const controller = new AbortController();
    const timeoutTimer = window.setTimeout(() => {
      controller.abort();
    }, BOOTSTRAP_TIMEOUT_MS);

    try {
      const session = await authService.refresh(controller.signal);
      if (!mountedRef.current) return;
      setUser(session.user);
      setBootstrapError(null);
      setSlowBootstrap(false);
      setStatus("authenticated");
    } catch (error: unknown) {
      if (!mountedRef.current) return;
      accessTokenStore.clearAccessToken();
      setUser(null);
      setSlowBootstrap(false);

      const problem = extractProblemDetails(error);
      const isAr = localeRef.current === "ar-SA";
      if (!problem || (problem.status && problem.status >= 500)) {
        setBootstrapError(
          isAr
            ? "تعذّر الوصول للخادم حالياً (قد يكون في وضع الاستيقاظ). يمكنك المحاولة مجدداً أو تسجيل الدخول."
            : "Unable to reach the server right now (it may be waking up). You can retry or go to Sign In.",
        );
      } else {
        setBootstrapError(null);
      }
      setStatus("unauthenticated");
    } finally {
      window.clearTimeout(slowTimer);
      window.clearTimeout(timeoutTimer);
    }
  }, []);

  useEffect(() => {
    void retrySession();
  }, [retrySession]);

  const login = useCallback(async (request: LoginRequest): Promise<AuthenticationResponse> => {
    const session = await authService.login(request);
    setUser(session.user);
    setBootstrapError(null);
    setSlowBootstrap(false);
    setStatus("authenticated");
    return session;
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    try {
      await authService.logout();
    } finally {
      accessTokenStore.clearAccessToken();
      setUser(null);
      setBootstrapError(null);
      setSlowBootstrap(false);
      setStatus("unauthenticated");
      router.replace("/login");
    }
  }, [router]);

  const changePassword = useCallback(
    async (request: ChangePasswordRequest): Promise<void> => {
      await authService.changePassword(request);
      accessTokenStore.clearAccessToken();
      setUser(null);
      setBootstrapError(null);
      setSlowBootstrap(false);
      setStatus("unauthenticated");
      router.replace("/login?passwordChanged=1");
    },
    [router],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, slowBootstrap, bootstrapError, retrySession, login, logout, changePassword }),
    [status, user, slowBootstrap, bootstrapError, retrySession, login, logout, changePassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
}
