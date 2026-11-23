"use client";

import { ThemeProvider as NextThemesProvider } from "next-themes";
import type { ReactNode } from "react";

export function ThemeProvider({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <NextThemesProvider attribute="data-theme" defaultTheme="system" enableSystem storageKey="almomiaa-theme" disableTransitionOnChange>
      {children}
    </NextThemesProvider>
  );
}
