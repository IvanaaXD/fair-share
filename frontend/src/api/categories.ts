import { apiRequest, toQueryString } from './client';
import type { AdminCategory, Category, CategoryProposal, CategoryStatus, SaveCategoryRequest } from './types';

const BASE = '/category';

/** CategoryController. */
export const categoriesApi = {
  /** Approved categories - what every user chooses from. */
  getAll: (signal?: AbortSignal) => apiRequest<Category[]>(BASE, { signal }),

  /** A user's proposal waits for approval; an administrator's category is approved at once. */
  create: (request: SaveCategoryRequest) => apiRequest<CategoryProposal>(BASE, { method: 'POST', body: request }),

  myProposals: (signal?: AbortSignal) => apiRequest<CategoryProposal[]>(`${BASE}/proposals/mine`, { signal }),

  // ---------- administrator ----------

  adminList: (status: CategoryStatus | undefined, signal?: AbortSignal) =>
    apiRequest<AdminCategory[]>(`${BASE}/admin${toQueryString({ status })}`, { signal }),

  /** The administrator may correct the name and icon while approving. */
  approve: (categoryId: string, request: SaveCategoryRequest) =>
    apiRequest<AdminCategory>(`${BASE}/${categoryId}/approve`, { method: 'POST', body: request }),

  reject: (categoryId: string, reason: string) =>
    apiRequest<AdminCategory>(`${BASE}/${categoryId}/reject`, { method: 'POST', body: { reason } }),

  update: (categoryId: string, request: SaveCategoryRequest) =>
    apiRequest<AdminCategory>(`${BASE}/${categoryId}`, { method: 'PUT', body: request }),

  remove: (categoryId: string) => apiRequest<void>(`${BASE}/${categoryId}`, { method: 'DELETE' }),
};

const STATUSES: CategoryStatus[] = ['Approved', 'Pending', 'Rejected'];

/** The backend may send the status as a name or as a number. */
export function categoryStatusName(status: CategoryStatus | number): CategoryStatus {
  return typeof status === 'number' ? (STATUSES[status] ?? 'Approved') : status;
}
