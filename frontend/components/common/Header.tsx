"use client";

import Link from "next/link";
import { LogOut } from "lucide-react";

interface HeaderProps {
  title: string;
  showAdminLink?: boolean;
  onLogout: () => void;
  showDashboard?: boolean;
}

export default function Header({
  title,
  showAdminLink,
  onLogout,
  showDashboard = true,
}: HeaderProps) {
  return (
    <header className="border-b bg-white">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <Link href="/" className="text-2xl font-bold text-blue-600">
          {title}
        </Link>
        <nav className="flex gap-6 items-center">
          <Link href="/tools" className="text-gray-600 hover:text-gray-900">
            Tools
          </Link>
          {showAdminLink && (
            <Link
              href="/admin/dashboard"
              className="text-gray-600 hover:text-gray-900"
            >
              Admin Dashboard
            </Link>
          )}
          {showDashboard && (
            <Link href="/dashboard" className="text-gray-600 hover:text-gray-900">
              Dashboard
            </Link>
          )}
          <button
            onClick={onLogout}
            className="flex items-center gap-2 text-gray-600 hover:text-gray-900"
          >
            <LogOut className="w-5 h-5" />
            Logout
          </button>
        </nav>
      </div>
    </header>
  );
}

