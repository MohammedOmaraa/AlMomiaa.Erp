import type { AxiosInstance } from "axios";
import { createIdempotencyKey, type PagedResult } from "../common/types.js";

export interface FinancialAccountDto {
  id: string;
  accountCode: string;
  nameAr: string;
  nameEn: string | null;
  accountType: string;
  ownerRepId: string | null;
  bankName: string | null;
  iban: string | null;
  currentBalance: number;
  isActive: boolean;
}

export interface FiscalPeriodDto {
  id: number;
  fiscalYear: number;
  fiscalMonth: number;
  periodStartUtc: string;
  periodEndUtc: string;
  isClosed: boolean;
  closedByUserId: string | null;
  closedAtUtc: string | null;
}

export interface CollectionAllocationDto {
  id: string;
  invoiceId: string;
  allocatedAmount: number;
}

export interface CollectionDto {
  id: string;
  receiptNumber: string;
  customerId: string;
  repId: string;
  paymentMethod: string;
  amount: number;
  status: string;
  isCustomerConfirmed: boolean;
  customerConfirmedAtUtc: string | null;
  paymentProofImageUrl: string | null;
  paymentProofImageHash: string | null;
  bankReferenceNumber: string | null;
  targetFinancialAccountId: string | null;
  verifiedByAccountantId: string | null;
  verifiedAtUtc: string | null;
  originalValueDateUtc: string;
  createdAtUtc: string;
  allocations: CollectionAllocationDto[];
}

export interface JournalLineDto {
  id: number;
  financialAccountId: string;
  customerId: string | null;
  supplierId: string | null;
  debitAmount: number;
  creditAmount: number;
}

export interface JournalEntryDto {
  id: string;
  entryNumber: string;
  fiscalPeriodId: number;
  postingDateUtc: string;
  originalValueDateUtc: string;
  referenceType: string;
  referenceId: string;
  descriptionAr: string;
  createdByUserId: string;
  lines: JournalLineDto[];
}

export function createFinanceService(http: AxiosInstance) {
  return {
    getAccounts: async () =>
      (await http.get<FinancialAccountDto[]>("/finance/accounts")).data,
    createAccount: async (request: {
      accountCode: string;
      nameAr: string;
      nameEn?: string | null;
      accountType: string;
      ownerRepId?: string | null;
      bankName?: string | null;
      iban?: string | null;
    }) => (await http.post<FinancialAccountDto>("/finance/accounts", request)).data,
    getFiscalPeriods: async () =>
      (await http.get<FiscalPeriodDto[]>("/finance/fiscal-periods")).data,
    createFiscalPeriod: async (request: { fiscalYear: number; fiscalMonth: number }) =>
      (await http.post<FiscalPeriodDto>("/finance/fiscal-periods", request)).data,
    closeFiscalPeriod: async (fiscalPeriodId: number) =>
      (await http.post<FiscalPeriodDto>(`/finance/fiscal-periods/${fiscalPeriodId}/close`)).data,
    getJournalEntries: async (params?: { page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<JournalEntryDto>>("/finance/journal-entries", { params })).data,
    recordCashCollection: async (
      request: { customerId: string; amount: number; allocations?: Array<{ invoiceId: string; allocatedAmount: number }> | null },
      idempotencyKey?: string
    ) =>
      (
        await http.post<CollectionDto>("/collections/cash", request, {
          headers: { "X-Idempotency-Key": idempotencyKey ?? createIdempotencyKey() },
        })
      ).data,
    confirmCashCollection: async (collectionId: string) =>
      (await http.post<CollectionDto>(`/collections/${collectionId}/confirm`)).data,
    recordTreasuryHandover: async (request: { collectionId: string; targetTreasuryAccountId: string }) =>
      (await http.post<CollectionDto>("/collections/handover", request)).data,
    recordBankTransfer: async (
      request: {
        customerId: string;
        amount: number;
        bankReferenceNumber: string;
        paymentProofImageUrl: string;
        paymentProofImageHash: string;
        originalValueDateUtc?: string | null;
        allocations?: Array<{ invoiceId: string; allocatedAmount: number }> | null;
      },
      idempotencyKey?: string
    ) =>
      (
        await http.post<CollectionDto>("/collections/bank-transfer", request, {
          headers: { "X-Idempotency-Key": idempotencyKey ?? createIdempotencyKey() },
        })
      ).data,
    verifyBankTransfer: async (collectionId: string, request: { targetBankAccountId: string; approve: boolean }) =>
      (await http.post<CollectionDto>(`/collections/${collectionId}/verify-bank`, request)).data,
  };
}
