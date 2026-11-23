export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface AuthenticatedUser {
  id: string;
  employeeCode: string;
  fullNameAr: string;
  fullNameEn: string | null;
  roleCodes: string[];
  permissions: string[];
  repType: string | null;
  defaultWarehouseId: number | null;
  assignedWarehouseIds: number[];
}

export interface AuthenticationResponse {
  accessToken: string;
  tokenType: "Bearer";
  expiresIn: number;
  user: AuthenticatedUser;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
