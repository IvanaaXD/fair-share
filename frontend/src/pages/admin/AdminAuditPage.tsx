import { useEffect, useState } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { adminApi } from '../../api/admin';
import type { AuditLogEntry } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { AUDIT_ENTITIES, isAuditEntity, isAuditVerb, splitAction, utcMoment } from './auditLabels';
import { PagePager } from './PagePager';
import pageStyles from '../Page.module.css';
import styles from './Admin.module.css';

const PAGE_SIZE = 50;
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
/** User ids are GUIDs; anything else typed into the address is ignored. */
const ID_PATTERN = /^[\w-]{1,64}$/;

interface AuditFilters {
  userId: string;
  /** The user's name, only for the filter chip (the address is the only place it comes from). */
  userName: string;
  entity: string;
  from: string;
  to: string;
  page: number;
}

function readFilters(params: URLSearchParams): AuditFilters {
  const date = (key: string) => {
    const value = params.get(key) ?? '';
    return DATE_PATTERN.test(value) ? value : '';
  };
  const userId = params.get('userId') ?? '';
  const page = Number.parseInt(params.get('page') ?? '', 10);
  return {
    userId: ID_PATTERN.test(userId) ? userId : '',
    userName: (params.get('user') ?? '').slice(0, 120),
    entity: (params.get('entity') ?? '').slice(0, 60),
    from: date('from'),
    to: date('to'),
    page: Number.isFinite(page) && page > 1 ? page : 1,
  };
}

function writeFilters(filters: AuditFilters): Record<string, string> {
  const query: Record<string, string> = {};
  if (filters.userId) {
    query.userId = filters.userId;
    if (filters.userName) query.user = filters.userName;
  }
  if (filters.entity) query.entity = filters.entity;
  if (filters.from) query.from = filters.from;
  if (filters.to) query.to = filters.to;
  if (filters.page > 1) query.page = String(filters.page);
  return query;
}

/** Who did what and when - every successful change in the system, newest first. */
export function AdminAuditPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const errorMessage = useErrorMessage();

  const [params, setParams] = useSearchParams();
  const filters = readFilters(params);
  const update = (changes: Partial<AuditFilters>) =>
    setParams(writeFilters({ ...filters, page: 1, ...changes }), { replace: true, preventScrollReset: true });

  const key = JSON.stringify(filters);
  const [loaded, setLoaded] = useState<{ key: string; entries: AuditLogEntry[] | null; error: unknown } | null>(null);

  useEffect(() => {
    const current = JSON.parse(key) as AuditFilters;
    const controller = new AbortController();
    adminApi
      .auditLogs(
        {
          userId: current.userId || undefined,
          entityType: current.entity || undefined,
          from: current.from || undefined,
          to: current.to || undefined,
          page: current.page,
          pageSize: PAGE_SIZE,
        },
        controller.signal,
      )
      .then(
        (entries) => setLoaded({ key, entries, error: null }),
        (error: unknown) => {
          if (!controller.signal.aborted) setLoaded({ key, entries: null, error });
        },
      );
    return () => controller.abort();
  }, [key]);

  const entries = loaded?.entries ?? null;
  const loading = loaded?.key !== key;
  const error = loaded?.key === key ? loaded.error : null;
  const dateError = filters.from && filters.to && filters.from > filters.to;
  const hasFilters = Boolean(filters.userId || filters.entity || filters.from || filters.to);

  const entityName = (entity: string) => (isAuditEntity(entity) ? t(`auditEntity.${entity}`) : entity);
  const actionName = (action: string) => {
    const [entity, verb] = splitAction(action);
    const verbText = isAuditVerb(verb) ? t(`auditVerb.${verb}`) : verb;
    return verb ? `${entityName(entity)} · ${verbText}` : entityName(entity);
  };

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('admin.auditTitle')}</h1>
          <p className={pageStyles.subtitle}>{t('admin.auditSubtitle')}</p>
        </div>
      </header>

      <div className={styles.toolbar}>
        <label className={styles.field}>
          <span>{t('admin.auditEntity')}</span>
          <select value={filters.entity} onChange={(event) => update({ entity: event.target.value })}>
            <option value="">{t('admin.auditAllEntities')}</option>
            {AUDIT_ENTITIES.map((entity) => (
              <option key={entity} value={entity}>
                {t(`auditEntity.${entity}`)}
              </option>
            ))}
          </select>
        </label>
        <label className={styles.field}>
          <span>{t('analytics.from')}</span>
          <input
            type="date"
            value={filters.from}
            max={filters.to || undefined}
            onChange={(event) => update({ from: event.target.value })}
            aria-invalid={dateError ? true : undefined}
          />
        </label>
        <label className={styles.field}>
          <span>{t('analytics.to')}</span>
          <input
            type="date"
            value={filters.to}
            min={filters.from || undefined}
            onChange={(event) => update({ to: event.target.value })}
            aria-invalid={dateError ? true : undefined}
          />
        </label>
        {hasFilters && (
          <button type="button" className={styles.clearButton} onClick={() => setParams({}, { replace: true })}>
            {t('admin.clearFilters')}
          </button>
        )}
      </div>

      {filters.userId && (
        <div className={styles.chips}>
          <span className={styles.chip}>
            {t('admin.auditUser', { name: filters.userName ? localize(filters.userName) : t('admin.oneUser') })}
            <button
              type="button"
              className={styles.chipRemove}
              aria-label={t('admin.removeUserFilter')}
              onClick={() => update({ userId: '', userName: '' })}
            >
              <X size={14} aria-hidden="true" />
            </button>
          </span>
        </div>
      )}

      {dateError && <Alert tone="error">{t('analytics.errorOrder')}</Alert>}
      {error != null && (
        <Alert tone="error">
          {t('admin.auditLoadError')} {errorMessage(error)}
        </Alert>
      )}

      {entries && entries.length === 0 && (
        <p className={styles.emptyBox}>{hasFilters ? t('admin.auditNothingFound') : t('admin.auditEmpty')}</p>
      )}

      {entries && entries.length > 0 && (
        <div className={styles.tableCard} aria-busy={loading}>
          <table className={styles.table}>
            <caption className="visually-hidden">{t('admin.auditTitle')}</caption>
            <thead>
              <tr>
                <th scope="col">{t('admin.colTime')}</th>
                <th scope="col">{t('admin.colUser')}</th>
                <th scope="col">{t('admin.colAction')}</th>
                <th scope="col">{t('admin.colObject')}</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => {
                const name = entry.userFullName ? localize(entry.userFullName) : entry.userEmail;
                return (
                  <tr key={entry.id}>
                    <td data-label={t('admin.colTime')} className={styles.timeCell}>
                      <time dateTime={utcMoment(entry.timestamp)}>{format.dateTime(utcMoment(entry.timestamp))}</time>
                    </td>
                    <th scope="row" data-label={t('admin.colUser')}>
                      <button
                        type="button"
                        className={styles.linkButton}
                        title={t('admin.filterByUser')}
                        onClick={() => update({ userId: entry.userId, userName: entry.userFullName })}
                      >
                        {name}
                      </button>
                      <span className={styles.userEmail}>{entry.userEmail}</span>
                    </th>
                    <td data-label={t('admin.colAction')}>
                      <span className={styles.action}>{actionName(entry.action)}</span>
                      <code className={styles.code}>{entry.action}</code>
                    </td>
                    <td data-label={t('admin.colObject')}>
                      {entityName(entry.entityType)}
                      {entry.entityId && (
                        <code className={styles.code} title={entry.entityId}>
                          {entry.entityId.slice(0, 8)}
                        </code>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {entries && (entries.length > 0 || filters.page > 1) && (
        <PagePager page={filters.page} hasNext={entries.length === PAGE_SIZE} onChange={(page) => update({ page })} />
      )}
    </div>
  );
}
