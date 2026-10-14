import type { HubConnection } from '@microsoft/signalr';
import { refreshSession } from '../api/client';
import { tokenStore } from '../api/tokenStore';
import type { AppNotification } from '../api/types';
import { refreshUnreadCount, setLiveUpdates, setUnreadCount } from './unreadStore';

/**
 * Real-time notifications over SignalR (NotificationHub on the backend, /api/hubs/notifications).
 * One connection per signed-in user, shared by the whole app. When the backend creates a
 * notification it is pushed here at once: the badge changes, a toast appears and an open
 * notifications page adds it to the list.
 *
 * If the connection cannot be made (backend without the hub, a proxy that blocks WebSockets,
 * no network), nothing breaks: the badge keeps being checked every minute as before.
 */
export const HUB_PATH = '/api/hubs/notifications';

type Listener = (notification: AppNotification) => void;
type CountListener = (unreadCount: number) => void;

const listeners = new Set<Listener>();
const countListeners = new Set<CountListener>();
let connection: HubConnection | null = null;
let connectedUserId: string | null = null;
let retryTimer: number | undefined;
let generation = 0;

/** Called for every notification pushed by the server. Returns the "unsubscribe" function. */
export function onNotificationCreated(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** Called when the server reports a new unread count (e.g. notifications read in another tab). */
export function onUnreadCountChanged(listener: CountListener): () => void {
  countListeners.add(listener);
  return () => countListeners.delete(listener);
}

/** Seconds until the access token expires (0 if unknown or already expired). */
function secondsLeft(token: string): number {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(payload)) as { exp?: number };
    return exp ? exp - Date.now() / 1000 : 0;
  } catch {
    return 0;
  }
}

/** The token for (re)connecting; renewed first if it is about to expire. */
async function accessToken(): Promise<string> {
  const token = tokenStore.getAccessToken();
  if (!token || secondsLeft(token) < 30) {
    await refreshSession();
  }
  return tokenStore.getAccessToken() ?? '';
}

export async function startRealtime(userId: string): Promise<void> {
  if (connectedUserId === userId && connection) return;
  await stopRealtime();

  const current = ++generation;
  connectedUserId = userId;

  // Loaded only when needed, so the library is not part of the first page load.
  const signalR = await import('@microsoft/signalr');
  if (current !== generation) return;

  const hub = new signalR.HubConnectionBuilder()
    .withUrl(HUB_PATH, { accessTokenFactory: accessToken })
    .withAutomaticReconnect([0, 2_000, 10_000, 30_000, 60_000])
    .configureLogging(signalR.LogLevel.None)
    .build();

  hub.on('notificationCreated', (notification: AppNotification, unreadCount: number) => {
    setUnreadCount(userId, unreadCount);
    listeners.forEach((listener) => listener(notification));
  });
  hub.on('unreadCountChanged', (unreadCount: number) => {
    setUnreadCount(userId, unreadCount);
    countListeners.forEach((listener) => listener(unreadCount));
  });

  hub.onreconnecting(() => setLiveUpdates(false));
  hub.onreconnected(() => {
    setLiveUpdates(true);
    // Something may have arrived while the connection was down.
    void refreshUnreadCount(userId);
  });
  // Automatic reconnecting gave up: try again later, the minute-by-minute check covers the gap.
  hub.onclose(() => {
    setLiveUpdates(false);
    if (current === generation) scheduleRetry(userId);
  });

  connection = hub;
  try {
    await hub.start();
    if (current !== generation) return;
    setLiveUpdates(true);
    void refreshUnreadCount(userId);
  } catch {
    if (current === generation) scheduleRetry(userId);
  }
}

function scheduleRetry(userId: string) {
  window.clearTimeout(retryTimer);
  retryTimer = window.setTimeout(() => {
    if (connectedUserId === userId) {
      connection = null;
      connectedUserId = null;
      void startRealtime(userId);
    }
  }, 60_000);
}

export async function stopRealtime(): Promise<void> {
  generation++;
  window.clearTimeout(retryTimer);
  setLiveUpdates(false);
  const hub = connection;
  connection = null;
  connectedUserId = null;
  if (hub) {
    try {
      await hub.stop();
    } catch {
      // Already closed.
    }
  }
}
