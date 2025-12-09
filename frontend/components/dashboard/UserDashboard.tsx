"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { authApi, usersApi, apiClient } from "@/lib/api";
import Header from "@/components/common/Header";
import LoadingSpinner from "@/components/common/LoadingSpinner";
import ToolsGrid from "@/components/dashboard/ToolsGrid";
import AccountInfo from "@/components/dashboard/AccountInfo";
import UsageStatistics from "@/components/dashboard/UsageStatistics";

export default function UserDashboard() {
  const router = useRouter();
  const [user, setUser] = useState<any>(null);
  const [usageStats, setUsageStats] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token =
      typeof window !== "undefined" ? localStorage.getItem("token") : null;
    if (!token) {
      router.push("/login");
      return;
    }

    const fetchData = async () => {
      try {
        const [profileResponse, statsResponse] = await Promise.all([
          usersApi.getProfile(),
          usersApi.getUsageStatistics(),
        ]);

        if (profileResponse.data) {
          setUser(profileResponse.data);
        }
        if (statsResponse.data) {
          setUsageStats(statsResponse.data);
        }
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [router]);

  const handleLogout = async () => {
    await authApi.logout();
    // ✅ Clear both tokens
    apiClient.clearTokens();
    router.push("/");
  };

  if (loading) {
    return <LoadingSpinner />;
  }

  const isAdmin =
    user?.subscriptionTier === "Admin" || user?.subscriptionTier === "admin";

  return (
    <div className="min-h-screen bg-gray-50">
      <Header
        title="UtilityTools"
        showAdminLink={isAdmin}
        onLogout={handleLogout}
      />

      <main className="container mx-auto px-4 py-12">
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
          <p className="text-gray-600">
            Welcome back! Choose a tool to get started.
          </p>
        </div>

        <ToolsGrid />

        <div className="grid md:grid-cols-2 gap-6">
          <AccountInfo user={user || {}} />
          <UsageStatistics stats={usageStats} />
        </div>
      </main>
    </div>
  );
}

