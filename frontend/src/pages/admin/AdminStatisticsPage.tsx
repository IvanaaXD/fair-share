import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { adminApi } from '../../api/admin';
import type { AdminSeries, AdminStatistics, AnalyticsQuery } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { SpendingChart } from '../../components/charts/SpendingChart';
import { granularityName } from '../../components/charts/chartMath';
import { Alert } from '../../components/ui/Alert';
import { useFormatters } from '../../i18n/format';
import { periodToParams, readPeriod, type PeriodSelection } from '../analytics/analyticsPeriod';
import { PeriodFilter } from '../analytics/PeriodFilter';
import pageStyles from '../Page.module.css';
import styles from './Admin.module.css';

/** One small chart per kind of record - the counts differ a lot, so they do not share an axis. */
const SERIES: AdminSeries[] = ['newUsers', 'newGroups', 'personalExpenses', 'groupExpenses', 'settlements', 'payments'];

/** Payment statuses the app knows by name; any other is shown as it comes. */
const PAYMENT_STATUSES = ['Succeeded', 'Pending', 'Failed', 'Declined'] as const;

export function AdminStatisticsPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const errorMessage = useErrorMessage();

  const [params, setParams] = useSearchParams();
  const selection = readPeriod(params);
  const select = (next: PeriodSelection) => setParams(periodToParams(next), { replace: true, preventScrollReset: true });

  const query: Omit<AnalyticsQuery, 'currency'> =
    selection.period === 'Custom' ? { period: 'Custom', from: selection.from, to: selection.to } : { period: selection.period };
  const key = JSON.stringify(query);
  const [loaded, setLoaded] = useState<{ key: string; data: AdminStatistics | null; error: unknown } | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    adminApi.statistics(JSON.parse(key) as Omit<AnalyticsQuery, 'currency'>, controller.signal).then(
      (data) => setLoaded({ key, data, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded({ key, data: null, error });
      },
    );
    return () => controller.abort();
  }, [key]);

  const data = loaded?.data ?? null;
  const loading = loaded?.key !== key;
  const error = loaded?.key === key ? loaded.error : null;
  const totals = data?.totals;
  const unit = data ? granularityName(data.granularity) : 'Day';
  const count = (value: number) => format.number(value);

  const payments = totals ? Object.entries(totals.paymentsByStatus) : [];
  const paymentTotal = payments.reduce((sum, [, value]) => sum + value, 0);
  const statusName = (status: string) =>
    (PAYMENT_STATUSES as readonly string[]).includes(status)
      ? t(`admin.paymentStatus_${status as (typeof PAYMENT_STATUSES)[number]}`)
      : status;

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('admin.statisticsTitle')}</h1>
          <p className={pageStyles.subtitle}>{t('admin.statisticsSubtitle')}</p>
        </div>
      </header>

      {error != null && (
        <Alert tone="error">
          {t('admin.loadError')} {errorMessage(error)}
        </Alert>
      )}
      {!data && error == null && <p className={styles.muted}>{t('analytics.loading')}</p>}

      {totals && (
        <section aria-labelledby="admin-totals-title" className={styles.section}>
          <div>
            <h2 id="admin-totals-title" className={styles.sectionTitle}>
              {t('admin.totalsTitle')}
            </h2>
            <p className={styles.muted}>{t('admin.totalsHint')}</p>
          </div>
          <div className={styles.tiles}>
            <div className={styles.tile}>
              <span className={styles.tileLabel}>{t('admin.users')}</span>
              <span className={styles.tileValue}>{count(totals.users)}</span>
              <span className={styles.tileNote}>
                {t('admin.blockedCount', { count: totals.blockedUsers })} · {t('admin.adminCount', { count: totals.admins })}
              </span>
            </div>
            <div className={styles.tile}>
              <span className={styles.tileLabel}>{t('admin.groups')}</span>
              <span className={styles.tileValue}>{count(totals.groups)}</span>
            </div>
            <div className={styles.tile}>
              <span className={styles.tileLabel}>{t('admin.expenses')}</span>
              <span className={styles.tileValue}>{count(totals.personalExpenses + totals.groupExpenses)}</span>
              <span className={styles.tileNote}>
                {t('admin.personalCount', { count: totals.personalExpenses })} · {t('admin.groupCount', { count: totals.groupExpenses })}
              </span>
            </div>
            <div className={styles.tile}>
              <span className={styles.tileLabel}>{t('admin.settlements')}</span>
              <span className={styles.tileValue}>{count(totals.settledSettlements + totals.proposedSettlements)}</span>
              <span className={styles.tileNote}>
                {t('admin.settledCount', { count: totals.settledSettlements })} · {t('admin.proposedCount', { count: totals.proposedSettlements })}
              </span>
            </div>
            <div className={styles.tile}>
              <span className={styles.tileLabel}>{t('admin.payments')}</span>
              <span className={styles.tileValue}>{count(paymentTotal)}</span>
              {payments.length > 0 && (
                <span className={styles.tileNote}>
                  {payments.map(([status, value]) => `${statusName(status)} ${count(value)}`).join(' · ')}
                </span>
              )}
            </div>
          </div>
        </section>
      )}

      <section aria-labelledby="admin-activity-title" className={styles.section}>
        <div>
          <h2 id="admin-activity-title" className={styles.sectionTitle}>
            {t('admin.activityTitle')}
          </h2>
          <p className={styles.muted}>
            {data ? `${format.dateRange(data.from, data.to)} · ${t(`analytics.by_${unit}`)}` : t('admin.activityHint')}
          </p>
        </div>
        <PeriodFilter selection={selection} onSelect={select} />

        {data && (
          <div className={styles.multiples} aria-busy={loading}>
            {SERIES.map((series) => {
              const points = data.timeline.map((point) => ({
                periodStart: point.periodStart,
                periodEnd: point.periodEnd,
                total: point[series],
              }));
              const sum = points.reduce((total, point) => total + point.total, 0);
              return (
                <section key={series} className={styles.card} aria-labelledby={`admin-series-${series}`}>
                  <div className={styles.cardTop}>
                    <h3 id={`admin-series-${series}`} className={styles.cardTitle}>
                      {t(`admin.series_${series}`)}
                    </h3>
                    <span className={styles.cardValue}>{count(sum)}</span>
                  </div>
                  <SpendingChart
                    points={points}
                    granularity={data.granularity}
                    unit="count"
                    label={t('admin.seriesChartLabel', { name: t(`admin.series_${series}`) })}
                    height={170}
                    compact
                  />
                </section>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
