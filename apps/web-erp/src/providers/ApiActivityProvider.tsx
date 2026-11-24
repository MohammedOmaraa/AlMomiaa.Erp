"use client";

import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import type { ApiCallLog } from "@almomiaa/api-client";
import { subscribeApiActivity } from "../lib/api-client";

interface ApiActivityContextValue {
  logs: ApiCallLog[];
  inspectorOpen: boolean;
  setInspectorOpen: (open: boolean) => void;
  clearLogs: () => void;
}

const ApiActivityContext = createContext<ApiActivityContextValue | null>(null);

export function ApiActivityProvider({ children }: { children: ReactNode }) {
  const [logs, setLogs] = useState<ApiCallLog[]>([]);
  const [inspectorOpen, setInspectorOpen] = useState(false);

  useEffect(() => {
    return subscribeApiActivity((entry) => {
      setLogs((prev) => [entry, ...prev].slice(0, 25));
    });
  }, []);

  const clearLogs = useCallback(() => setLogs([]), []);

  return (
    <ApiActivityContext.Provider value={{ logs, inspectorOpen, setInspectorOpen, clearLogs }}>
      {children}
    </ApiActivityContext.Provider>
  );
}

export function useApiActivity(): ApiActivityContextValue {
  const ctx = useContext(ApiActivityContext);
  if (!ctx) {
    throw new Error("useApiActivity must be used inside ApiActivityProvider.");
  }
  return ctx;
}
