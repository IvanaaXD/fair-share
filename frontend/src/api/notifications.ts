import { apiRequest, toQueryString } from './client';
import type { AppNotification } from './types';

/** NotificationController. */
export const notificationsApi = {
  /** isRead=false returns only the unread ones. */
  mine: (isRead?: boolean, signal?: AbortSignal) =>
    apiRequest<AppNotification[]>(`/notifications/mine${toQueryString({ isRead })}`, { signal }),

  unreadCount: (signal?: AbortSignal) => apiRequest<{ count: number }>('/notifications/unread-count', { signal }),

  markRead: (id: string) => apiRequest<void>(`/notifications/${id}/read`, { method: 'POST' }),

  markAllRead: () => apiRequest<void>('/notifications/read-all', { method: 'POST' }),
};
