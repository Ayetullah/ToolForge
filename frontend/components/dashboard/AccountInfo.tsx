"use client";

import Link from "next/link";

interface AccountInfoProps {
  user: {
    firstName?: string;
    lastName?: string;
    email?: string;
    subscriptionTier?: string;
    subscriptionExpiresAt?: string;
    isEmailVerified?: boolean;
  };
}

export default function AccountInfo({ user }: AccountInfoProps) {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-xl font-semibold text-gray-900 mb-4">Account Information</h2>
      <div className="space-y-2">
        <p className="text-gray-800">
          <span className="font-medium text-gray-900">Name:</span> {user?.firstName}{" "}
          {user?.lastName}
        </p>
        <p className="text-gray-800">
          <span className="font-medium text-gray-900">Email:</span> {user?.email}
        </p>
        <p className="text-gray-800">
          <span className="font-medium text-gray-900">Subscription:</span>{" "}
          <span
            className={`px-2 py-1 rounded text-sm ${
              user?.subscriptionTier === "Premium" ||
              user?.subscriptionTier === "Pro" ||
              user?.subscriptionTier === "Enterprise"
                ? "bg-green-100 text-green-800"
                : "bg-gray-100 text-gray-800"
            }`}
          >
            {user?.subscriptionTier || "Free"}
          </span>
        </p>
        {user?.subscriptionExpiresAt && (
          <p className="text-gray-800">
            <span className="font-medium text-gray-900">Expires:</span>{" "}
            {new Date(user.subscriptionExpiresAt).toLocaleDateString()}
          </p>
        )}
        <p className="text-gray-800">
          <span className="font-medium text-gray-900">Email Verified:</span>{" "}
          {user?.isEmailVerified ? (
            <span className="text-green-600">✓ Verified</span>
          ) : (
            <span className="text-red-600">✗ Not Verified</span>
          )}
        </p>
        <Link
          href="/pricing"
          className="inline-block mt-4 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          Upgrade Plan
        </Link>
      </div>
    </div>
  );
}

