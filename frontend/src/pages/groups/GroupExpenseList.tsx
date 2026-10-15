import { useCallback, useEffect, useState } from 'react';
import { MessageSquare, ReceiptText } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { groupExpensesApi } from '../../api/groupExpenses';
import type { GroupExpense } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { initials, memberColor } from '../../groups/groupUtils';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Groups.module.css';

const PAGE_SIZE = 20;

interface ListState {
  /** Which load this is the result of - a new version means the list is being reloaded. */
  key: string;
  items: GroupExpense[];
  page: number;
  hasMore: boolean;
  loadingMore: boolean;
  error: unknown;
}

/**
 * The group's expenses, newest first. The backend returns pages without a total, so a page
 * that comes back full means there may be more and "Show more" loads the next one.
 */
function useGroupExpenses(groupId: string, version: number) {
  const key = `${groupId}:${version}`;
  const [state, setState] = useState<ListState>({ key: '', items: [], page: 0, hasMore: false, loadingMore: false, error: null });

  const fetchPage = useCallback(
    (page: number, signal?: AbortSignal) =>
      groupExpensesApi.list(groupId, page, PAGE_SIZE, signal).then(
        (items) =>
          setState((current) => ({
            key,
            items: page === 1 ? items : [...current.items, ...items],
            page,
            hasMore: items.length === PAGE_SIZE,
            loadingMore: false,
            error: null,
          })),
        (error: unknown) => {
          if (!signal?.aborted) {
            setState((current) => ({ ...current, key, loadingMore: false, error }));
          }
        },
      ),
    [groupId, key],
  );

  useEffect(() => {
    const controller = new AbortController();
    void fetchPage(1, controller.signal);
    return () => controller.abort();
  }, [fetchPage]);

  const loadMore = () => {
    setState((current) => ({ ...current, loadingMore: true, error: null }));
    void fetchPage(state.page + 1);
  };

  // While a reload runs, the previous items stay on screen instead of flashing empty.
  return {
    items: state.items,
    hasMore: state.hasMore,
    loading: state.key !== key || state.loadingMore,
    error: state.error,
    loadMore,
    // A failed first page is loaded again; a failed "Show more" tries the next page again.
    retry: () => (state.items.length === 0 ? void fetchPage(1) : loadMore()),
  };
}

interface GroupExpenseListProps {
  groupId: string;
  currency: string;
  /** Changes whenever the list must be loaded again (an expense was added, edited, deleted). */
  version: number;
  myId: string | undefined;
  colorOf: Map<string, string>;
  onOpen: (expense: GroupExpense) => void;
}

export function GroupExpenseList({ groupId, currency, version, myId, colorOf, onOpen }: GroupExpenseListProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const { items, hasMore, loading, error, loadMore, retry } = useGroupExpenses(groupId, version);
  const money = (amount: number) => format.money(amount, currency);

  if (items.length === 0 && loading) {
    return <p className={styles.panelHint}>{t('groupExpenses.loading')}</p>;
  }

  if (items.length === 0 && error) {
    return (
      <p className={styles.panelHint} role="alert">
        {t('groupExpenses.loadError')} {errorMessage(error)}{' '}
        <button type="button" className={styles.inlineButton} onClick={retry}>
          {t('common.retry')}
        </button>
      </p>
    );
  }

  if (items.length === 0) {
    return (
      <div className={styles.soon}>
        <ReceiptText size={22} strokeWidth={1.75} aria-hidden="true" />
        <p>{t('groupExpenses.empty')}</p>
      </div>
    );
  }

  return (
    <>
      <ul className={styles.expenseList} aria-busy={loading}>
        {items.map((expense) => {
          const title = localize(expense.description?.trim() || expense.categoryName);
          const mine = expense.splits.find((split) => split.userId === myId);
          const iPaid = expense.paidByUserId === myId;
          const [payerFirst = '', payerLast = ''] = expense.paidByName.trim().split(/\s+/);

          let share: { text: string; tone: 'owe' | 'owed' | 'muted' };
          if (iPaid) {
            share = { text: t('groupExpenses.owedToYou', { amount: money(expense.amount - (mine?.amount ?? 0)) }), tone: 'owed' };
          } else if (mine) {
            share = { text: t('groupExpenses.yourShare', { amount: money(mine.amount) }), tone: 'owe' };
          } else {
            share = { text: t('groupExpenses.notIncluded'), tone: 'muted' };
          }

          return (
            <li key={expense.id}>
              <button
                type="button"
                className={styles.expenseRow}
                onClick={() => onOpen(expense)}
              >
                <Avatar
                  initials={localize(initials(payerFirst, payerLast))}
                  color={colorOf.get(expense.paidByUserId) ?? memberColor(5)}
                />
                <span className={styles.expenseMain}>
                  <span className={styles.expenseTitle}>{title}</span>
                  <span className={styles.expenseMeta}>
                    {localize(expense.paidByName)} · {format.shortDate(expense.date)} · {localize(expense.categoryName)}
                    {expense.receiptImageUrl && (
                      <span className={styles.expenseReceipt} title={t('groupExpenses.hasReceipt')}>
                        <ReceiptText size={14} aria-hidden="true" />
                        <span className="visually-hidden">{t('groupExpenses.hasReceipt')}</span>
                      </span>
                    )}
                    {expense.commentCount ? (
                      <span className={styles.expenseComments} title={t('groupExpenses.comments', { count: expense.commentCount })}>
                        <MessageSquare size={14} aria-hidden="true" />
                        <span aria-hidden="true">{expense.commentCount}</span>
                        <span className="visually-hidden">{t('groupExpenses.comments', { count: expense.commentCount })}</span>
                      </span>
                    ) : null}
                  </span>
                  <span className={styles.splitBar} aria-hidden="true">
                    {expense.splits.map((split) =>
                      split.amount > 0 ? (
                        <span key={split.userId} style={{ flexGrow: split.amount, background: colorOf.get(split.userId) ?? memberColor(5) }} />
                      ) : null,
                    )}
                  </span>
                </span>
                <span className={styles.expenseRight}>
                  <span className={`${styles.expenseAmount} money`}>{money(expense.amount)}</span>
                  <span className={`${styles.expenseShare} ${styles[`share_${share.tone}`]}`}>{share.text}</span>
                </span>
              </button>
            </li>
          );
        })}
      </ul>
      {error !== null && (
        <p className={styles.panelHint} role="alert">
          {errorMessage(error)}{' '}
          <button type="button" className={styles.inlineButton} onClick={retry}>
            {t('common.retry')}
          </button>
        </p>
      )}
      {hasMore && (
        <Button variant="secondary" onClick={loadMore} disabled={loading} className={styles.loadMore}>
          {loading ? t('groupExpenses.loading') : t('groupExpenses.loadMore')}
        </Button>
      )}
    </>
  );
}
