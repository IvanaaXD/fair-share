import { useEffect } from 'react';
import { useAuth } from '../auth/useAuth';
import { startRealtime, stopRealtime } from './realtime';

/** Keeps the real-time connection open while a user is signed in (used by the app layout). */
export function useRealtimeConnection() {
  const { user } = useAuth();
  const userId = user?.id ?? null;

  useEffect(() => {
    if (!userId) return;
    void startRealtime(userId);
    return () => {
      void stopRealtime();
    };
  }, [userId]);
}
