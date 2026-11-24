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
} from "@almomiaa/api-client";
import { accessTokenStore } from "../lib/api-client";
import { useAuth } from "./AuthProvider";
import { useLocale } from "./LocaleProvider";

export interface RealtimeToast {
  id: string;
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
}

const RealtimeContext = createContext<RealtimeContextValue>({
  hubState: "disconnected",
  lastEvent: null,
  eventVersion: 0,
  toasts: [],
  dismissToast: () => undefined,
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

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((item) => item.id !== id));
  }, []);

  const pushToast = useCallback((toast: Omit<RealtimeToast, "id">) => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
    setToasts((prev) => [{ ...toast, id }, ...prev].slice(0, 4));
  }, []);

  useEffect(() => {
    if (status !== "authenticated" || !user) {
      setHubState("disconnected");
      return;
    }

    const client = createOperationsHubClient({
      hubUrl: resolveSignalRHubUrl(),
      getAccessToken: () => accessTokenStore.getAccessToken(),
      onStateChange: setHubState,
      onEvent: (event) => {
        setLastEvent(event);
        setEventVersion((v) => v + 1);
        const isAr = localeRef.current === "ar-SA";

        if (event.type === "OrderSubmitted") {
          pushToast({
            tone: event.payload.requiresFinanceCoApproval || event.payload.hasStockShortage ? "warning" : "info",
            title: isAr
              ? `طلب بيع جديد بانتظار الاعتماد: ${event.payload.orderNumber}`
              : `New Sell Request Pending Approval: ${event.payload.orderNumber}`,
            message: isAr
              ? `المندوب: ${event.payload.repName} • الإجمالي: ${event.payload.totalAmount.toFixed(2)} ر.س`
              : `Rep: ${event.payload.repName} • Total: SAR ${event.payload.totalAmount.toFixed(2)}`,
          });
        } else if (event.type === "CreditCoApprovalRequired") {
          pushToast({
            tone: "warning",
            title: isAr
              ? `يتطلب موافقة ائتمانية مالية: ${event.payload.orderNumber}`
              : `Credit Co-Approval Required: ${event.payload.orderNumber}`,
            message: isAr
              ? `الإجمالي: ${event.payload.totalAmount.toFixed(2)} ر.س`
              : `Total: SAR ${event.payload.totalAmount.toFixed(2)}`,
          });
        } else if (event.type === "OrderApproved") {
          pushToast({
            tone: "success",
            title: isAr
              ? `تم اعتماد الطلب: ${event.payload.orderNumber}`
              : `Order Approved: ${event.payload.orderNumber}`,
            message: `${event.payload.totalAmount.toFixed(2)} SAR`,
          });
        } else if (event.type === "OrderRejected") {
          pushToast({
            tone: "danger",
            title: isAr
              ? `تم رفض الطلب: ${event.payload.orderNumber}`
              : `Order Rejected: ${event.payload.orderNumber}`,
            message: event.payload.rejectionReason,
          });
        }
      },
    });

    void client.start();
    return () => {
      void client.stop();
    };
  }, [status, user, pushToast]);

  const value = useMemo<RealtimeContextValue>(
    () => ({ hubState, lastEvent, eventVersion, toasts, dismissToast }),
    [hubState, lastEvent, eventVersion, toasts, dismissToast],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime(): RealtimeContextValue {
  return useContext(RealtimeContext);
}
