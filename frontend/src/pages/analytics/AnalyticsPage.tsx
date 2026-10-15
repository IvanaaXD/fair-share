import { lazy, Suspense, useEffect, useState } from 'react';
import { BarChart3, Download, MapPin, Plus, Table2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { analyticsApi } from '../../api/analytics';
import { reportsApi } from '../../api/reports';
import type { AnalyticsQuery, ExpenseLocation, SpendingAnalytics } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { SpendingChart, SpendingTable } from '../../components/charts/SpendingChart';
import { granularityName } from '../../components/charts/chartMath';
import { Alert } from '../../components/ui/Alert';
import { Button, ButtonLink } from '../../components/ui/Button';
import { SelectField } from '../../components/ui/SelectField';
import { useCurrencyOptions } from '../../i18n/currencies';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import type { ExpensesPageState } from '../expenses/ExpensesPage';
import { CategoryBreakdown, Change } from './CategoryBreakdown';
import { daysBetween, periodToParams, readPeriod, type PeriodSelection } from './analyticsPeriod';
import { PeriodFilter } from './PeriodFilter';
import { ExportReportDialog } from '../reports/ExportReportDialog';
import pageStyles from '../Page.module.css';
import styles from './Analytics.module.css';

// Leaflet is big and only needed here, so the map is loaded on its own.
const ExpenseMap = lazy(() => import('./ExpenseMap'));

const ADD_EXPENSE_STATE: ExpensesPageState = { addExpense: true };

const CURRENCY_PATTERN = /^[A-Z]{3}$/;

/** "?currency=EUR" - the currency the numbers are shown in; anything invalid is ignored. */
function readCurrency(params: URLSearchParams): string | null {
  const value = params.get('currency')?.toUpperCase() ?? '';
  return CURRENCY_PATTERN.test(value) ? value : null;
}

interface Loaded<T> {
  key: string;
  data: T | null;
  error: unknown;
}

export function AnalyticsPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const errorMessage = useErrorMessage();
  const { user } = useAuth();
  const defaultCurrency = user?.defaultCurrency ?? 'BAM';

  // The period and the currency live in the address, so a refresh or a shared link shows the
  // same numbers. The user's default currency is not written there.
  const [params, setParams] = useSearchParams();
  const selection = readPeriod(params);
  const currency = readCurrency(params) ?? defaultCurrency;
  const update = (period: PeriodSelection, code: string) =>
    setParams(
      { ...periodToParams(period), ...(code === defaultCurrency ? {} : { currency: code }) },
      { replace: true, preventScrollReset: true },
    );
  const select = (next: PeriodSelection) => update(next, currency);
  const currencyOptions = useCurrencyOptions(defaultCurrency, currency);

  const [view, setView] = useState<'chart' | 'table'>('chart');
  const [exporting, setExporting] = useState(false);

  const query: AnalyticsQuery =
    selection.period === 'Custom'
      ? { period: 'Custom', from: selection.from, to: selection.to, currency }
      : { period: selection.period, currency };
  const key = JSON.stringify(query);

  const [spending, setSpending] = useState<Loaded<SpendingAnalytics> | null>(null);
  const [locations, setLocations] = useState<Loaded<ExpenseLocation[]> | null>(null);

  useEffect(() => {
    const request: AnalyticsQuery = JSON.parse(key) as AnalyticsQuery;
    const controller = new AbortController();
    analyticsApi.spending(request, controller.signal).then(
      (data) => setSpending({ key, data, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setSpending({ key, data: null, error });
      },
    );
    // The map shows expenses in every currency, so it does not need the currency.
    analyticsApi.map({ ...request, currency: undefined }, controller.signal).then(
      (data) => setLocations({ key, data, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLocations({ key, data: null, error });
      },
    );
    return () => controller.abort();
  }, [key]);

  // While a new period loads, the old numbers stay on screen (dimmed) instead of jumping away.
  const data = spending?.data ?? null;
  const loading = spending?.key !== key;
  const spendingError = spending?.key === key ? spending.error : null;
  const mapData = locations?.data ?? null;
  const mapError = locations?.key === key ? locations.error : null;

  // The numbers on screen are in the currency they were calculated in; while another currency
  // loads they stay (dimmed) with their own currency instead of getting the new symbol.
  const shown = data?.currency ?? currency;
  const isEmpty = data !== null && data.expenseCount === 0 && !loading;
  const days = data ? daysBetween(data.from, data.to) : 1;
  const top = data?.byCategory[0];
  const unit = data ? granularityName(data.granularity) : 'Day';

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('analytics.title')}</h1>
          <p className={pageStyles.subtitle} aria-live="polite">
            {data ? format.dateRange(data.from, data.to) : t('analytics.subtitle')}
          </p>
        </div>
        <div className={styles.headerActions}>
          {/* Every expense is converted to this currency with today's rate (also in the report). */}
          <SelectField
            compact
            label={t('analytics.currency')}
            name="currency"
            value={currency}
            onChange={(event) => update(selection, event.target.value)}
            options={currencyOptions}
            className={styles.currencySelect}
          />
          {data && (
            <Button variant="secondary" icon={<Download size={18} aria-hidden="true" />} onClick={() => setExporting(true)}>
              {t('reports.button')}
            </Button>
          )}
        </div>
      </header>

      <PeriodFilter selection={selection} onSelect={select} />

      {spendingError != null && (
        <Alert tone="error">
          {t('analytics.loadError')} {errorMessage(spendingError)}
        </Alert>
      )}

      {!data && spendingError == null && <p className={styles.loading}>{t('analytics.loading')}</p>}

      {isEmpty && (
        <section className={pageStyles.empty} aria-labelledby="analytics-empty-title">
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            <BarChart3 size={28} strokeWidth={1.75} />
          </span>
          <h2 id="analytics-empty-title" className={pageStyles.emptyTitle}>
            {t('analytics.emptyTitle')}
          </h2>
          <p className={pageStyles.emptyText}>{t('analytics.emptyText')}</p>
          <div className={pageStyles.actions}>
            <ButtonLink to="/expenses" state={ADD_EXPENSE_STATE} icon={<Plus size={18} aria-hidden="true" />}>
              {t('overview.addExpense')}
            </ButtonLink>
          </div>
        </section>
      )}

      {data && !isEmpty && (
        <div className={styles.content} aria-busy={loading}>
          <section className={styles.stats} aria-label={t('analytics.summary')}>
            <div className={`${styles.stat} ${styles.statMain}`}>
              <span className={styles.statLabel}>{t('analytics.totalSpent')}</span>
              <span className={`${styles.statValue} ${styles.statHero} money`}>{format.money(data.totalSpent, shown)}</span>
              <span className={styles.statNote}>
                <Change current={data.totalSpent} previous={data.previousPeriodTotal} />{' '}
                {data.previousPeriodTotal > 0
                  ? t('analytics.comparedTo', {
                      range: format.dateRange(data.previousFrom, data.previousTo),
                      amount: format.money(data.previousPeriodTotal, shown),
                    })
                  : t('analytics.noPrevious')}
              </span>
            </div>
            <div className={styles.stat}>
              <span className={styles.statLabel}>{t('analytics.count')}</span>
              <span className={styles.statValue}>{format.number(data.expenseCount)}</span>
            </div>
            <div className={styles.stat}>
              <span className={styles.statLabel}>{t('analytics.perDay')}</span>
              <span className={`${styles.statValue} money`}>{format.money(data.totalSpent / days, shown)}</span>
            </div>
            {top && (
              <div className={styles.stat}>
                <span className={styles.statLabel}>{t('analytics.topCategory')}</span>
                <span className={styles.statValue}>{localize(top.categoryName)}</span>
                <span className={styles.statNote}>{t('analytics.share', { percent: format.percent(top.percentage) })}</span>
              </div>
            )}
          </section>

          <section className={styles.card} aria-labelledby="analytics-timeline-title">
            <div className={styles.cardHeader}>
              <div>
                <h2 id="analytics-timeline-title" className={styles.cardTitle}>
                  {t('analytics.timelineTitle')}
                </h2>
                <p className={styles.cardHint}>
                  {t(`analytics.by_${unit}`)} · {shown}
                </p>
              </div>
              <div className={styles.viewSwitch} role="group" aria-label={t('analytics.viewLabel')}>
                <button
                  type="button"
                  aria-pressed={view === 'chart'}
                  className={view === 'chart' ? `${styles.viewButton} ${styles.viewActive}` : styles.viewButton}
                  onClick={() => setView('chart')}
                  title={t('analytics.viewChart')}
                >
                  <BarChart3 size={17} aria-hidden="true" />
                  <span className="visually-hidden">{t('analytics.viewChart')}</span>
                </button>
                <button
                  type="button"
                  aria-pressed={view === 'table'}
                  className={view === 'table' ? `${styles.viewButton} ${styles.viewActive}` : styles.viewButton}
                  onClick={() => setView('table')}
                  title={t('analytics.viewTable')}
                >
                  <Table2 size={17} aria-hidden="true" />
                  <span className="visually-hidden">{t('analytics.viewTable')}</span>
                </button>
              </div>
            </div>
            {view === 'chart' ? (
              <SpendingChart
                points={data.timeline}
                granularity={data.granularity}
                currency={shown}
                label={t('analytics.timelineChartLabel')}
              />
            ) : (
              <SpendingTable points={data.timeline} granularity={data.granularity} currency={shown} caption={t('analytics.timelineTitle')} />
            )}
          </section>

          <div className={styles.twoColumns}>
            <section className={styles.card} aria-labelledby="analytics-categories-title">
              <div>
                <h2 id="analytics-categories-title" className={styles.cardTitle}>
                  {t('analytics.categoriesTitle')}
                </h2>
                <p className={styles.cardHint}>{t('analytics.categoriesHint')}</p>
              </div>
              <CategoryBreakdown categories={data.byCategory} currency={shown} from={data.from} to={data.to} />
            </section>

            <section className={styles.card} aria-labelledby="analytics-map-title">
              <div>
                <h2 id="analytics-map-title" className={styles.cardTitle}>
                  {t('analytics.mapTitle')}
                </h2>
                <p className={styles.cardHint}>
                  {mapData ? t('analytics.mapCount', { count: mapData.length }) : t('analytics.mapLoading')}
                </p>
              </div>
              {mapError != null && <Alert tone="error">{errorMessage(mapError)}</Alert>}
              {mapData && mapData.length > 0 && (
                <Suspense fallback={<div className={styles.map} />}>
                  <ExpenseMap locations={mapData} />
                </Suspense>
              )}
              {mapData && mapData.length === 0 && (
                <div className={styles.mapEmpty}>
                  <MapPin size={24} aria-hidden="true" />
                  <p>{t('analytics.mapEmpty')}</p>
                </div>
              )}
            </section>
          </div>

          <p className={styles.note}>
            {t('analytics.note', { currency: shown })}
            {data.convertedExpenseCount ? (
              <>
                {' '}
                {t('analytics.converted', {
                  count: data.convertedExpenseCount,
                  date: data.ratesUpdatedAt ? format.longDate(data.ratesUpdatedAt).replace(/\.$/, '') : '—',
                })}{' '}
                <a href="https://www.exchangerate-api.com" target="_blank" rel="noreferrer">
                  {t('analytics.ratesSource')}
                </a>
              </>
            ) : null}
            {data.unconvertedExpenseCount ? <> {t('analytics.unconverted', { count: data.unconvertedExpenseCount })}</> : null}
          </p>
        </div>
      )}

      {/* The same period and currency as the page, so the report matches what is on the screen. */}
      {exporting && data && (
        <ExportReportDialog
          title={t('reports.spendingTitle')}
          download={(options) => reportsApi.spending(query, options)}
          fileName={(reportFormat) => `fairshare-${data.from.slice(0, 10)}-${data.to.slice(0, 10)}.${reportFormat.toLowerCase()}`}
          onClose={() => setExporting(false)}
        >
          <p>{t('reports.spendingText', { range: format.dateRange(data.from, data.to), currency })}</p>
        </ExportReportDialog>
      )}
    </div>
  );
}
