import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "dotnet sexy — Make .NET Sexy Again",
  description: "The simple, modern, community-driven way to start building with .NET.",
  icons: { icon: "/favicon.svg", shortcut: "/favicon.svg" },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
