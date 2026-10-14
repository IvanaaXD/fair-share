import { createElement, useEffect, useState } from 'react';
import { ArrowRight, Plus, ReceiptText, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { analyticsApi } from '../api/analytics';
import { budgetsApi } from '../api/budgets';
import { expensesApi } from '../api/expenses';
import { notificationsApi } from '../api/notifications';
import type { AppNotification, Budget, Expense, SpendingAnalytics } from '../api/types';
import { useErrorMessage } from '../api/useErrorMessage';
import { useAuth } from '../auth/useAuth';
import { SpendingChart } from '../components/charts/SpendingChart';
import { Alert } from '../components/ui/Alert';
import { ButtonLink } from '../components/ui/Button';
import { balanceStatus, groupIcon } from '../groups/groupUtils';
import { useMyGroups, type GroupSummary } from '../groups/myGroupsStore';
import { addMonths, currentMonth, todayDateOnly, useFormatters } from '../i18n/format';
import { useLocalizedText } from '../i18n/useLocalizedText';
import { NOTIFICATION_ICONS, notificationTypeName, WARNING_TYPES } from '../notifications/notificationTypes';
import { Change } from './analytics/CategoryBreakdown';
import { categoryIcon } from './expenses/categoryIcons';
import type { ExpensesPageState } from './expenses/ExpensesPage';
import type { GroupsPageState } from './groups/GroupsPage';
import styles from './Overview.module.css';
import pageStyles from './Page.module.css';

/** Opens the expenses page with the "new expense" window already open. */
const ADD_EXPENSE_STATE: ExpensesPageState = { addExpense: true };
/** Opens the groups page with the "new group" window already open. */
const CREATE_GROUP_STATE: GroupsPageState = { createGroup: true };

const RECENT_COUNT = 5;
const LIST_LIMIT = 4;
const WARNING_PERCENT = 80;

/** One section's data; every section loads and fails on its own, so one error hides only one card. */
type Section<T> = { status: 'loading' } | { status: 'done'; data: T } | { status: 'error'; error: unknown };

interface OverviewData {
  thisMonth: Section<SpendingAnalytics>;
  /** The same days of the previous month (1st to today's day), for a fair comparison. */
  lastMonth: Section<SpendingAnalytics>;
  budgets: Section<Budget[]>;
  recent: Section<{ items: Expense[]; total: number }>;
  unread: Section<AppNotification[]>;
}

const LOADING: OverviewData = {
  thisMonth: { status: 'loading' },
  lastMonth: { status: 'loading' },
  budgets: { status: 'loading' },
  recent: { status: 'loading' },
  unread: { status: 'loading' },
};

function settle<T>(promise: Promise<T>): Promise<Section<T>> {
  return promise.then(
    (data) => ({ status: 'done', data }),
    (error: unknown) => ({ status: 'error', error }),
  );
}

/** The days to compare: this month so far, and the same days of last month. */
function comparisonRanges(today: string) {
  const month = today.slice(0, 7);
  const day = Number(today.slice(8, 10));
  const previous = addMonths(month, -1);
  const [year, monthNumber] = previous.split('-').map(Number);
  const daysInPrevious = new Date(Date.UTC(year, monthNumber, 0)).getUTCDate();
  return {
    current: { from: `${month}-01`, to: today },
    previous: { from: `${previous}-01`, to: `${previous}-${String(Math.min(day, daysInPrevious)).padStart(2, '0')}` },
  };
}

/** Amounts in different currencies are listed side by side, never added together. */
function sumByCurrency(groups: GroupSummary[], sign: 1 | -1) {
  const totals = new Map<string, number>();
  for (const group of groups) {
    const balance = group.myBalance ?? 0;
    if (balanceStatus(balance) === (sign === 1 ? 'owed' : 'owe')) {
      totals.set(group.currency, (totals.get(group.currency) ?? 0) + Math.abs(balance));
    }
  }
  return [...totals.entries()];
}

export function OverviewPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const errorMessage = useErrorMessage();
  const { user } = useAuth();
  const currency = user?.defaultCurrency ?? 'BAM';
  const { groups, error: groupsError } = useMyGroups();

  // Read once when the page opens; a render must not depend on the current time.
  const [today] = useState(() => todayDateOnly());
  const [month] = useState(() => currentMonth());
  const ranges = comparisonRanges(today);

  const [data, setData] = useState<OverviewData>(LOADING);

  useEffect(() => {
    const controller = new AbortController();
    const { signal } = controller;
    const { current, previous } = comparisonRanges(today);

    // Every card starts loading at once; each one appears as soon as its own data is there.
    const update = <K extends keyof OverviewData>(key: K) => (section: OverviewData[K]) => {
      if (!signal.aborted) setData((state) => ({ ...state, [key]: section }));
    };
    void settle(analyticsApi.spending({ period: 'Custom', ...current, currency }, signal)).then(update('thisMonth'));
    void settle(analyticsApi.spending({ period: 'Custom', ...previous, currency }, signal)).then(update('lastMonth'));
    void settle(budgetsApi.mine(month, signal)).then(update('budgets'));
    void settle(
      expensesApi
        .list({ page: 1, pageSize: RECENT_COUNT, sortBy: 'Date', sortDescending: true }, signal)
        .then((page) => ({ items: page.items, total: page.totalCount })),
    ).then(update('recent'));
    void settle(notificationsApi.mine(false, signal)).then(update('unread'));

    return () => controller.abort();
  }, [today, month, currency]);

  const header = (
    <header className={pageStyles.header}>
      <div className={pageStyles.headerText}>
        <h1 className={pageStyles.title}>{t('overview.title')}</h1>
        <p className={pageStyles.subtitle}>{format.month(month)}</p>
      </div>
      <ButtonLink to="/expenses" state={ADD_EXPENSE_STATE} icon={<Plus size={18} aria-hidden="true" />}>
        {t('overview.addExpense')}
      </ButtonLink>
    </header>
  );

  // A brand-new account: nothing to show yet, only the two ways to begin.
  const isNewUser = data.recent.status === 'done' && data.recent.data.total === 0 && groups !== null && groups.length === 0;
  if (isNewUser) {
    return (
      <div className={pageStyles.page}>
        {header}
        <section className={pageStyles.empty} aria-labelledby="overview-empty-title">
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            <ReceiptText size={28} strokeWidth={1.75} />
          </span>
          <h2 id="overview-empty-title" className={pageStyles.emptyTitle}>
            {t('overview.emptyTitle')}
          </h2>
          <p className={pageStyles.emptyText}>{t('overview.emptyText')}</p>
          <div className={pageStyles.actions}>
            <ButtonLink to="/expenses" state={ADD_EXPENSE_STATE} icon={<Plus size={18} aria-hidden="true" />}>
              {t('overview.addExpense')}
            </ButtonLink>
            <ButtonLink to="/groups" state={CREATE_GROUP_STATE} variant="secondary" icon={<Users size={18} aria-hidden="true" />}>
              {t('overview.createGroup')}
            </ButtonLink>
          </div>
        </section>
      </div>
    );
  }

  const money = (amount: number) => format.money(amount, currency);
  const thisMonth = data.thisMonth.status === 'done' ? data.thisMonth.data : null;
  const lastMonth = data.lastMonth.status === 'done' ? data.lastMonth.data : null;
  const budgets = data.budgets.status === 'done' ? data.budgets.data : null;
  // Over = more spent than the limit; exactly at the limit still counts as "close to the limit".
  const isOver = (budget: Budget) => budget.currentSpending - budget.monthlyLimit > 0.005;
  const overBudgets = budgets?.filter(isOver).length ?? 0;
  const nearBudgets = budgets?.filter((budget) => !isOver(budget) && budget.percentageUsed >= WARNING_PERCENT).length ?? 0;
  const iOwe = groups ? sumByCurrency(groups, -1) : [];
  const owedToMe = groups ? sumByCurrency(groups, 1) : [];
  const amounts = (list: [string, number][]) =>
    list.length === 0 ? format.money(0, currency) : list.map(([code, amount]) => format.money(amount, code)).join(' + ');
  const dash = '—';

  return (
    <div className={pageStyles.page}>
      {header}

      {/* ---------- the four numbers ---------- */}
      <section className={styles.stats} aria-label={t('overview.summary')}>
        <div className={`${styles.stat} ${styles.statMain}`}>
          <span className={styles.statLabel}>{t('overview.spentThisMonth')}</span>
          <span className={`${styles.statValue} ${styles.statHero} money`}>{thisMonth ? money(thisMonth.totalSpent) : dash}</span>
          {thisMonth && lastMonth && (
            <span className={styles.statNote}>
              <Change current={thisMonth.totalSpent} previous={lastMonth.totalSpent} />{' '}
              {lastMonth.totalSpent > 0
                ? t('overview.comparedTo', {
                    range: format.dateRange(ranges.previous.from, ranges.previous.to),
                    amount: money(lastMonth.totalSpent),
                  })
                : t('overview.noPrevious')}
            </span>
          )}
          {data.thisMonth.status === 'error' && <span className={styles.statNote}>{t('overview.loadFailed')}</span>}
        </div>
        <Link to="/groups" className={`${styles.stat} ${styles.statLink}`}>
          <span className={styles.statLabel}>{t('overview.youOwe')}</span>
          <span className={`${styles.statValue} money ${iOwe.length > 0 ? styles.oweText : ''}`}>{groups ? amounts(iOwe) : dash}</span>
          <span className={styles.statNote}>{t('overview.inGroups', { count: countGroups(groups, 'owe') })}</span>
        </Link>
        <Link to="/groups" className={`${styles.stat} ${styles.statLink}`}>
          <span className={styles.statLabel}>{t('overview.owedToYou')}</span>
          <span className={`${styles.statValue} money ${owedToMe.length > 0 ? styles.owedText : ''}`}>{groups ? amounts(owedToMe) : dash}</span>
          <span className={styles.statNote}>{t('overview.inGroups', { count: countGroups(groups, 'owed') })}</span>
        </Link>
        <Link to="/budgets" className={`${styles.stat} ${styles.statLink}`}>
          <span className={styles.statLabel}>{t('overview.budgets')}</span>
          <span className={`${styles.statValue} ${overBudgets > 0 ? styles.oweText : ''}`}>
            {budgets === null ? dash : budgets.length === 0 ? t('overview.noBudgetsShort') : t('overview.budgetsOver', { count: overBudgets })}
          </span>
          {budgets !== null && budgets.length > 0 && (
            <span className={styles.statNote}>
              {nearBudgets > 0 ? t('overview.budgetsNear', { count: nearBudgets }) : t('overview.budgetsOf', { count: budgets.length })}
            </span>
          )}
        </Link>
      </section>

      <div className={styles.grid}>
        {/* ---------- this month by day ---------- */}
        <section className={`${styles.card} ${styles.wide}`} aria-labelledby="overview-chart-title">
          <CardHeader
            id="overview-chart-title"
            title={t('overview.chartTitle')}
            hint={thisMonth ? t('analytics.expenseCount', { count: thisMonth.expenseCount }) : undefined}
            link="/analytics"
            linkText={t('overview.toAnalytics')}
          />
          {data.thisMonth.status === 'error' && <Alert tone="error">{errorMessage(data.thisMonth.error)}</Alert>}
          {thisMonth && thisMonth.expenseCount === 0 && <p className={styles.empty}>{t('overview.noSpendingYet')}</p>}
          {thisMonth && thisMonth.expenseCount > 0 && (
            <SpendingChart
              points={thisMonth.timeline}
              granularity={thisMonth.granularity}
              currency={currency}
              label={t('overview.chartLabel')}
              height={290}
              compact
            />
          )}
          {data.thisMonth.status === 'loading' && <div className={styles.chartPlaceholder} />}
        </section>

        {/* ---------- budgets closest to the limit ---------- */}
        <section className={styles.card} aria-labelledby="overview-budgets-title">
          <CardHeader id="overview-budgets-title" title={t('overview.budgetsTitle')} link="/budgets" linkText={t('overview.all')} />
          {data.budgets.status === 'error' && <Alert tone="error">{errorMessage(data.budgets.error)}</Alert>}
          {budgets && budgets.length === 0 && (
            <p className={styles.empty}>
              {t('overview.noBudgets')} <Link to="/budgets">{t('overview.setBudget')}</Link>
            </p>
          )}
          {budgets && budgets.length > 0 && (
            <ul className={styles.list}>
              {[...budgets]
                .sort((a, b) => b.percentageUsed - a.percentageUsed)
                .slice(0, LIST_LIMIT)
                .map((budget) => {
                  const level = budget.percentageUsed >= WARNING_PERCENT ? styles.meterWarning : '';
                  return (
                    <li key={budget.id} className={styles.budget}>
                      <span className={styles.budgetTop}>
                        <span className={styles.rowTitle}>{localize(budget.categoryName)}</span>
                        <span className={`${styles.percent} money`}>{Math.round(budget.percentageUsed)}%</span>
                      </span>
                      <span className={`${styles.meter} ${level}`} aria-hidden="true">
                        <span style={{ width: `${Math.min(100, budget.percentageUsed)}%` }} />
                      </span>
                      <span className={styles.rowMeta}>
                        {t('budgets.spentOf', { spent: money(budget.currentSpending), limit: money(budget.monthlyLimit) })}
                      </span>
                    </li>
                  );
                })}
            </ul>
          )}
        </section>

        {/* ---------- groups ---------- */}
        <section className={styles.card} aria-labelledby="overview-groups-title">
          <CardHeader id="overview-groups-title" title={t('overview.groupsTitle')} link="/groups" linkText={t('overview.all')} />
          {groupsError != null && !groups && <Alert tone="error">{errorMessage(groupsError)}</Alert>}
          {groups && groups.length === 0 && (
            <p className={styles.empty}>
              {t('overview.noGroups')}{' '}
              <Link to="/groups" state={CREATE_GROUP_STATE}>
                {t('overview.createGroup')}
              </Link>
            </p>
          )}
          {groups && groups.length > 0 && <GroupBalances groups={groups} />}
        </section>

        {/* ---------- latest expenses ---------- */}
        <section className={styles.card} aria-labelledby="overview-recent-title">
          <CardHeader id="overview-recent-title" title={t('overview.recentTitle')} link="/expenses" linkText={t('overview.all')} />
          {data.recent.status === 'error' && <Alert tone="error">{errorMessage(data.recent.error)}</Alert>}
          {data.recent.status === 'done' && data.recent.data.items.length === 0 && (
            <p className={styles.empty}>{t('overview.noExpenses')}</p>
          )}
          {data.recent.status === 'done' && data.recent.data.items.length > 0 && (
            <ul className={styles.list}>
              {data.recent.data.items.map((expense) => (
                <li key={expense.id} className={styles.row}>
                  <span className={styles.rowIcon} aria-hidden="true">
                    {createElement(categoryIcon(expense.categoryName), { size: 18 })}
                  </span>
                  <span className={styles.rowText}>
                    <span className={styles.rowTitle}>{localize(expense.description || expense.categoryName)}</span>
                    <span className={styles.rowMeta}>
                      {localize(expense.categoryName)} · {format.shortDate(expense.date)}
                    </span>
                  </span>
                  <span className={`${styles.rowAmount} money`}>{format.money(expense.amount, expense.currency)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        {/* ---------- unread notifications ---------- */}
        <section className={styles.card} aria-labelledby="overview-notifications-title">
          <CardHeader
            id="overview-notifications-title"
            title={t('overview.notificationsTitle')}
            link="/notifications"
            linkText={t('overview.all')}
          />
          {data.unread.status === 'error' && <Alert tone="error">{errorMessage(data.unread.error)}</Alert>}
          {data.unread.status === 'done' && data.unread.data.length === 0 && (
            <p className={styles.empty}>{t('overview.noNotifications')}</p>
          )}
          {data.unread.status === 'done' && data.unread.data.length > 0 && (
            <ul className={styles.list}>
              {data.unread.data.slice(0, LIST_LIMIT).map((notification) => {
                const type = notificationTypeName(notification.type);
                return (
                  <li key={notification.id} className={styles.row}>
                    <span
                      className={WARNING_TYPES.has(type) ? `${styles.rowIcon} ${styles.rowIconWarning}` : styles.rowIcon}
                      aria-hidden="true"
                    >
                      {createElement(NOTIFICATION_ICONS[type], { size: 18 })}
                    </span>
                    <span className={styles.rowText}>
                      <span className={styles.message}>{localize(notification.message)}</span>
                      <span className={styles.rowMeta}>
                        {t(`notificationType.${type}`)} · {format.shortDate(notification.createdAt)}
                      </span>
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}

function countGroups(groups: GroupSummary[] | null, status: 'owe' | 'owed') {
  return groups?.filter((group) => group.myBalance !== null && balanceStatus(group.myBalance) === status).length ?? 0;
}

function CardHeader({ id, title, hint, link, linkText }: { id: string; title: string; hint?: string; link: string; linkText: string }) {
  return (
    <div className={styles.cardHeader}>
      <div>
        <h2 id={id} className={styles.cardTitle}>
          {title}
        </h2>
        {hint && <p className={styles.cardHint}>{hint}</p>}
      </div>
      <Link to={link} className={styles.cardLink}>
        {linkText}
        <ArrowRight size={15} aria-hidden="true" />
      </Link>
    </div>
  );
}

/** Groups where something is open first (what I owe, then what I am owed), settled ones last. */
function GroupBalances({ groups }: { groups: GroupSummary[] }) {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const order = { owe: 0, owed: 1, settled: 2 } as const;
  const sorted = [...groups].sort((a, b) => {
    const statusA = a.myBalance === null ? 'settled' : balanceStatus(a.myBalance);
    const statusB = b.myBalance === null ? 'settled' : balanceStatus(b.myBalance);
    return order[statusA] - order[statusB] || Math.abs(b.myBalance ?? 0) - Math.abs(a.myBalance ?? 0);
  });

  return (
    <ul className={styles.list}>
      {sorted.slice(0, LIST_LIMIT).map((group) => {
        const status = group.myBalance === null ? null : balanceStatus(group.myBalance);
        const amount = format.money(Math.abs(group.myBalance ?? 0), group.currency);
        return (
          <li key={group.id}>
            <Link to={`/groups/${group.id}`} className={`${styles.row} ${styles.rowLink}`}>
              <span className={styles.rowIcon} aria-hidden="true">
                {createElement(groupIcon(group.name), { size: 18 })}
              </span>
              <span className={styles.rowText}>
                <span className={styles.rowTitle}>{localize(group.name)}</span>
                <span className={styles.rowMeta}>{t('overview.members', { count: group.members.length })}</span>
              </span>
              <span
                className={
                  status === 'owe' ? `${styles.balance} ${styles.oweText}` : status === 'owed' ? `${styles.balance} ${styles.owedText}` : styles.balance
                }
              >
                {status === 'owe'
                  ? t('overview.balanceOwe', { amount })
                  : status === 'owed'
                    ? t('overview.balanceOwed', { amount })
                    : status === 'settled'
                      ? t('overview.balanceSettled')
                      : '—'}
              </span>
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
