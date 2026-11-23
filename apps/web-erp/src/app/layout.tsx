import type { Metadata } from "next";
import type { ReactNode } from "react";
import { cookies } from "next/headers";
import "@almomiaa/ui/tokens.css";
import "./globals.css";
import { AuthProvider } from "../providers/AuthProvider";
import { LocaleProvider } from "../providers/LocaleProvider";
import { ThemeProvider } from "../providers/ThemeProvider";

export const metadata: Metadata = {
  title: "المومياء | AlMomiaa ERP",
  description: "AlMomiaa ERP workspace",
};

type RootLayoutProps = Readonly<{ children: ReactNode }>;

export default async function RootLayout({ children }: RootLayoutProps) {
  const cookieStore = await cookies();
  const cookieLocale = cookieStore.get("almomiaa-locale")?.value;
  const locale = cookieLocale === "en-US" ? "en-US" : "ar-SA";
  const dir = locale === "ar-SA" ? "rtl" : "ltr";

  return (
    <html lang={locale} dir={dir} suppressHydrationWarning>
      <body>
        <ThemeProvider>
          <LocaleProvider initialLocale={locale}>
            <AuthProvider>{children}</AuthProvider>
          </LocaleProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
