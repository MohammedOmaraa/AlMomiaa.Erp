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
import {
  createOperationsHubClient,
  type AuthenticatedUser,
  type LoginRequest,
  type OperationsHubEvent,
  type OperationsHubState,
} from "@almomiaa/api-client";
import type { Locale, Theme } from "@almomiaa/ui";
import {
  accessTokenStore,
  authService,
  setApiLocale,
  signalRHubUrl,
  subscribeAuthFailure,
} from "../lib/api-client";
import { arMessages, type RepMessages } from "../locales/ar";
import { enMessages } from "../locales/en";

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

export interface LiveToast {
  id: string;
  tone: "success" | "danger" | "info" | "warning";
  title: string;
  message: string;
}

interface AppContextValue {
  locale: Locale;
  isAr: boolean;
  setLocale: (locale: Locale) => void;
  t: RepMessages;
  theme: Theme;
  setTheme: (theme: Theme) => void;
  user: AuthenticatedUser | null;
  authStatus: "loading" | "authenticated" | "unauthenticated";
  login: (req: LoginRequest) => Promise<AuthenticatedUser>;
  logout: () => Promise<void>;
  hasPermission: (claim: string) => boolean;
  isOnline: boolean;
  hubState: OperationsHubState;
  lastEvent: OperationsHubEvent | null;
  eventVersion: number;
  toasts: LiveToast[];
  dismissToast: (id: string) => void;
  canInstallPwa: boolean;
  promptInstallPwa: () => Promise<void>;
}

const AppContext = createContext<AppContextValue | null>(null);

export function AppProviders({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>("ar-SA");
  const [theme, setThemeState] = useState<Theme>("light");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [authStatus, setAuthStatus] = useState<"loading" | "authenticated" | "unauthenticated">("loading");
  const [isOnline, setIsOnline] = useState<boolean>(
    typeof navigator === "undefined" ? true : navigator.onLine,
  );
  const [hubState, setHubState] = useState<OperationsHubState>("disconnected");
  const [lastEvent, setLastEvent] = useState<OperationsHubEvent | null>(null);
  const [eventVersion, setEventVersion] = useState(0);
  const [toasts, setToasts] = useState<LiveToast[]>([]);
  const [installPrompt, setInstallPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const localeRef = useRef<Locale>("ar-SA");

  const setLocale = useCallback((next: Locale) => {
    localeRef.current = next;
    setLocaleState(next);
    setApiLocale(next);
    document.documentElement.lang = next === "ar-SA" ? "ar" : "en";
    document.documentElement.dir = next === "ar-SA" ? "rtl" : "ltr";
  }, []);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
    document.documentElement.classList.toggle("dark", next === "dark");
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((item) => item.id !== id));
  }, []);

  const pushToast = useCallback((toast: Omit<LiveToast, "id">) => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
    setToasts((prev) => [{ ...toast, id }, ...prev].slice(0, 4));
  }, []);

  useEffect(() => {
    document.documentElement.lang = "ar";
    document.documentElement.dir = "rtl";
    setApiLocale("ar-SA");
  }, []);

  useEffect(() => {
    const onOnline = () => setIsOnline(true);
    const onOffline = () => setIsOnline(false);
    const onBeforeInstall = (e: Event) => {
      e.preventDefault();
      setInstallPrompt(e as BeforeInstallPromptEvent);
    };

    window.addEventListener("online", onOnline);
    window.addEventListener("offline", onOffline);
    window.addEventListener("beforeinstallprompt", onBeforeInstall);
    return () => {
      window.removeEventListener("online", onOnline);
      window.removeEventListener("offline", onOffline);
      window.removeEventListener("beforeinstallprompt", onBeforeInstall);
    };
  }, []);

  useEffect(() => {
    const unsubscribe = subscribeAuthFailure(() => {
      accessTokenStore.clearAccessToken();
      setUser(null);
      setAuthStatus("unauthenticated");
    });

    let active = true;
    authService
      .refresh()
      .then((res) => {
        if (!active) return;
        setUser(res.user);
        setAuthStatus("authenticated");
      })
      .catch(() => {
        if (!active) return;
        accessTokenStore.clearAccessToken();
        setUser(null);
        setAuthStatus("unauthenticated");
      });

    return () => {
      active = false;
      unsubscribe();
    };
  }, []);

  useEffect(() => {
    if (authStatus !== "authenticated" || !user) {
      setHubState("disconnected");
      return;
    }

    const hub = createOperationsHubClient({
      hubUrl: signalRHubUrl,
      getAccessToken: () => accessTokenStore.getAccessToken(),
      onStateChange: setHubState,
      onEvent: (event) => {
        setLastEvent(event);
        setEventVersion((v) => v + 1);
        const dict = localeRef.current === "ar-SA" ? arMessages : enMessages;

        if (event.type === "OrderApproved") {
          pushToast({
            tone: "success",
            title: `${dict.realtime.approvedToast} ${event.payload.orderNumber}`,
            message: `${event.payload.totalAmount.toFixed(2)} SAR`,
          });
        } else if (event.type === "OrderRejected") {
          pushToast({
            tone: "danger",
            title: `${dict.realtime.rejectedToast} ${event.payload.orderNumber}`,
            message: event.payload.rejectionReason,
          });
        } else if (event.type === "OrderUpdated") {
          pushToast({
            tone: "info",
            title: `${dict.realtime.updatedToast} ${event.payload.orderNumber}`,
            message: `${event.payload.changeType} — ${event.payload.totalAmount.toFixed(2)} SAR`,
          });
        }
      },
    });

    void hub.start();
    return () => {
      void hub.stop();
    };
  }, [authStatus, user, pushToast]);

  const login = useCallback(async (req: LoginRequest) => {
    const res = await authService.login(req);
    setUser(res.user);
    setAuthStatus("authenticated");
    return res.user;
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
    setUser(null);
    setAuthStatus("unauthenticated");
  }, []);

  const hasPermission = useCallback(
    (claim: string) => Boolean(user?.permissions.includes(claim)),
    [user],
  );

  const promptInstallPwa = useCallback(async () => {
    if (!installPrompt) return;
    await installPrompt.prompt();
    await installPrompt.userChoice;
    setInstallPrompt(null);
  }, [installPrompt]);

  const value = useMemo<AppContextValue>(
    () => ({
      locale,
      isAr: locale === "ar-SA",
      setLocale,
      t: locale === "ar-SA" ? arMessages : enMessages,
      theme,
      setTheme,
      user,
      authStatus,
      login,
      logout,
      hasPermission,
      isOnline,
      hubState,
      lastEvent,
      eventVersion,
      toasts,
      dismissToast,
      canInstallPwa: Boolean(installPrompt),
      promptInstallPwa,
    }),
    [
      locale,
      setLocale,
      theme,
      setTheme,
      user,
      authStatus,
      login,
      logout,
      hasPermission,
      isOnline,
      hubState,
      lastEvent,
      eventVersion,
      toasts,
      dismissToast,
      installPrompt,
      promptInstallPwa,
    ],
  );

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp(): AppContextValue {
  const ctx = useContext(AppContext);
  if (!ctx) {
    throw new Error("useApp must be used inside AppProviders");
  }
  return ctx;
}
