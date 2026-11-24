import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface CustomerGradeDto {
  id: number;
  gradeCode: string;
  defaultPaymentDays: number;
  allowCredit: boolean;
  updatedAtUtc: string;
}

export interface CustomerDto {
  id: string;
  customerCode: string;
  nameAr: string;
  nameEn: string | null;
  phone: string;
  address: string;
  city: string;
  category: string;
  vatNumber: string | null;
  gradeId: number;
  gradeCode: string;
  priceListId: number | null;
  customPaymentDays: number | null;
  creditLimit: number;
  currentBalance: number;
  assignedRepId: string;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
  assignedRepCode?: string | null;
  assignedRepNameAr?: string | null;
  assignedRepNameEn?: string | null;
}

export interface SalesRepOptionDto {
  id: string;
  employeeCode: string;
  fullNameAr: string;
  fullNameEn: string | null;
  phone: string;
  repType: string | null;
  assignedCustomersCount: number;
}

export interface BulkAssignCustomersRepResponse {
  assignedRepId: string;
  assignedRepCode: string;
  assignedRepNameAr: string;
  updatedCount: number;
  customerIds: string[];
}

export interface RecentCustomerOrderDto {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
}

export interface CustomerCreditStatusDto {
  customerId: string;
  customerCode: string;
  nameAr: string;
  gradeCode: string;
  allowCredit: boolean;
  effectivePaymentDays: number;
  priceListId: number | null;
  creditLimit: number;
  currentBalance: number;
  overdueDebt: number;
  recentOrders: RecentCustomerOrderDto[];
}

export interface CreateCustomerRequest {
  customerCode: string;
  nameAr: string;
  nameEn?: string | null;
  phone: string;
  address: string;
  city?: string | null;
  category: string;
  vatNumber?: string | null;
  gradeId: number;
  priceListId?: number | null;
  customPaymentDays?: number | null;
  creditLimit: number;
  assignedRepId: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface UpdateCustomerGradeItem {
  gradeCode: string;
  defaultPaymentDays: number;
  allowCredit: boolean;
}

export function createCustomersService(http: AxiosInstance) {
  return {
    getCustomers: async (params?: { search?: string; gradeId?: number; city?: string; isActive?: boolean; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<CustomerDto>>("/customers", { params })).data,
    getCreditStatus: async (id: string) =>
      (await http.get<CustomerCreditStatusDto>(`/customers/${id}/credit-status`)).data,
    createCustomer: async (request: CreateCustomerRequest) =>
      (await http.post<CustomerDto>("/customers", request)).data,
    getSalesReps: async () =>
      (await http.get<SalesRepOptionDto[]>("/customers/sales-reps")).data,
    assignCustomerRep: async (customerId: string, assignedRepId: string) =>
      (await http.put<CustomerDto>(`/customers/${customerId}/assign-rep`, { assignedRepId })).data,
    bulkAssignCustomersRep: async (request: { assignedRepId: string; customerIds: string[] }) =>
      (await http.post<BulkAssignCustomersRepResponse>("/customers/assign-rep/bulk", request)).data,
    getGrades: async () =>
      (await http.get<CustomerGradeDto[]>("/customers/grades")).data,
    updateGrades: async (grades: UpdateCustomerGradeItem[]) =>
      (await http.put<CustomerGradeDto[]>("/customers/grades", { grades })).data,
  };
}
