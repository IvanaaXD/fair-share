import { apiRequest, toQueryString } from './client';
import type { Budget, CreateBudgetRequest, UpdateBudgetRequest } from './types';

/** BudgetController - one budget per category and month ("YYYY-MM"). */
export const budgetsApi = {
  mine: (month: string, signal?: AbortSignal) =>
    apiRequest<Budget[]>(`/budget/mine${toQueryString({ month })}`, { signal }),

  create: (request: CreateBudgetRequest) => apiRequest<Budget>('/budget', { method: 'POST', body: request }),

  update: (budgetId: string, request: UpdateBudgetRequest) =>
    apiRequest<Budget>(`/budget/${budgetId}`, { method: 'PUT', body: request }),

  remove: (budgetId: string) => apiRequest<void>(`/budget/${budgetId}`, { method: 'DELETE' }),
};
