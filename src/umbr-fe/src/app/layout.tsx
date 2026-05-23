import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";

import { SiteFooter } from "@/components/layout/site-footer";
import { SiteHeader } from "@/components/layout/site-header";
import { FaroProvider } from "@/components/observability/faro-provider";
import { Toaster } from "@/components/ui/sonner";
import { getSiteSettings } from "@/lib/cms/client";
import { getFaroCollectorUrl } from "@/lib/env";

import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export async function generateMetadata(): Promise<Metadata> {
  const settings = await getSiteSettings();

  return {
    title: {
      default: settings.siteName,
      template: `%s · ${settings.siteName}`,
    },
    description: settings.tagline,
  };
}

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const settings = await getSiteSettings();
  const faroCollectorUrl = getFaroCollectorUrl();

  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col">
        <FaroProvider collectorUrl={faroCollectorUrl}>
          <SiteHeader siteName={settings.siteName} />
          <main className="flex-1">{children}</main>
          <SiteFooter siteName={settings.siteName} tagline={settings.tagline} />
          <Toaster />
        </FaroProvider>
      </body>
    </html>
  );
}
