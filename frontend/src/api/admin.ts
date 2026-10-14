import { apiRequest, toQueryString } from './client';
import type { AdminStatistics, AdminUser, AdminUserQuery, AnalyticsQuery, AuditLogEntry, AuditLogQuery } from './types';

/** AdminController and AdminUserController - only for users with the Admin role. */
export const adminApi = {
  statistics: (query: Omit<AnalyticsQuery, 'currency'>, signal?: AbortSignal) =>
    apiRequest<AdminStatistics>(`/admin/statistics${toQueryString({ ...query })}`, { signal }),

  /** One page of users; the backend does not return a total, so "next" exists while a page is full. */
  users: (query: AdminUserQuery, signal?: AbortSignal) =>
    apiRequest<AdminUser[]>(`/admin/users${toQueryString({ ...query })}`, { signal }),

  block: (userId: string) => apiRequest<void>(`/admin/users/${userId}/block`, { method: 'POST' }),

  unblock: (userId: string) => apiRequest<void>(`/admin/users/${userId}/unblock`, { method: 'POST' }),

  /** Newest first, one page at a time. */
  auditLogs: (query: AuditLogQuery, signal?: AbortSignal) =>
    apiRequest<AuditLogEntry[]>(`/admin/audit-logs${toQueryString({ ...query })}`, { signal }),
};
