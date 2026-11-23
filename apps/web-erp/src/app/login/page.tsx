"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LocaleSwitcher, ThemeToggle, type Theme } from "@almomiaa/ui";
import { useTheme } from "next-themes";
import { LoginForm } from "../../features/auth/LoginForm";
import { ar } from "../../locales/ar";
import { en } from "../../locales/en";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

export default function LoginPage() {
  const router = useRouter();
  const { locale, setLocale } = useLocale();
  const { resolvedTheme, setTheme } = useTheme();
  const { status } = useAuth();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (status === "authenticated") {
      router.replace("/");
    }
  }, [status, router]);

  const t = locale === "ar-SA" ? ar : en;
  const theme: Theme = mounted && resolvedTheme === "dark" ? "dark" : "light";

  return (
    <div className="flex min-h-screen w-full flex-col overflow-x-clip bg-surface-canvas text-text-primary">
      <header className="flex min-h-16 items-center justify-end gap-2 border-b border-surface-border bg-surface-card px-4 py-2 sm:px-6">
        <LocaleSwitcher
          value={locale}
          onChange={setLocale}
          label={t.auth.languageLabel}
        />
        <ThemeToggle
          value={theme}
          onChange={(nextTheme) => setTheme(nextTheme)}
          labels={{
            light: t.auth.switchToDark,
            dark: t.auth.switchToLight,
          }}
        />
      </header>
      <main className="flex flex-1 items-center justify-center p-4 sm:p-6">
        <LoginForm />
      </main>
    </div>
  );
}
