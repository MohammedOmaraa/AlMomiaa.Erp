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
    getGrades: async () =>
      (await http.get<CustomerGradeDto[]>("/customers/grades")).data,
    updateGrades: async (grades: UpdateCustomerGradeItem[]) =>
      (await http.put<CustomerGradeDto[]>("/customers/grades", { grades })).data,
  };
}
