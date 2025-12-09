"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { adminApi, authApi, apiClient } from "@/lib/api";
import Header from "@/components/common/Header";
import LoadingSpinner from "@/components/common/LoadingSpinner";
import StatsCards from "@/components/dashboard/StatsCards";
import UsersTable from "@/components/dashboard/UsersTable";

interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  subscriptionTier: string;
  totalUsageCount: number;
  isEmailVerified: boolean;
  createdAt: string;
}

interface UsersResponse {
  users: User[];
  totalPages: number;
}

interface SystemStats {
  totalUsers: number;
  activeUsers: number;
  totalOperations: number;
  totalFileSizeProcessed: number;
}

export default function AdminDashboard() {
  const router = useRouter();
  const [stats, setStats] = useState<SystemStats | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [searchTerm, setSearchTerm] = useState("");
  const [subscriptionFilter, setSubscriptionFilter] = useState("");

  useEffect(() => {
    const token =
      typeof window !== "undefined" ? localStorage.getItem("token") : null;
    if (!token) {
      router.push("/login");
      return;
    }

    const checkAdminAndFetchData = async () => {
      try {
        const [statsResponse, usersResponse] = await Promise.all([
          adminApi.getSystemStats(),
          adminApi.getAllUsers(
            page,
            20,
            searchTerm || undefined,
            subscriptionFilter || undefined
          ),
        ]);

        if (statsResponse.data) {
          setStats(statsResponse.data as SystemStats);
        }
        if (usersResponse.data) {
          const data = usersResponse.data as UsersResponse;
          setUsers(data.users || []);
        }
      } catch (error: unknown) {
        const err = error as { response?: { status?: number } };
        if (err.response?.status === 403 || err.response?.status === 401) {
          router.push("/dashboard");
          return;
        }
        console.error("Error fetching admin data:", error);
      } finally {
        setLoading(false);
      }
    };

    checkAdminAndFetchData();
  }, [router, page, searchTerm, subscriptionFilter]);

  const handleLogout = async () => {
    await authApi.logout();
    // ✅ Clear both tokens
    apiClient.clearTokens();
    router.push("/");
  };

  if (loading) {
    return <LoadingSpinner />;
  }

  const totalPages = stats
    ? Math.ceil((stats.totalUsers || 0) / 20)
    : 1;

  return (
    <div className="min-h-screen bg-gray-50">
      <Header title="UtilityTools Admin" onLogout={handleLogout} />

      <main className="container mx-auto px-4 py-12">
        <h1 className="text-3xl font-bold mb-8">Admin Dashboard</h1>

        <StatsCards stats={stats} />

        <UsersTable
          users={users}
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          subscriptionFilter={subscriptionFilter}
          onFilterChange={setSubscriptionFilter}
          page={page}
          totalPages={totalPages}
          onPageChange={setPage}
        />
      </main>
    </div>
  );
}

