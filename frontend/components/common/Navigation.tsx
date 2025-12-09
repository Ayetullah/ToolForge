"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { LogOut } from "lucide-react";
import { apiClient } from "@/lib/api";
import { getUserRoleFromToken } from "@/lib/auth";

export default function Navigation() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    // Check authentication status
    const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
    if (token) {
      setIsAuthenticated(true);
      const userRole = getUserRoleFromToken(token);
      setIsAdmin(userRole?.isAdmin ?? false);
    } else {
      setIsAuthenticated(false);
      setIsAdmin(false);
    }
    setIsLoading(false);
  }, []);

  const handleLogout = async () => {
    try {
      apiClient.clearTokens();
      setIsAuthenticated(false);
      setIsAdmin(false);
      router.push("/");
      router.refresh(); // Refresh to update UI
    } catch (error) {
      // Clear tokens anyway
      apiClient.clearTokens();
      setIsAuthenticated(false);
      setIsAdmin(false);
      router.push("/");
    }
  };

  if (isLoading) {
    // Show loading state (optional - can show skeleton or nothing)
    return null;
  }

  return (
    <nav className="flex gap-6 items-center">
      <Link href="/tools" className="text-gray-700 hover:text-gray-900 font-medium">
        Tools
      </Link>
      <Link href="/pricing" className="text-gray-700 hover:text-gray-900 font-medium">
        Pricing
      </Link>
      {isAuthenticated ? (
        <>
          {isAdmin && (
            <Link
              href="/admin/dashboard"
              className="text-gray-700 hover:text-gray-900 font-medium"
            >
              Admin
            </Link>
          )}
          <Link href="/dashboard" className="text-gray-700 hover:text-gray-900 font-medium">
            Dashboard
          </Link>
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 text-gray-700 hover:text-gray-900 font-medium"
          >
            <LogOut className="w-5 h-5" />
            Logout
          </button>
        </>
      ) : (
        <>
          <Link href="/login" className="text-gray-700 hover:text-gray-900 font-medium">
            Login
          </Link>
          <Link
            href="/register"
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            Sign Up
          </Link>
        </>
      )}
    </nav>
  );
}

