import { apiRequest } from './client';
import type { Category } from './types';

export const categoriesApi = {
  getAll: (signal?: AbortSignal) => apiRequest<Category[]>('/category', { signal }),
};
