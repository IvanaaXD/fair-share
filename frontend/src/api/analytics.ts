import { apiRequest, toQueryString } from './client';
import type { AnalyticsQuery, ExpenseLocation, SpendingAnalytics } from './types';

/** AnalyticsController - the signed-in user's personal expenses. */
export const analyticsApi = {
  spending: (query: AnalyticsQuery, signal?: AbortSignal) =>
    apiRequest<SpendingAnalytics>(`/analytics/spending${toQueryString({ ...query })}`, { signal }),

  /** Expenses with a location (at most the 500 newest). */
  map: (query: AnalyticsQuery, signal?: AbortSignal) =>
    apiRequest<ExpenseLocation[]>(`/analytics/map${toQueryString({ ...query })}`, { signal }),
};
