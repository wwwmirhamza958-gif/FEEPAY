import type { Metadata } from "next";
import { Lexend, Source_Sans_3 } from "next/font/google";
import { PortalShell } from "@/components/shell/PortalShell";
import "./globals.css";

const lexend = Lexend({
  subsets: ["latin"],
  variable: "--font-heading",
  display: "swap",
});

const sourceSans = Source_Sans_3({
  subsets: ["latin"],
  variable: "--font-body",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Fee Platform — Institution",
  description: "Institution portal — Fee Platform (A2Z Creatorz)",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${lexend.variable} ${sourceSans.variable}`}>
      <body className="fee-canvas antialiased">
        <PortalShell>{children}</PortalShell>
      </body>
    </html>
  );
}
