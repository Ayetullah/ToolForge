/**
 * Token Manager - Handles token expiration and automatic refresh
 */

import { apiClient } from "./api";

interface TokenPayload {
  exp: number;
  [key: string]: any;
}

/**
 * Decode JWT token without verification (client-side only)
 */
function decodeToken(token: string): TokenPayload | null {
  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error("Error decoding token:", error);
    return null;
  }
}

/**
 * Check if token is expired or will expire soon (within 5 minutes)
 */
function isTokenExpiredOrExpiringSoon(token: string | null): boolean {
  if (!token) return true;

  const decoded = decodeToken(token);
  if (!decoded || !decoded.exp) return true;

  const expirationTime = decoded.exp * 1000; // Convert to milliseconds
  const now = Date.now();
  const fiveMinutes = 5 * 60 * 1000; // 5 minutes in milliseconds

  return expirationTime - now < fiveMinutes;
}

/**
 * Initialize token refresh mechanism
 * Checks token expiration periodically and refreshes if needed
 */
export function initializeTokenRefresh(): () => void {
  let intervalId: NodeJS.Timeout | null = null;

  const checkAndRefreshToken = async () => {
    if (typeof window === "undefined") return;

    const token = localStorage.getItem("token");
    const refreshToken = localStorage.getItem("refreshToken");

    if (!token || !refreshToken) {
      return;
    }

    // Check if token is expired or expiring soon
    if (isTokenExpiredOrExpiringSoon(token)) {
      try {
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}/api/auth/refresh`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({ refreshToken }),
          }
        );

        if (response.ok) {
          const data = await response.json();
          if (data.accessToken) {
            apiClient.setToken(data.accessToken);
            if (data.refreshToken) {
              apiClient.setRefreshToken(data.refreshToken);
            }
            console.log("Token refreshed successfully");
          }
        } else {
          // Refresh failed, clear tokens
          apiClient.clearTokens();
          if (window.location.pathname !== "/login") {
            window.location.href = "/login";
          }
        }
      } catch (error) {
        console.error("Error refreshing token:", error);
        apiClient.clearTokens();
      }
    }
  };

  // Check token every 2 minutes
  if (typeof window !== "undefined") {
    intervalId = setInterval(checkAndRefreshToken, 2 * 60 * 1000);
    // Also check immediately
    checkAndRefreshToken();
  }

  // Return cleanup function
  return () => {
    if (intervalId) {
      clearInterval(intervalId);
    }
  };
}

