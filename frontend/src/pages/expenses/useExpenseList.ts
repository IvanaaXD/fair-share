import { useCallback, useEffect, useState } from 'react';
import { expensesApi } from '../../api/expenses';
import type { Expense, ExpenseQuery, PagedResponse } from '../../api/types';

interface LoadedState {
  /** Which request this result belongs to. */
  key: string;
  version: number;
  data: PagedResponse<Expense> | null;
  error: unknown;
}

/**
 * Loads one page of expenses for the given query and loads it again whenever the query
 * changes. The previous page stays on screen while the next one loads, so the list does not
 * jump; an outdated response (the user kept typing) is cancelled and never shown.
 */
export function useExpenseList(query: ExpenseQuery) {
  const key = JSON.stringify(query);
  const [version, setVersion] = useState(0);
  const [state, setState] = useState<LoadedState>({ key: '', version: -1, data: null, error: null });

  useEffect(() => {
    const controller = new AbortController();
    expensesApi.list(JSON.parse(key) as ExpenseQuery, controller.signal).then(
      (data) => setState({ key, version, data, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setState((previous) => ({ key, version, data: previous.data, error }));
        }
      },
    );
    return () => controller.abort();
  }, [key, version]);

  /** Loads the same page again, e.g. after an expense was added, changed or deleted. */
  const reload = useCallback(() => setVersion((value) => value + 1), []);

  return {
    data: state.data,
    error: state.error,
    loading: state.key !== key || state.version !== version,
    reload,
  };
}
