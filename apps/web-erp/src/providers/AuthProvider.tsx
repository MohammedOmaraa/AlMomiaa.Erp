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
import type {
  AuthenticatedUser,
  AuthenticationResponse,
  ChangePasswordRequest,
  LoginRequest,
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
  login: (request: LoginRequest) => Promise<AuthenticationResponse>;
  logout: () => Promise<void>;
  changePassword: (request: ChangePasswordRequest) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: Readonly<{ children: ReactNode }>) {
  const router = useRouter();
  const { locale } = useLocale();
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const bootstrapStartedRef = useRef(false);

  useEffect(() => {
    setApiClientLocale(locale);
  }, [locale]);

  useEffect(() => {
    return subscribeAuthClientEvents({
      onSessionRefreshed: (session) => {
        setUser(session.user);
        setStatus("authenticated");
      },
      onAuthFailure: () => {
        setUser(null);
        setStatus("unauthenticated");
      },
    });
  }, []);

  useEffect(() => {
    if (bootstrapStartedRef.current) {
      return;
    }
    bootstrapStartedRef.current = true;

    const controller = new AbortController();
    authService
      .refresh(controller.signal)
      .then((session) => {
        if (!controller.signal.aborted) {
          setUser(session.user);
          setStatus("authenticated");
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          accessTokenStore.clearAccessToken();
          setUser(null);
          setStatus("unauthenticated");
        }
      });

    return () => {
      controller.abort();
    };
  }, []);

  const login = useCallback(async (request: LoginRequest): Promise<AuthenticationResponse> => {
    const session = await authService.login(request);
    setUser(session.user);
    setStatus("authenticated");
    return session;
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    try {
      await authService.logout();
    } finally {
      accessTokenStore.clearAccessToken();
      setUser(null);
      setStatus("unauthenticated");
      router.replace("/login");
    }
  }, [router]);

  const changePassword = useCallback(
    async (request: ChangePasswordRequest): Promise<void> => {
      await authService.changePassword(request);
      accessTokenStore.clearAccessToken();
      setUser(null);
      setStatus("unauthenticated");
      router.replace("/login?passwordChanged=1");
    },
    [router],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, login, logout, changePassword }),
    [status, user, login, logout, changePassword]
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
