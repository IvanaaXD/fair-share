import { useEffect, useSyncExternalStore } from 'react';
import { categoriesApi } from '../api/categories';

/**
 * Number of category proposals waiting for an administrator - the badge next to "Categories"
 * in the administrator's menu. Checked when the menu appears, every two minutes while the app is
 * visible, and right after the administrator approves or rejects something.
 */
const POLL_MS = 120_000;

let count = 0;
const listeners = new Set<() => void>();

function set(next: number) {
  if (next !== count) {
    count = next;
    listeners.forEach((listener) => listener());
  }
}

export async function refreshPendingCategories(): Promise<void> {
  try {
    const pending = await categoriesApi.adminList('Pending');
    set(pending.length);
  } catch {
    // Only a badge - the next check will try again.
  }
}

/** For administrators only; others always get 0 and nothing is requested. */
export function usePendingCategoryCount(enabled: boolean): number {
  const value = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => count,
  );

  useEffect(() => {
    if (!enabled) return;
    void refreshPendingCategories();
    const timer = window.setInterval(() => {
      if (document.visibilityState === 'visible') void refreshPendingCategories();
    }, POLL_MS);
    return () => window.clearInterval(timer);
  }, [enabled]);

  return enabled ? value : 0;
}
