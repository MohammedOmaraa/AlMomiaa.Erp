import { AxiosError, type AxiosInstance } from "axios";
import type { AccessTokenStore } from "./access-token-store.js";
import {
  authenticationResponseSchema,
  changePasswordRequestSchema,
  loginRequestSchema,
  problemDetailsSchema,
} from "./auth.schemas.js";
import type {
  AuthenticationResponse,
  ChangePasswordRequest,
  LoginRequest,
  ProblemDetails,
} from "./auth.types.js";

export interface AuthService {
  login: (request: LoginRequest, signal?: AbortSignal) => Promise<AuthenticationResponse>;
  refresh: (signal?: AbortSignal) => Promise<AuthenticationResponse>;
  logout: (signal?: AbortSignal) => Promise<void>;
  changePassword: (request: ChangePasswordRequest, signal?: AbortSignal) => Promise<void>;
}

export function extractProblemDetails(error: unknown): ProblemDetails | null {
  if (!(error instanceof AxiosError)) {
    return null;
  }

  const parsed = problemDetailsSchema.safeParse(error.response?.data);
  if (parsed.success) {
    return {
      ...parsed.data,
      status: parsed.data.status ?? error.response?.status,
    };
  }

  if (error.response?.status) {
    return { status: error.response.status };
  }

  return null;
}

export function createAuthService(
  http: AxiosInstance,
  tokenStore: AccessTokenStore,
  refreshSingleFlight: (signal?: AbortSignal) => Promise<AuthenticationResponse>
): AuthService {
  return {
    async login(request: LoginRequest, signal?: AbortSignal): Promise<AuthenticationResponse> {
      const validated = loginRequestSchema.parse(request);
      const response = await http.post<unknown>("/auth/login", validated, {
        withCredentials: true,
        signal,
      });
      const parsed = authenticationResponseSchema.parse(response.data);
      tokenStore.setAccessToken(parsed.accessToken);
      return parsed;
    },

    async refresh(signal?: AbortSignal): Promise<AuthenticationResponse> {
      return refreshSingleFlight(signal);
    },

    async logout(signal?: AbortSignal): Promise<void> {
      try {
        await http.post("/auth/logout", undefined, {
          withCredentials: true,
          signal,
        });
      } finally {
        tokenStore.clearAccessToken();
      }
    },

    async changePassword(request: ChangePasswordRequest, signal?: AbortSignal): Promise<void> {
      const validated = changePasswordRequestSchema.parse(request);
      await http.post("/auth/change-password", validated, {
        withCredentials: true,
        signal,
      });
      tokenStore.clearAccessToken();
    },
  };
}
