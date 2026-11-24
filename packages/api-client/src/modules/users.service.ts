import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface UserSummaryDto {
  id: string;
  employeeCode: string;
  fullNameAr: string;
  fullNameEn: string | null;
  phone: string;
  email: string | null;
  repType: string | null;
  defaultWarehouseId: number | null;
  isActive: boolean;
  createdAtUtc: string;
  roleCodes: string[];
  assignedWarehouseIds: number[];
}

export interface UserDetailDto extends UserSummaryDto {
  effectivePermissions: string[];
}

export interface RoleDto {
  id: number;
  roleCode: string;
  nameAr: string;
  nameEn: string;
  permissions: string[];
}

export interface CreateUserRequest {
  employeeCode: string;
  fullNameAr: string;
  fullNameEn?: string | null;
  phone: string;
  email?: string | null;
  password: string;
  repType?: string | null;
  defaultWarehouseId?: number | null;
  roleIds: number[];
  assignedWarehouseIds?: number[];
  createMobileVan?: boolean;
  vehiclePlateNumber?: string | null;
}

export interface CreateRoleRequest {
  roleCode: string;
  nameAr: string;
  nameEn: string;
  permissions: string[];
}

export function createUsersService(http: AxiosInstance) {
  return {
    getUsers: async (params?: { search?: string; roleCode?: string; isActive?: boolean; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<UserSummaryDto>>("/users", { params })).data,
    getUserById: async (id: string) =>
      (await http.get<UserDetailDto>(`/users/${id}`)).data,
    createUser: async (request: CreateUserRequest) =>
      (await http.post<UserSummaryDto>("/users", request)).data,
    updateUserStatus: async (id: string, isActive: boolean) =>
      (await http.patch<UserSummaryDto>(`/users/${id}/status`, { isActive })).data,
    updateUserRoles: async (id: string, roleIds: number[]) =>
      (await http.put<UserDetailDto>(`/users/${id}/roles`, { roleIds })).data,
    updateUserWarehouses: async (id: string, defaultWarehouseId: number | null, assignedWarehouseIds: number[]) =>
      (await http.put<UserDetailDto>(`/users/${id}/warehouses`, { defaultWarehouseId, assignedWarehouseIds })).data,
    getRoles: async () =>
      (await http.get<RoleDto[]>("/roles")).data,
    createRole: async (request: CreateRoleRequest) =>
      (await http.post<RoleDto>("/roles", request)).data,
    updateRolePermissions: async (roleId: number, permissions: string[]) =>
      (await http.put<RoleDto>(`/roles/${roleId}/permissions`, { permissions })).data,
  };
}
