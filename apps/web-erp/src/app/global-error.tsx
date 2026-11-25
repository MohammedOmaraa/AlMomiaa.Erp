"use client";

import "@almomiaa/ui/tokens.css";
import "./globals.css";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <html lang="ar-SA" dir="rtl">
      <body className="flex min-h-screen items-center justify-center bg-surface-canvas p-6 text-text-primary">
        <div className="w-full max-w-md space-y-4 rounded-xl border border-surface-border bg-surface-card p-6 text-center shadow-sm">
          <h1 className="text-lg font-bold text-text-primary">
            حدث خطأ في التطبيق | Application Error
          </h1>
          <p className="text-sm text-text-secondary">
            {error.message || "تعذر إكمال التحميل. يرجى إعادة المحاولة أو تسجيل الدخول مجدداً."}
          </p>
          <div className="flex flex-wrap justify-center gap-2">
            <button
              type="button"
              onClick={() => reset()}
              className="rounded-lg bg-brand-gold px-4 py-2 text-sm font-semibold text-surface-obsidian"
            >
              إعادة المحاولة / Retry
            </button>
            <a
              href="/login"
              className="rounded-lg border border-surface-border bg-surface-canvas px-4 py-2 text-sm font-semibold text-text-primary"
            >
              تسجيل الدخول / Sign In
            </a>
          </div>
        </div>
      </body>
    </html>
  );
}
