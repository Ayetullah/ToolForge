"use client";

import { Users, Activity, FileText, TrendingUp, LucideIcon } from "lucide-react";

interface StatCard {
  icon: LucideIcon;
  label: string;
  value: string | number;
  iconColor: string;
  bgColor: string;
}

interface StatsCardsProps {
  stats: {
    totalUsers?: number;
    activeUsers?: number;
    totalOperations?: number;
    totalFileSizeProcessed?: number;
  } | null;
}

export default function StatsCards({ stats }: StatsCardsProps) {
  const cards: StatCard[] = [
    {
      icon: Users,
      label: "Total Users",
      value: stats?.totalUsers || 0,
      iconColor: "text-blue-600",
      bgColor: "bg-blue-100",
    },
    {
      icon: Activity,
      label: "Active Users",
      value: stats?.activeUsers || 0,
      iconColor: "text-green-600",
      bgColor: "bg-green-100",
    },
    {
      icon: FileText,
      label: "Total Operations",
      value: stats?.totalOperations || 0,
      iconColor: "text-purple-600",
      bgColor: "bg-purple-100",
    },
    {
      icon: TrendingUp,
      label: "Data Processed",
      value: stats?.totalFileSizeProcessed
        ? `${(stats.totalFileSizeProcessed / 1024 / 1024 / 1024).toFixed(2)} GB`
        : "0 GB",
      iconColor: "text-yellow-600",
      bgColor: "bg-yellow-100",
    },
  ];

  return (
    <div className="grid md:grid-cols-4 gap-6 mb-8">
      {cards.map((card, index) => {
        const Icon = card.icon;
        return (
          <div key={index} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center gap-4">
              <div
                className={`w-12 h-12 ${card.bgColor} rounded-lg flex items-center justify-center`}
              >
                <Icon className={`w-6 h-6 ${card.iconColor}`} />
              </div>
              <div>
                <p className="text-sm text-gray-600">{card.label}</p>
                <p className="text-2xl font-bold">{card.value}</p>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

