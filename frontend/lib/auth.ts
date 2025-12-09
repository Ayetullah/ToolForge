import { usersApi } from "./api";

export interface UserRole {
  subscriptionTier: string;
  isAdmin: boolean;
}

/**
 * Decode JWT token to get user information
 * Note: This is a simple implementation. In production, you should verify the token signature.
 */
export function decodeJWT(token: string): any {
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
    return null;
  }
}

/**
 * Get user role from token
 */
export function getUserRoleFromToken(token: string | null): UserRole | null {
  if (!token) return null;

  const decoded = decodeJWT(token);
  if (!decoded) return null;

  // JWT token'da subscription_tier claim'i var
  const subscriptionTier = decoded.subscription_tier || 
                           decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || 
                           decoded.role || 
                           decoded.subscriptionTier || 
                           "Free";

  return {
    subscriptionTier: subscriptionTier,
    isAdmin: subscriptionTier === "Admin" || subscriptionTier === "admin",
  };
}

/**
 * Check if user has required role
 */
export function hasRole(
  token: string | null,
  requiredRole: string | string[]
): boolean {
  const userRole = getUserRoleFromToken(token);
  if (!userRole) return false;

  const roles = Array.isArray(requiredRole) ? requiredRole : [requiredRole];
  return roles.some(
    (role) =>
      userRole.subscriptionTier.toLowerCase() === role.toLowerCase() ||
      userRole.isAdmin
  );
}

/**
 * Get user profile from API (more reliable than JWT decode)
 */
export async function getUserProfile(): Promise<UserRole | null> {
  try {
    const response = await usersApi.getProfile();
    if (response.data) {
      return {
        subscriptionTier: response.data.subscriptionTier,
        isAdmin:
          response.data.subscriptionTier === "Admin" ||
          response.data.subscriptionTier === "admin",
      };
    }
    return null;
  } catch (error) {
    return null;
  }
}

