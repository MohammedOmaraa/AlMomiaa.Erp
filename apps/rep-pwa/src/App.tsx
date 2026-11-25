import type { ReactNode } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes } from "react-router-dom";
import { Button } from "@almomiaa/ui";
import { AppProviders, useApp } from "./providers/AppProviders";
import { LoginPage } from "./pages/LoginPage";
import { RepHomePage } from "./pages/RepHomePage";
import { NewSellRequestPage } from "./pages/NewSellRequestPage";
import { MyRequestsPage } from "./pages/MyRequestsPage";

function RequireAuth({ children }: { children: ReactNode }) {
  const { authStatus, slowBootstrap, retrySession, isAr, t } = useApp();
  if (authStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-canvas p-4 text-text-primary">
        <div
          role="status"
          className="w-full max-w-sm space-y-3 rounded-xl border border-surface-border bg-surface-card p-6 text-center"
        >
          <div className="mx-auto h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
          <p className="text-sm text-text-secondary">{t.auth.checkingSession}</p>
          {slowBootstrap && (
            <>
              <p className="text-xs text-text-muted">
                {isAr
                  ? "قد يستغرق الخادم السحابي بضع ثوانٍ للاستيقاظ. يمكنك إعادة المحاولة أو الذهاب لتسجيل الدخول."
                  : "The cloud server may take a few seconds to wake up. You can retry or go to Sign In."}
              </p>
              <div className="flex flex-wrap justify-center gap-2 pt-1">
                <Button variant="secondary" size="compact" onClick={() => void retrySession()}>
                  {isAr ? "إعادة المحاولة" : "Retry"}
                </Button>
                <Link
                  to="/login"
                  className="inline-flex min-h-9 items-center justify-center rounded-lg bg-brand-gold px-3 text-xs font-semibold text-surface-obsidian"
                >
                  {isAr ? "تسجيل الدخول" : "Sign In"}
                </Link>
              </div>
            </>
          )}
        </div>
      </div>
    );
  }
  if (authStatus !== "authenticated") {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

export function App() {
  return (
    <AppProviders>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/"
            element={
              <RequireAuth>
                <RepHomePage />
              </RequireAuth>
            }
          />
          <Route
            path="/new-request"
            element={
              <RequireAuth>
                <NewSellRequestPage />
              </RequireAuth>
            }
          />
          <Route
            path="/requests"
            element={
              <RequireAuth>
                <MyRequestsPage />
              </RequireAuth>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AppProviders>
  );
}
