import { useEffect, useSyncExternalStore } from 'react';
import { notificationsApi } from '../api/notifications';
import { useAuth } from '../auth/useAuth';

/**
 * Number of unread notifications for the badge in the sidebar. Checked when the app opens,
 * every minute while it is open (and visible), and right after the notifications page changes
 * something. Notifications are created on the server (a new expense in a group, a payment...);
 * while the SignalR connection is up (realtime.ts) the server pushes the new number and the
 * minute-by-minute check pauses - it only covers the time without a connection.
 */
const POLL_MS = 60_000;

let state: { userId: string | null; count: number } = { userId: null, count: 0 };

/** While the real-time connection is up, the server pushes every change and polling pauses. */
let liveUpdates = false;

export function setLiveUpdates(on: boolean) {
  liveUpdates = on;
}
const listeners = new Set<() => void>();

function set(next: typeof state) {
  if (next.userId !== state.userId || next.count !== state.count) {
    state = next;
    listeners.forEach((listener) => listener());
  }
}

export async function refreshUnreadCount(userId: string): Promise<void> {
  try {
    const { count } = await notificationsApi.unreadCount();
    set({ userId, count });
  } catch {
    // The badge is not important enough to show an error; the next check will try again.
  }
}

/** Lowers the badge immediately (e.g. after "mark all as read"), before the server confirms. */
export function setUnreadCount(userId: string, count: number) {
  set({ userId, count: Math.max(0, count) });
}

export function useUnreadCount(): number {
  const { user } = useAuth();
  const userId = user?.id ?? null;
  const snapshot = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => state,
  );

  useEffect(() => {
    if (!userId) return;
    void refreshUnreadCount(userId);
    const timer = window.setInterval(() => {
      if (!liveUpdates && document.visibilityState === 'visible') void refreshUnreadCount(userId);
    }, POLL_MS);
    const onVisible = () => {
      if (document.visibilityState === 'visible') void refreshUnreadCount(userId);
    };
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, [userId]);

  return snapshot.userId === userId ? snapshot.count : 0;
}
