import { useCallback, useEffect, useSyncExternalStore } from 'react';
import { groupsApi } from '../api/groups';
import type { Group } from '../api/types';
import { useAuth } from '../auth/useAuth';

/** A group with the signed-in user's balance in it (null when the balance could not be loaded). */
export interface GroupSummary extends Group {
  myBalance: number | null;
}

interface State {
  /** Whose groups these are - another sign-in in the same tab starts from scratch. */
  userId: string | null;
  groups: GroupSummary[] | null;
  error: unknown;
  loading: boolean;
}

/**
 * The signed-in user's groups, shared by the sidebar ("Your groups"), the groups page and the
 * overview. Loaded once and reloaded after a change (new group, member added, group left...),
 * so every place shows the same list without loading it separately.
 */
let state: State = { userId: null, groups: null, error: null, loading: false };
const listeners = new Set<() => void>();
let requestId = 0;

function setState(next: State) {
  state = next;
  listeners.forEach((listener) => listener());
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

async function loadSummary(group: Group, userId: string): Promise<GroupSummary> {
  try {
    const balances = await groupsApi.balances(group.id);
    const mine = balances.find((balance) => balance.userId === userId);
    return { ...group, myBalance: mine ? mine.netBalance : 0 };
  } catch {
    // The list is still useful without one balance; the group page shows the real error.
    return { ...group, myBalance: null };
  }
}

/** Loads (or reloads) the groups of the given user. Older responses that arrive late are ignored. */
export async function reloadMyGroups(userId: string): Promise<void> {
  const id = ++requestId;
  const sameUser = state.userId === userId;
  setState({ userId, groups: sameUser ? state.groups : null, error: null, loading: true });

  try {
    const groups = await groupsApi.mine();
    // The balance of every group is one request; they run in parallel.
    const summaries = await Promise.all(groups.map((group) => loadSummary(group, userId)));
    summaries.sort((a, b) => a.name.localeCompare(b.name));
    if (id === requestId) {
      setState({ userId, groups: summaries, error: null, loading: false });
    }
  } catch (error) {
    if (id === requestId) {
      setState({ userId, groups: sameUser ? state.groups : null, error, loading: false });
    }
  }
}

export function useMyGroups() {
  const { user } = useAuth();
  const userId = user?.id ?? null;
  const snapshot = useSyncExternalStore(subscribe, () => state);

  useEffect(() => {
    if (userId && snapshot.userId !== userId && !snapshot.loading) {
      void reloadMyGroups(userId);
    }
  }, [userId, snapshot.userId, snapshot.loading]);

  const reload = useCallback(() => (userId ? reloadMyGroups(userId) : Promise.resolve()), [userId]);
  const current = snapshot.userId === userId;

  return {
    groups: current ? snapshot.groups : null,
    error: current ? snapshot.error : null,
    loading: !current || snapshot.loading,
    reload,
  };
}
