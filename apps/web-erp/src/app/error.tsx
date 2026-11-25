"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { AlertBanner, Button, SurfaceCard } from "@almomiaa/ui";
import { useLocale } from "../providers/LocaleProvider";

export default function AppErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const router = useRouter();
  const { locale } = useLocale();
  const isAr = locale === "ar-SA";

  useEffect(() => {
    if (error) {
      // Keep error diagnostic safe (message only, no sensitive headers)
      console.error("AlMomiaa ERP route error:", error.message);
    }
  }, [error]);

  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-4 text-text-primary">
      <SurfaceCard className="w-full max-w-md space-y-4 p-6">
        <AlertBanner
          tone="danger"
          title={isAr ? "حدث خطأ غير متوقع في الصفحة" : "An unexpected page error occurred"}
          message={
            error.message
            || (isAr
              ? "تعذر تحميل هذه الصفحة مؤقتاً. يمكنك إعادة المحاولة أو العودة لتسجيل الدخول."
              : "This page could not be loaded right now. Try again or return to Sign In.")
          }
        />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="secondary" size="compact" onClick={() => router.replace("/login")}>
            {isAr ? "تسجيل الدخول" : "Sign In"}
          </Button>
          <Button variant="secondary" size="compact" onClick={() => router.replace("/")}>
            {isAr ? "الرئيسية" : "Dashboard"}
          </Button>
          <Button size="compact" onClick={() => reset()}>
            {isAr ? "إعادة المحاولة" : "Try Again"}
          </Button>
        </div>
      </SurfaceCard>
    </div>
  );
}
