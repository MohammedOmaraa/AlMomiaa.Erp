import type { ReactNode } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppProviders, useApp } from "./providers/AppProviders";
import { LoginPage } from "./pages/LoginPage";
import { RepHomePage } from "./pages/RepHomePage";
import { NewSellRequestPage } from "./pages/NewSellRequestPage";
import { MyRequestsPage } from "./pages/MyRequestsPage";

function RequireAuth({ children }: { children: ReactNode }) {
  const { authStatus, t } = useApp();
  if (authStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-canvas text-sm text-text-secondary">
        {t.auth.checkingSession}
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
