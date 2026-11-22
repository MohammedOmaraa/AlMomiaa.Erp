import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";

export const metadata: Metadata = {
  title: "المومية | تطبيق الويب",
  description: "مساحة تطبيق الويب الخاصة بالمومية.",
};

type RootLayoutProps = Readonly<{
  children: ReactNode;
}>;

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html lang="ar-SA" dir="rtl">
      <body>{children}</body>
    </html>
  );
}
