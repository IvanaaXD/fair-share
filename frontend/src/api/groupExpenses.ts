import { apiRequest, toQueryString } from './client';
import type { GroupExpense, ImageUrlResponse, SaveGroupExpenseRequest, Settlement, SettlementStatus } from './types';

const base = (groupId: string) => `/groups/${groupId}/expenses`;

/** GroupExpenseController and GroupExpenseReceiptController. */
export const groupExpensesApi = {
  /** Newest first. The backend returns a plain list, so "is there more" = the page came back full. */
  list: (groupId: string, page: number, pageSize: number, signal?: AbortSignal) =>
    apiRequest<GroupExpense[]>(`${base(groupId)}${toQueryString({ page, pageSize })}`, { signal }),

  create: (groupId: string, request: SaveGroupExpenseRequest) =>
    apiRequest<GroupExpense>(base(groupId), { method: 'POST', body: request }),

  update: (groupId: string, expenseId: string, request: SaveGroupExpenseRequest) =>
    apiRequest<GroupExpense>(`${base(groupId)}/${expenseId}`, { method: 'PUT', body: request }),

  remove: (groupId: string, expenseId: string) =>
    apiRequest<void>(`${base(groupId)}/${expenseId}`, { method: 'DELETE' }),

  uploadReceipt: (groupId: string, expenseId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiRequest<ImageUrlResponse>(`${base(groupId)}/${expenseId}/receipt`, { method: 'PUT', body: form });
  },

  removeReceipt: (groupId: string, expenseId: string) =>
    apiRequest<void>(`${base(groupId)}/${expenseId}/receipt`, { method: 'DELETE' }),
};

/** SettlementController (balances are in groupsApi). */
export const settlementsApi = {
  list: (groupId: string, status?: SettlementStatus, signal?: AbortSignal) =>
    apiRequest<Settlement[]>(`/groups/${groupId}/settlements${toQueryString({ status })}`, { signal }),

  /** Replaces the earlier unsettled suggestions. useExact: exact algorithm instead of the greedy one. */
  generate: (groupId: string, useExact: boolean) =>
    apiRequest<Settlement[]>(`/groups/${groupId}/settlements/generate${toQueryString({ useExact })}`, { method: 'POST' }),

  settle: (groupId: string, settlementId: string) =>
    apiRequest<Settlement>(`/groups/${groupId}/settlements/${settlementId}/settle`, { method: 'POST' }),

  /** Only the creditor; at most once every 24 hours per settlement. */
  remind: (groupId: string, settlementId: string, message: string | null) =>
    apiRequest<Settlement>(`/groups/${groupId}/settlements/${settlementId}/remind`, { method: 'POST', body: { message } }),
};
