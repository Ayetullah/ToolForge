"use client";

import { Inter } from "next/font/google";
import { useEffect } from "react";
import { initializeTokenRefresh } from "@/lib/tokenManager";
import "./globals.css";

const inter = Inter({ subsets: ["latin"] });

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // ✅ Initialize token refresh mechanism on app load
  useEffect(() => {
    const cleanup = initializeTokenRefresh();
    return cleanup; // Cleanup on unmount
  }, []);

  return (
    <html lang="en">
      <head>
        <title>UtilityTools - All Your Utility Tools In One Place</title>
        <meta
          name="description"
          content="Powerful, fast, and secure tools for PDF, images, documents, and more. No installation required."
        />
        <meta
          name="keywords"
          content="PDF tools, image compression, document converter, utility tools, online tools"
        />
      </head>
      <body className={inter.className}>{children}</body>
    </html>
  );
}
