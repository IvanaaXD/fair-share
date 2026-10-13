import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Plus, ReceiptText, SearchX } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate, useSearchParams } from 'react-router';
import type { Expense } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Pagination } from '../../components/ui/Pagination';
import { useCategories } from '../../hooks/useCategories';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { ExpenseDialog, type ExpenseChange } from './ExpenseDialog';
import { ExpenseFiltersBar } from './ExpenseFiltersBar';
import { ExpenseList } from './ExpenseList';
import {
  DEFAULT_FILTERS,
  hasActiveFilters,
  readFilters,
  toExpenseQuery,
  writeFilters,
  type ExpenseFilters,
} from './expenseFilters';
import { useExpenseList } from './useExpenseList';
import pageStyles from '../Page.module.css';
import styles from './ExpensesPage.module.css';

/** Other pages open this one with the "add expense" window already open (see OverviewPage). */
export interface ExpensesPageState {
  addExpense?: boolean;
}

/** null = closed, { expense: null } = adding, { expense } = editing */
type DialogState = { expense: Expense | null } | null;

export function ExpensesPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const filters = useMemo(() => readFilters(params), [params]);

  const { categories, error: categoriesError } = useCategories();
  const { data, error, loading, reload } = useExpenseList(useMemo(() => toExpenseQuery(filters), [filters]));

  const [dialog, setDialog] = useState<DialogState>(() =>
    (location.state as ExpensesPageState | null)?.addExpense ? { expense: null } : null,
  );
  // The id makes the same message shown twice in a row count as a new one (new timer).
  const [notice, setNotice] = useState<{ id: number; text: string } | null>(null);
  const listRef = useRef<HTMLElement>(null);

  // ---------- filters ----------

  const updateFilters = useCallback(
    (change: Partial<ExpenseFilters>, options: { push?: boolean } = {}) => {
      // Any change other than the page itself starts again from page 1.
      const next = { ...filters, page: 1, ...change };
      setParams(writeFilters(next), { replace: !options.push, preventScrollReset: true });
    },
    [filters, setParams],
  );

  // The search box follows typing immediately; the list (and the address) only after a pause.
  const [searchText, setSearchText] = useState(filters.search);
  const [lastUrlSearch, setLastUrlSearch] = useState(filters.search);
  if (filters.search !== lastUrlSearch) {
    // The address changed from outside (Back button, "Clear filters") - show it in the box.
    setLastUrlSearch(filters.search);
    setSearchText(filters.search);
  }
  const debouncedSearch = useDebouncedValue(searchText, 350);
  const lastDebounced = useRef(debouncedSearch);
  useEffect(() => {
    // Only a new pause in typing updates the address. Without this check, "Clear filters"
    // would be undone by the search text that was typed before it.
    if (debouncedSearch === lastDebounced.current) {
      return;
    }
    lastDebounced.current = debouncedSearch;
    if (debouncedSearch.trim() !== filters.search.trim()) {
      updateFilters({ search: debouncedSearch });
    }
  }, [debouncedSearch, filters.search, updateFilters]);

  const clearFilters = () => {
    setSearchText('');
    setParams(writeFilters({ ...DEFAULT_FILTERS, sort: filters.sort }), { replace: true, preventScrollReset: true });
  };

  const changePage = (page: number) => {
    updateFilters({ page }, { push: true });
    listRef.current?.scrollIntoView({ block: 'start', behavior: 'smooth' });
  };

  // After deleting the last expense of the last page, that page no longer exists.
  useEffect(() => {
    if (data && data.items.length === 0 && data.totalCount > 0 && filters.page > data.totalPages) {
      updateFilters({ page: data.totalPages });
    }
  }, [data, filters.page, updateFilters]);

  // ---------- "add expense" opened from another page ----------

  useEffect(() => {
    if ((location.state as ExpensesPageState | null)?.addExpense) {
      // Forget the request, so a refresh or Back does not open the window again.
      void navigate({ search: location.search }, { replace: true, state: null });
    }
  }, [location.state, location.search, navigate]);

  // ---------- messages after a change ----------

  useEffect(() => {
    if (!notice) {
      return;
    }
    const timer = window.setTimeout(() => setNotice(null), 5000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  const handleDone = (change: ExpenseChange) => {
    setDialog(null);
    setNotice({ id: Date.now(), text: t(`expenses.${change}`) });
    reload();
  };

  // ---------- what to show ----------

  const filtersActive = hasActiveFilters(filters);
  const total = data?.totalCount ?? 0;
  const firstShown = data && total > 0 ? (data.page - 1) * data.pageSize + 1 : 0;
  const lastShown = data ? firstShown + data.items.length - (data.items.length > 0 ? 1 : 0) : 0;

  let subtitle = t('app.loading');
  if (data) {
    subtitle = filtersActive ? t('expenses.found', { count: total }) : t('expenses.count', { count: total });
  } else if (error) {
    subtitle = t('expenses.loadError');
  }

  const showFirstRunEmpty = data !== null && total === 0 && !filtersActive && !loading;
  const addButton = (
    <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setDialog({ expense: null })}>
      {t('expenses.add')}
    </Button>
  );

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('expenses.title')}</h1>
          <p className={pageStyles.subtitle} aria-live="polite">
            {subtitle}
          </p>
        </div>
        {addButton}
      </header>

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && (
          <Alert key={notice.id} tone="success">
            {notice.text}
          </Alert>
        )}
      </div>

      {showFirstRunEmpty ? (
        <section className={pageStyles.empty} aria-labelledby="expenses-empty-title">
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            <ReceiptText size={28} strokeWidth={1.75} />
          </span>
          <h2 id="expenses-empty-title" className={pageStyles.emptyTitle}>
            {t('expenses.emptyTitle')}
          </h2>
          <p className={pageStyles.emptyText}>{t('expenses.emptyText')}</p>
          <div className={pageStyles.actions}>{addButton}</div>
        </section>
      ) : (
        <>
          <ExpenseFiltersBar
            filters={filters}
            searchText={searchText}
            onSearchTextChange={setSearchText}
            onChange={(change) => updateFilters(change)}
            onClear={clearFilters}
            categories={categories}
          />

          {error !== null && !loading && (
            <Alert tone="error">
              <span className={styles.errorLine}>
                {t('expenses.loadError')} {errorMessage(error)}
                <button type="button" className={styles.inlineButton} onClick={reload}>
                  {t('common.retry')}
                </button>
              </span>
            </Alert>
          )}

          {data && (
            <section
              ref={listRef}
              className={styles.listCard}
              aria-label={t('expenses.title')}
              aria-busy={loading}
              data-loading={loading || undefined}
            >
              {data.items.length > 0 ? (
                <ExpenseList expenses={data.items} onEdit={(expense) => setDialog({ expense })} />
              ) : (
                <div className={styles.noResults}>
                  <SearchX size={28} strokeWidth={1.75} aria-hidden="true" />
                  <h2 className={styles.noResultsTitle}>{t('expenses.noResultsTitle')}</h2>
                  <p className={styles.noResultsText}>{t('expenses.noResultsText')}</p>
                  {filtersActive && (
                    <Button variant="secondary" onClick={clearFilters}>
                      {t('expenses.filters.clear')}
                    </Button>
                  )}
                </div>
              )}

              {total > 0 && (
                <div className={styles.listFooter}>
                  <span className={styles.summary}>
                    {t('pagination.summary', { from: firstShown, to: lastShown, total })}
                  </span>
                  <Pagination page={data.page} totalPages={data.totalPages} onChange={changePage} />
                </div>
              )}
            </section>
          )}
        </>
      )}

      {dialog && (
        <ExpenseDialog
          expense={dialog.expense}
          categories={categories}
          categoriesFailed={categoriesError !== null}
          defaultCurrency={user?.defaultCurrency ?? 'BAM'}
          onClose={() => setDialog(null)}
          onDone={handleDone}
          onChanged={reload}
        />
      )}
    </div>
  );
}
