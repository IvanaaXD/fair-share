import { apiRequest, toQueryString } from './client';
import type { AppNotification } from './types';

/** NotificationController. */
export const notificationsApi = {
  /** isRead=false returns only the unread ones. */
  mine: (isRead?: boolean, signal?: AbortSignal) =>
    apiRequest<AppNotification[]>(`/notification/mine${toQueryString({ isRead })}`, { signal }),

  unreadCount: (signal?: AbortSignal) => apiRequest<{ count: number }>('/notification/unread-count', { signal }),

  markRead: (id: string) => apiRequest<void>(`/notification/${id}/read`, { method: 'POST' }),

  markAllRead: () => apiRequest<void>('/notification/read-all', { method: 'POST' }),
};
