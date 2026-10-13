import { apiPath, apiRequest, toQueryString } from './client';
import type { Expense, ExpenseQuery, ImageUrlResponse, PagedResponse, SaveExpenseRequest } from './types';

/** One function per endpoint of ExpenseController and ExpenseReceiptController. */
export const expensesApi = {
  list: (query: ExpenseQuery, signal?: AbortSignal) =>
    apiRequest<PagedResponse<Expense>>(`/expenses${toQueryString({ ...query })}`, { signal }),

  get: (id: string, signal?: AbortSignal) => apiRequest<Expense>(`/expenses/${id}`, { signal }),

  create: (request: SaveExpenseRequest) => apiRequest<Expense>('/expenses', { method: 'POST', body: request }),

  update: (id: string, request: SaveExpenseRequest) =>
    apiRequest<Expense>(`/expenses/${id}`, { method: 'PUT', body: request }),

  remove: (id: string) => apiRequest<void>(`/expenses/${id}`, { method: 'DELETE' }),

  /** multipart/form-data with the field "file" (JPEG, PNG or WebP, at most 5 MB). */
  uploadReceipt: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiRequest<ImageUrlResponse>(`/expenses/${id}/receipt`, { method: 'PUT', body: form });
  },

  removeReceipt: (id: string) => apiRequest<void>(`/expenses/${id}/receipt`, { method: 'DELETE' }),
};

/**
 * Any protected image (receipt, profile picture) as a Blob. An <img src> cannot send the
 * access token, so protected images are downloaded with it and shown from an object URL.
 */
export function fetchProtectedImage(url: string, signal?: AbortSignal) {
  return apiRequest<Blob>(apiPath(url), { signal, responseType: 'blob' });
}
