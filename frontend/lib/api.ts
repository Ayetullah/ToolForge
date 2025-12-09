const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  message?: string;
}

// ✅ Auth API Response Types
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  subscriptionTier: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  message: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface ForgotPasswordResponse {
  success: boolean;
  message: string;
}

export interface ResetPasswordResponse {
  success: boolean;
  message: string;
}

export interface VerifyEmailResponse {
  success: boolean;
  message: string;
}

export interface ResendVerificationResponse {
  success: boolean;
  message: string;
}

// ✅ User API Response Types
export interface UserProfile {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  subscriptionTier: string;
  subscriptionExpiresAt?: string;
  isEmailVerified: boolean;
  totalUsageCount: number;
  totalFileSizeProcessed: number;
  createdAt: string;
}

export interface UsageStatistics {
  totalOperations: number;
  operationsByTool: Record<string, number>;
  operationsByDate: Array<{
    date: string;
    count: number;
  }>;
  totalFileSizeProcessed: number;
  periodStart?: string;
  periodEnd?: string;
}

// ✅ Admin API Response Types
export interface SystemUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  subscriptionTier: string;
  isEmailVerified: boolean;
  totalUsageCount: number;
  createdAt: string;
}

export interface GetAllUsersResponse {
  users: SystemUser[];
  totalPages: number;
  currentPage: number;
  pageSize: number;
  totalCount: number;
}

export interface SystemStats {
  totalUsers: number;
  activeUsers: number;
  totalOperations: number;
  totalFileSizeProcessed: number;
}

class ApiClient {
  private baseUrl: string;
  private token: string | null = null;
  private refreshToken: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
    if (typeof window !== "undefined") {
      // Check both localStorage and sessionStorage for tokens
      this.token = localStorage.getItem("token") || sessionStorage.getItem("token");
      this.refreshToken = localStorage.getItem("refreshToken") || sessionStorage.getItem("refreshToken");
    }
  }

  setToken(token: string | null, rememberMe: boolean = true) {
    this.token = token;
    if (typeof window !== "undefined") {
      if (token) {
        // Use localStorage for rememberMe=true, sessionStorage for rememberMe=false
        if (rememberMe) {
          localStorage.setItem("token", token);
        } else {
          sessionStorage.setItem("token", token);
        }
        // ✅ Also set cookie for Next.js middleware (server-side access)
        const maxAge = rememberMe ? 60 * 60 * 24 * 30 : 60 * 60 * 24 * 7; // 30 days or 7 days
        document.cookie = `token=${token}; path=/; max-age=${maxAge}; SameSite=Lax`;
      } else {
        localStorage.removeItem("token");
        sessionStorage.removeItem("token");
        // ✅ Remove cookie
        document.cookie = "token=; path=/; max-age=0";
      }
    }
  }

  setRefreshToken(refreshToken: string | null, rememberMe: boolean = true) {
    this.refreshToken = refreshToken;
    if (typeof window !== "undefined") {
      if (refreshToken) {
        // Use localStorage for rememberMe=true, sessionStorage for rememberMe=false
        if (rememberMe) {
          localStorage.setItem("refreshToken", refreshToken);
        } else {
          sessionStorage.setItem("refreshToken", refreshToken);
        }
      } else {
        localStorage.removeItem("refreshToken");
        sessionStorage.removeItem("refreshToken");
      }
    }
  }

  clearTokens() {
    this.token = null;
    this.refreshToken = null;
    if (typeof window !== "undefined") {
      localStorage.removeItem("token");
      localStorage.removeItem("refreshToken");
      sessionStorage.removeItem("token");
      sessionStorage.removeItem("refreshToken");
      // ✅ Clear cookies
      document.cookie = "token=; path=/; max-age=0";
      document.cookie = "refreshToken=; path=/; max-age=0";
    }
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: HeadersInit = {
      "Content-Type": "application/json",
      ...options.headers,
    };

    if (this.token) {
      headers["Authorization"] = `Bearer ${this.token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });

      // Check if response is JSON
      const contentType = response.headers.get("content-type");
      const isJson = contentType && contentType.includes("application/json");

      if (!isJson) {
        const text = await response.text();
        return {
          error: response.ok
            ? "Invalid response format"
            : `Server error: ${response.status} ${response.statusText}`,
        };
      }

      let data;
      try {
        data = await response.json();
      } catch (jsonError) {
        return {
          error: "Invalid JSON response from server",
        };
      }

      // ✅ Handle 401 Unauthorized - try to refresh token
      if (response.status === 401 && this.refreshToken) {
        const refreshed = await this.refreshAccessToken();
        if (refreshed) {
          // Retry the original request with new token
          return this.request<T>(endpoint, options);
        }
        // Refresh failed, clear tokens and return error
        this.clearTokens();
        return {
          error: "Session expired. Please login again.",
        };
      }

      if (!response.ok) {
        return {
          error:
            data.error ||
            data.message ||
            data.title ||
            `Error ${response.status}: ${response.statusText}`,
        };
      }

      return { data };
    } catch (error) {
      return {
        error: error instanceof Error ? error.message : "Network error",
      };
    }
  }

  // ✅ Refresh access token using refresh token
  private async refreshAccessToken(): Promise<boolean> {
    if (!this.refreshToken) {
      return false;
    }

    try {
      const response = await fetch(`${this.baseUrl}/api/auth/refresh`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ refreshToken: this.refreshToken }),
      });

      if (!response.ok) {
        return false;
      }

      const data = await response.json();
      if (data.accessToken) {
        this.setToken(data.accessToken);
        if (data.refreshToken) {
          this.setRefreshToken(data.refreshToken);
        }
        return true;
      }

      return false;
    } catch (error) {
      // Error refreshing token
      return false;
    }
  }

  async get<T>(endpoint: string): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { method: "GET" });
  }

  async post<T>(endpoint: string, body?: any): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      method: "POST",
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  async postFormData<T>(
    endpoint: string,
    formData: FormData
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: HeadersInit = {};

    if (this.token) {
      headers["Authorization"] = `Bearer ${this.token}`;
    }

    // Don't set Content-Type for FormData - browser will set it automatically with boundary

    try {
      const response = await fetch(url, {
        method: "POST",
        headers,
        body: formData,
        credentials: "include", // Include cookies for CORS
      });

      // Check if response is JSON
      const contentType = response.headers.get("content-type");
      const isJson = contentType && contentType.includes("application/json");

      if (!isJson) {
        const text = await response.text();
        return {
          error: response.ok
            ? "Invalid response format"
            : `Server error: ${response.status} ${response.statusText}`,
        };
      }

      let data;
      try {
        data = await response.json();
      } catch (jsonError) {
        return {
          error: "Invalid JSON response from server",
        };
      }

      if (!response.ok) {
        // Build detailed error message
        let errorMessage =
          data.error ||
          data.message ||
          data.title ||
          `Error ${response.status}: ${response.statusText}`;

        // Add validation errors if present
        if (data.errors && typeof data.errors === "object") {
          const validationErrors = Object.entries(data.errors)
            .map(
              ([key, value]) =>
                `${key}: ${Array.isArray(value) ? value.join(", ") : value}`
            )
            .join("; ");
          if (validationErrors) {
            errorMessage += ` (${validationErrors})`;
          }
        }

        return {
          error: errorMessage,
        };
      }

      return { data };
    } catch (error) {
      // Network error occurred

      return {
        error:
          error instanceof Error
            ? error.message
            : "Network error occurred. Please check your connection and try again.",
      };
    }
  }
}

export const apiClient = new ApiClient(API_BASE_URL);

// Auth API
export const authApi = {
  register: (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => apiClient.post<RegisterResponse>("/api/auth/register", data),

  login: (data: { email: string; password: string; rememberMe?: boolean }) =>
    apiClient.post<LoginResponse>("/api/auth/login", data),

  logout: () => {
    // ✅ Clear tokens immediately on logout
    apiClient.clearTokens();
    return apiClient.post("/api/auth/logout", {});
  },

  refresh: (data: { refreshToken: string }) =>
    apiClient.post<RefreshTokenResponse>("/api/auth/refresh", data),

  forgotPassword: (email: string) =>
    apiClient.post<ForgotPasswordResponse>("/api/auth/forgot-password", {
      email,
    }),

  resetPassword: (data: {
    email: string;
    token: string;
    newPassword: string;
  }) => apiClient.post<ResetPasswordResponse>("/api/auth/reset-password", data),

  verifyEmail: (data: { email: string; token: string }) =>
    apiClient.post<VerifyEmailResponse>("/api/auth/verify-email", data),

  resendVerification: (email: string) =>
    apiClient.post<ResendVerificationResponse>(
      "/api/auth/resend-verification",
      { email }
    ),
};

// Tools API
export const toolsApi = {
  mergePdf: (files: File[]) => {
    const formData = new FormData();
    files.forEach((file) => formData.append("files", file));
    return apiClient.postFormData("/api/tools/pdf/merge", formData);
  },

  splitPdf: (file: File, pagesSpec: string) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("pagesSpec", pagesSpec);
    return apiClient.postFormData("/api/tools/pdf/split", formData);
  },

  compressImage: (
    file: File,
    options: {
      quality?: number;
      targetFormat?: string;
      maxWidth?: number;
      maxHeight?: number;
    }
  ) => {
    const formData = new FormData();
    formData.append("file", file);
    if (options.quality) formData.append("quality", options.quality.toString());
    if (options.targetFormat)
      formData.append("targetFormat", options.targetFormat);
    if (options.maxWidth)
      formData.append("maxWidth", options.maxWidth.toString());
    if (options.maxHeight)
      formData.append("maxHeight", options.maxHeight.toString());
    return apiClient.postFormData("/api/tools/image/compress", formData);
  },

  formatJson: (json: string, indent?: number) =>
    apiClient.post("/api/tools/json/format", {
      json,
      indent: indent !== undefined && indent > 0,
      indentSize: indent ?? 2,
    }),

  summarize: (data: { text?: string; url?: string; maxLength?: number }) =>
    apiClient.post("/api/tools/ai/summarize", data),

  generateRegex: (description: string, examples?: string[]) =>
    apiClient.post("/api/tools/regex/generate", { description, examples }),

  cleanExcel: (file: File, options: any) => {
    const formData = new FormData();
    formData.append("file", file);
    Object.keys(options).forEach((key) => {
      const value = options[key];
      // Convert boolean to string for form data
      if (typeof value === "boolean") {
        formData.append(key, value.toString());
      } else if (value !== null && value !== undefined) {
        formData.append(key, value);
      }
    });
    return apiClient.postFormData("/api/tools/excel/clean", formData);
  },

  compressVideo: (file: File, options: any) => {
    const formData = new FormData();
    formData.append("file", file);
    Object.keys(options).forEach((key) => {
      if (options[key] !== null && options[key] !== undefined) {
        formData.append(key, options[key].toString());
      }
    });
    return apiClient.postFormData("/api/tools/video/compress", formData);
  },

  convertDocToPdf: (file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return apiClient.postFormData("/api/tools/convert/doc-to-pdf", formData);
  },

  removeBackground: (
    file: File,
    options: { transparent?: boolean; backgroundColor?: string }
  ) => {
    const formData = new FormData();
    formData.append("file", file);
    if (options.transparent !== undefined)
      formData.append("transparent", options.transparent.toString());
    if (options.backgroundColor)
      formData.append("backgroundColor", options.backgroundColor);
    return apiClient.postFormData(
      "/api/tools/image/remove-background",
      formData
    );
  },

  getJobStatus: (jobId: string) => apiClient.get(`/api/jobs/${jobId}/status`),
};

// Payment API
export const paymentApi = {
  subscribe: (tier: string) =>
    apiClient.post("/api/payments/subscribe", { tier }),

  cancel: () => apiClient.post("/api/payments/cancel", {}),
};

// Users API
export const usersApi = {
  getProfile: () => apiClient.get<UserProfile>("/api/users/profile"),

  getUsageStatistics: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    return apiClient.get<UsageStatistics>(
      `/api/users/usage-statistics?${params.toString()}`
    );
  },
};

// Admin API
export const adminApi = {
  getAllUsers: (
    page = 1,
    pageSize = 20,
    searchTerm?: string,
    subscriptionTier?: string
  ) => {
    const params = new URLSearchParams();
    params.append("page", page.toString());
    params.append("pageSize", pageSize.toString());
    if (searchTerm) params.append("searchTerm", searchTerm);
    if (subscriptionTier) params.append("subscriptionTier", subscriptionTier);
    return apiClient.get<GetAllUsersResponse>(
      `/api/admin/users?${params.toString()}`
    );
  },

  getSystemStats: () => apiClient.get<SystemStats>("/api/admin/stats"),
};
