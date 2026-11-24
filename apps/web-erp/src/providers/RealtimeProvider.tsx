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
import {
  createOperationsHubClient,
  type OperationsHubEvent,
  type OperationsHubState,
  type UserNotificationDto,
} from "@almomiaa/api-client";
import { accessTokenStore, erpApi } from "../lib/api-client";
import { useAuth } from "./AuthProvider";
import { useLocale } from "./LocaleProvider";

export interface RealtimeToast {
  id: string;
  notificationId?: string;
  tone: "info" | "success" | "warning" | "danger";
  title: string;
  message: string;
}

interface RealtimeContextValue {
  hubState: OperationsHubState;
  lastEvent: OperationsHubEvent | null;
  eventVersion: number;
  toasts: RealtimeToast[];
  dismissToast: (id: string) => void;
  notifications: UserNotificationDto[];
  unreadCount: number;
  loadingNotifications: boolean;
  refreshNotifications: () => Promise<void>;
  markRead: (notificationId: string) => Promise<void>;
  markAllRead: () => Promise<void>;
  mutedPopups: boolean;
  setMutedPopups: (muted: boolean) => void;
}

const RealtimeContext = createContext<RealtimeContextValue>({
  hubState: "disconnected",
  lastEvent: null,
  eventVersion: 0,
  toasts: [],
  dismissToast: () => undefined,
  notifications: [],
  unreadCount: 0,
  loadingNotifications: false,
  refreshNotifications: async () => undefined,
  markRead: async () => undefined,
  markAllRead: async () => undefined,
  mutedPopups: false,
  setMutedPopups: () => undefined,
});

function resolveSignalRHubUrl(): string {
  const configured = process.env.NEXT_PUBLIC_SIGNALR_HUB_URL?.trim();
  if (configured) return configured;

  const apiUrl = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
  if (apiUrl && (apiUrl.startsWith("http://") || apiUrl.startsWith("https://"))) {
    try {
      return `${new URL(apiUrl).origin}/hubs/operations`;
    } catch {
      return "http://localhost:5000/hubs/operations";
    }
  }
  return "http://localhost:5000/hubs/operations";
}

export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { status, user } = useAuth();
  const { locale } = useLocale();
  const localeRef = useRef(locale);
  localeRef.current = locale;

  const [hubState, setHubState] = useState<OperationsHubState>("disconnected");
  const [lastEvent, setLastEvent] = useState<OperationsHubEvent | null>(null);
  const [eventVersion, setEventVersion] = useState(0);
  const [toasts, setToasts] = useState<RealtimeToast[]>([]);
  const [notifications, setNotifications] = useState<UserNotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loadingNotifications, setLoadingNotifications] = useState(false);
  const [mutedPopups, setMutedPopupsState] = useState(false);
  const mutedRef = useRef(false);

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

  const pushToast = useCallback((toast: Omit<RealtimeToast, "id">) => {
    if (mutedRef.current) return;
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
    setToasts((prev) => [{ ...toast, id }, ...prev].slice(0, 4));
  }, []);

  const refreshNotifications = useCallback(async () => {
    if (status !== "authenticated" || !user) return;
    setLoadingNotifications(true);
    try {
      const page = await erpApi.notifications.getNotifications({ page: 1, pageSize: 50 });
      setNotifications(page.items);
      setUnreadCount(page.unreadCount);
    } catch {
      // Keep existing state silently on transient network hiccup
    } finally {
      setLoadingNotifications(false);
    }
  }, [status, user]);

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
    if (status !== "authenticated" || !user) {
      setNotifications([]);
      setUnreadCount(0);
      setToasts([]);
      setHubState("disconnected");
      return;
    }

    void refreshNotifications();

    const client = createOperationsHubClient({
      hubUrl: resolveSignalRHubUrl(),
      getAccessToken: () => accessTokenStore.getAccessToken(),
      onStateChange: setHubState,
      onEvent: (event) => {
        setLastEvent(event);
        setEventVersion((v) => v + 1);
        const isAr = localeRef.current === "ar-SA";

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
              title: isAr ? n.titleAr : n.titleEn,
              message: isAr ? n.messageAr : n.messageEn,
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

    void client.start();
    return () => {
      void client.stop();
    };
  }, [status, user, pushToast, refreshNotifications]);

  const value = useMemo<RealtimeContextValue>(
    () => ({
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
    }),
    [
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
    ],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime(): RealtimeContextValue {
  return useContext(RealtimeContext);
}
