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
  type ChangePasswordRequest,
  type LoginRequest,
  type OperationsHubEvent,
  type OperationsHubState,
  type UserNotificationDto,
} from "@almomiaa/api-client";
import type { Locale, Theme } from "@almomiaa/ui";
import {
  accessTokenStore,
  authService,
  erpApi,
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
  notificationId?: string;
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
  slowBootstrap: boolean;
  retrySession: () => Promise<void>;
  login: (req: LoginRequest) => Promise<AuthenticatedUser>;
  logout: () => Promise<void>;
  changePassword: (req: ChangePasswordRequest) => Promise<void>;
  hasPermission: (claim: string) => boolean;
  isOnline: boolean;
  hubState: OperationsHubState;
  lastEvent: OperationsHubEvent | null;
  eventVersion: number;
  toasts: LiveToast[];
  dismissToast: (id: string) => void;
  notifications: UserNotificationDto[];
  unreadCount: number;
  loadingNotifications: boolean;
  refreshNotifications: () => Promise<void>;
  markRead: (notificationId: string) => Promise<void>;
  markAllRead: () => Promise<void>;
  mutedPopups: boolean;
  setMutedPopups: (muted: boolean) => void;
  canInstallPwa: boolean;
  promptInstallPwa: () => Promise<void>;
}

const AppContext = createContext<AppContextValue | null>(null);

export function AppProviders({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>("ar-SA");
  const [theme, setThemeState] = useState<Theme>("light");
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [authStatus, setAuthStatus] = useState<"loading" | "authenticated" | "unauthenticated">("loading");
  const [slowBootstrap, setSlowBootstrap] = useState(false);
  const mountedRef = useRef(true);
  const [isOnline, setIsOnline] = useState<boolean>(
    typeof navigator === "undefined" ? true : navigator.onLine,
  );
  const [hubState, setHubState] = useState<OperationsHubState>("disconnected");
  const [lastEvent, setLastEvent] = useState<OperationsHubEvent | null>(null);
  const [eventVersion, setEventVersion] = useState(0);
  const [toasts, setToasts] = useState<LiveToast[]>([]);
  const [notifications, setNotifications] = useState<UserNotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loadingNotifications, setLoadingNotifications] = useState(false);
  const [mutedPopups, setMutedPopupsState] = useState(false);
  const [installPrompt, setInstallPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const localeRef = useRef<Locale>("ar-SA");
  const mutedRef = useRef(false);

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

  const setMutedPopups = useCallback((muted: boolean) => {
    mutedRef.current = muted;
    setMutedPopupsState(muted);
    if (muted) {
      setToasts([]);
    }
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((item) => item.id !== id));
  }, []);

  const pushToast = useCallback((toast: Omit<LiveToast, "id">) => {
    if (mutedRef.current) return;
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
    setToasts((prev) => [{ ...toast, id }, ...prev].slice(0, 4));
  }, []);

  const refreshNotifications = useCallback(async () => {
    if (authStatus !== "authenticated" || !user) return;
    setLoadingNotifications(true);
    try {
      const page = await erpApi.notifications.getNotifications({ page: 1, pageSize: 50 });
      setNotifications(page.items);
      setUnreadCount(page.unreadCount);
    } catch {
      // Keep existing state on transient error
    } finally {
      setLoadingNotifications(false);
    }
  }, [authStatus, user]);

  const markRead = useCallback(async (notificationId: string) => {
    const res = await erpApi.notifications.markRead(notificationId);
    setNotifications((prev) =>
      prev.map((item) => (item.id === notificationId ? res.notification : item)),
    );
    setUnreadCount(res.unreadCount);
    setToasts((prev) => prev.filter((t) => t.notificationId !== notificationId));
  }, []);

  const markAllRead = useCallback(async () => {
    await erpApi.notifications.markAllRead();
    const nowIso = new Date().toISOString();
    setNotifications((prev) =>
      prev.map((item) => (item.isRead ? item : { ...item, isRead: true, readAtUtc: nowIso })),
    );
    setUnreadCount(0);
    setToasts([]);
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
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  const retrySession = useCallback(async () => {
    if (!mountedRef.current) return;
    setAuthStatus("loading");
    setSlowBootstrap(false);

    const slowTimer = window.setTimeout(() => {
      if (mountedRef.current) {
        setSlowBootstrap(true);
      }
    }, 5_000);

    const controller = new AbortController();
    const timeoutTimer = window.setTimeout(() => {
      controller.abort();
    }, 18_000);

    try {
      const res = await authService.refresh(controller.signal);
      if (!mountedRef.current) return;
      setUser(res.user);
      setSlowBootstrap(false);
      setAuthStatus("authenticated");
    } catch {
      if (!mountedRef.current) return;
      accessTokenStore.clearAccessToken();
      setUser(null);
      setSlowBootstrap(false);
      setAuthStatus("unauthenticated");
    } finally {
      window.clearTimeout(slowTimer);
      window.clearTimeout(timeoutTimer);
    }
  }, []);

  useEffect(() => {
    const unsubscribe = subscribeAuthFailure(() => {
      if (!mountedRef.current) return;
      accessTokenStore.clearAccessToken();
      setUser(null);
      setSlowBootstrap(false);
      setAuthStatus("unauthenticated");
    });

    void retrySession();

    return () => {
      unsubscribe();
    };
  }, [retrySession]);

  useEffect(() => {
    if (authStatus !== "authenticated" || !user) {
      setNotifications([]);
      setUnreadCount(0);
      setToasts([]);
      setHubState("disconnected");
      return;
    }

    void refreshNotifications();

    const hub = createOperationsHubClient({
      hubUrl: signalRHubUrl,
      getAccessToken: () => accessTokenStore.getAccessToken(),
      onStateChange: setHubState,
      onEvent: (event) => {
        setLastEvent(event);
        setEventVersion((v) => v + 1);
        const isArNow = localeRef.current === "ar-SA";

        if (event.type === "NotificationReceived") {
          const n = event.payload;
          setNotifications((prev) => {
            if (prev.some((x) => x.id === n.id)) return prev;
            return [n, ...prev].slice(0, 50);
          });
          if (!n.isRead) {
            setUnreadCount((c) => c + 1);
            pushToast({
              notificationId: n.id,
              tone: n.severity,
              title: isArNow ? n.titleAr : n.titleEn,
              message: isArNow ? n.messageAr : n.messageEn,
            });
          }
        } else if (event.type === "NotificationsMarkedAllRead") {
          const nowIso = new Date().toISOString();
          setNotifications((prev) =>
            prev.map((item) => (item.isRead ? item : { ...item, isRead: true, readAtUtc: nowIso })),
          );
          setUnreadCount(0);
          setToasts([]);
        }
      },
    });

    void hub.start();
    return () => {
      void hub.stop();
    };
  }, [authStatus, user, pushToast, refreshNotifications]);

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

  const changePassword = useCallback(async (req: ChangePasswordRequest) => {
    await authService.changePassword(req);
    accessTokenStore.clearAccessToken();
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
      slowBootstrap,
      retrySession,
      login,
      logout,
      changePassword,
      hasPermission,
      isOnline,
      hubState,
      lastEvent,
      eventVersion,
      toasts,
      dismissToast,
      notifications,
      unreadCount,
      loadingNotifications,
      refreshNotifications,
      markRead,
      markAllRead,
      mutedPopups,
      setMutedPopups,
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
      slowBootstrap,
      retrySession,
      login,
      logout,
      changePassword,
      hasPermission,
      isOnline,
      hubState,
      lastEvent,
      eventVersion,
      toasts,
      dismissToast,
      notifications,
      unreadCount,
      loadingNotifications,
      refreshNotifications,
      markRead,
      markAllRead,
      mutedPopups,
      setMutedPopups,
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
