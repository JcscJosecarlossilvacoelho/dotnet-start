import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: ".NET Start — .NET starts here",
  description: "The simple, modern, community-driven way to start building with .NET.",
  icons: { icon: "/favicon.svg", shortcut: "/favicon.svg" },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
