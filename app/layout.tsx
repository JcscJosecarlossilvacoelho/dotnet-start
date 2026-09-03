import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: ".NET Start — .NET starts here",
  description: "Practical .NET docs, a recommended stack, and trusted context for coding agents — from first project to production.",
  icons: { icon: "/favicon.svg", shortcut: "/favicon.svg" },
  themeColor: "#09070d",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body><a className="skip-link" href="#main-content">Skip to content</a>{children}</body></html>;
}
