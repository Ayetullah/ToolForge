import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// Protected routes that require authentication
// Note: /tools routes are now free (no authentication required)
// Individual tool pages can be accessed without login
const protectedRoutes = ["/dashboard", "/admin"];
// Admin-only routes
const adminRoutes = ["/admin"];

/**
 * Simple JWT decode (without verification - for middleware only)
 * In production, you should verify the token signature
 */
function decodeJWT(token: string): any {
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

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Check if route is protected
  const isProtectedRoute = protectedRoutes.some((route) =>
    pathname.startsWith(route)
  );
  const isAdminRoute = adminRoutes.some((route) =>
    pathname.startsWith(route)
  );

  // Get token from cookie, header, or localStorage (via request)
  // Note: Middleware runs on server, so we can't access localStorage directly
  // Token should be in Authorization header or cookie
  const authHeader = request.headers.get("authorization");
  const token = request.cookies.get("token")?.value || 
                (authHeader?.startsWith("Bearer ") ? authHeader.replace("Bearer ", "") : null);

  // If no token and trying to access protected route, redirect to login
  if (isProtectedRoute && !token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // For admin routes, check if user is admin
  if (isAdminRoute && token) {
    const decoded = decodeJWT(token);
    if (decoded) {
      const subscriptionTier = decoded.subscription_tier || 
                               decoded.subscriptionTier || 
                               "Free";
      const isAdmin = subscriptionTier === "Admin" || subscriptionTier === "admin";
      
      if (!isAdmin) {
        // Not admin, redirect to user dashboard
        return NextResponse.redirect(new URL("/dashboard", request.url));
      }
    } else {
      // Invalid token, redirect to login
      const loginUrl = new URL("/login", request.url);
      loginUrl.searchParams.set("redirect", pathname);
      return NextResponse.redirect(loginUrl);
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public files (public folder)
     */
    "/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};

