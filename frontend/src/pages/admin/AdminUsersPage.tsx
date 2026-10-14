import { useCallback, useEffect, useRef, useState } from 'react';
import { FileClock, Lock, Search, Unlock } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { adminApi } from '../../api/admin';
import type { AdminUser } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { isAdminRole } from '../../auth/roles';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { initials, memberColor } from '../../groups/groupUtils';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { PagePager } from './PagePager';
import pageStyles from '../Page.module.css';
import styles from './Admin.module.css';

const PAGE_SIZE = 20;
const STATUSES = ['all', 'active', 'blocked'] as const;
type Status = (typeof STATUSES)[number];

/** Filters live in the address (?q=ana&status=blocked&page=2), like on the expenses page. */
function readFilters(params: URLSearchParams) {
  const status = params.get('status') as Status | null;
  const page = Number.parseInt(params.get('page') ?? '', 10);
  return {
    search: (params.get('q') ?? '').slice(0, 100),
    status: status && STATUSES.includes(status) ? status : 'all',
    page: Number.isFinite(page) && page > 1 ? page : 1,
  };
}

type PendingAction = { kind: 'block' | 'unblock'; user: AdminUser } | null;

export function AdminUsersPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const errorMessage = useErrorMessage();
  const { user: me } = useAuth();

  const [params, setParams] = useSearchParams();
  const filters = readFilters(params);
  const update = useCallback(
    (changes: Partial<ReturnType<typeof readFilters>>) => {
      setParams(
        (current) => {
          const next = { ...readFilters(current), page: 1, ...changes };
          const query: Record<string, string> = {};
          if (next.search) query.q = next.search;
          if (next.status !== 'all') query.status = next.status;
          if (next.page > 1) query.page = String(next.page);
          return query;
        },
        { replace: true, preventScrollReset: true },
      );
    },
    [setParams],
  );

  // The list is asked for only after typing stops. Only a new pause in typing updates the
  // address, so going Back to an older search is not undone by the text in the box.
  const [searchText, setSearchText] = useState(filters.search);
  const debouncedSearch = useDebouncedValue(searchText.trim(), 350);
  const lastDebounced = useRef(debouncedSearch);
  useEffect(() => {
    if (debouncedSearch === lastDebounced.current) return;
    lastDebounced.current = debouncedSearch;
    if (debouncedSearch !== filters.search) update({ search: debouncedSearch });
  }, [debouncedSearch, filters.search, update]);

  const [version, setVersion] = useState(0);
  const key = JSON.stringify({ ...filters, version });
  const [loaded, setLoaded] = useState<{ key: string; users: AdminUser[] | null; error: unknown } | null>(null);
  const [pending, setPending] = useState<PendingAction>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    const { search, status, page } = JSON.parse(key) as ReturnType<typeof readFilters>;
    const controller = new AbortController();
    adminApi
      .users(
        { search: search || undefined, isBlocked: status === 'all' ? undefined : status === 'blocked', page, pageSize: PAGE_SIZE },
        controller.signal,
      )
      .then(
        (users) => setLoaded({ key, users, error: null }),
        (error: unknown) => {
          if (!controller.signal.aborted) setLoaded({ key, users: null, error });
        },
      );
    return () => controller.abort();
  }, [key]);

  const users = loaded?.users ?? null;
  const loading = loaded?.key !== key;
  const error = loaded?.key === key ? loaded.error : null;

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('admin.usersTitle')}</h1>
          <p className={pageStyles.subtitle}>{t('admin.usersSubtitle')}</p>
        </div>
      </header>

      <div className={styles.toolbar}>
        <label className={styles.search}>
          <Search size={18} aria-hidden="true" />
          <span className="visually-hidden">{t('admin.searchLabel')}</span>
          <input
            type="search"
            value={searchText}
            placeholder={t('admin.searchPlaceholder')}
            onChange={(event) => setSearchText(event.target.value)}
          />
        </label>
        <div className={styles.segmented} role="group" aria-label={t('admin.statusFilter')}>
          {STATUSES.map((status) => (
            <button
              key={status}
              type="button"
              aria-pressed={filters.status === status}
              className={filters.status === status ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
              onClick={() => update({ status })}
            >
              {t(`admin.status_${status}`)}
            </button>
          ))}
        </div>
      </div>

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && <Alert tone="success">{notice}</Alert>}
      </div>
      {error != null && (
        <Alert tone="error">
          {t('admin.usersLoadError')} {errorMessage(error)}
        </Alert>
      )}

      {users && users.length === 0 && (
        <p className={styles.emptyBox}>{filters.search || filters.status !== 'all' ? t('admin.noUsersFound') : t('admin.noUsers')}</p>
      )}

      {users && users.length > 0 && (
        <div className={styles.tableCard} aria-busy={loading}>
          <table className={styles.table}>
            <caption className="visually-hidden">{t('admin.usersTitle')}</caption>
            <thead>
              <tr>
                <th scope="col">{t('admin.colUser')}</th>
                <th scope="col">{t('admin.colRole')}</th>
                <th scope="col">{t('admin.colStatus')}</th>
                <th scope="col">{t('admin.colCreated')}</th>
                <th scope="col">
                  <span className="visually-hidden">{t('admin.colActions')}</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {users.map((user, index) => {
                const name = localize(`${user.firstName} ${user.lastName}`);
                const isMe = user.id === me?.id;
                const admin = isAdminRole(user.role);
                const auditQuery = new URLSearchParams({ userId: user.id, user: `${user.firstName} ${user.lastName}` });
                return (
                  <tr key={user.id} className={user.isBlocked ? styles.rowBlocked : undefined}>
                    <th scope="row">
                      <span className={styles.userCell}>
                        <Avatar size="sm" initials={localize(initials(user.firstName, user.lastName))} color={memberColor(index)} />
                        <span className={styles.userText}>
                          <span className={styles.userName}>
                            {name}
                            {isMe && <span className={styles.youBadge}>{t('admin.you')}</span>}
                          </span>
                          <span className={styles.userEmail}>{user.email}</span>
                        </span>
                      </span>
                    </th>
                    <td data-label={t('admin.colRole')}>
                      <span className={admin ? `${styles.badge} ${styles.badgeAdmin}` : styles.badge}>
                        {admin ? t('admin.roleAdmin') : t('admin.roleCustomer')}
                      </span>
                    </td>
                    <td data-label={t('admin.colStatus')}>
                      <span className={user.isBlocked ? `${styles.badge} ${styles.badgeBlocked}` : `${styles.badge} ${styles.badgeActive}`}>
                        {user.isBlocked ? t('admin.blocked') : t('admin.active')}
                      </span>
                      {user.mustChangePassword && <span className={styles.subNote}>{t('admin.mustChangePassword')}</span>}
                    </td>
                    <td data-label={t('admin.colCreated')}>{format.shortDate(user.createdAt)}</td>
                    <td className={styles.actionsCell}>
                      <Link
                        to={`/admin/audit?${auditQuery.toString()}`}
                        className={styles.iconLink}
                        aria-label={t('admin.activityOf', { name })}
                        title={t('admin.activityOf', { name })}
                      >
                        <FileClock size={18} aria-hidden="true" />
                      </Link>
                      {!isMe &&
                        (user.isBlocked ? (
                          <Button variant="secondary" className={styles.rowButton} onClick={() => setPending({ kind: 'unblock', user })}>
                            <Unlock size={16} aria-hidden="true" />
                            {t('admin.unblock')}
                          </Button>
                        ) : (
                          <Button variant="secondary" className={styles.rowButton} onClick={() => setPending({ kind: 'block', user })}>
                            <Lock size={16} aria-hidden="true" />
                            {t('admin.block')}
                          </Button>
                        ))}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {users && (users.length > 0 || filters.page > 1) && (
        <PagePager page={filters.page} hasNext={users.length === PAGE_SIZE} onChange={(page) => update({ page })} />
      )}

      {pending && (
        <ConfirmDialog
          title={pending.kind === 'block' ? t('admin.blockTitle') : t('admin.unblockTitle')}
          confirmLabel={pending.kind === 'block' ? t('admin.block') : t('admin.unblock')}
          busyLabel={t('admin.saving')}
          danger={pending.kind === 'block'}
          onClose={() => setPending(null)}
          onConfirm={async () => {
            const name = localize(`${pending.user.firstName} ${pending.user.lastName}`);
            if (pending.kind === 'block') await adminApi.block(pending.user.id);
            else await adminApi.unblock(pending.user.id);
            setPending(null);
            setNotice(pending.kind === 'block' ? t('admin.blockedNotice', { name }) : t('admin.unblockedNotice', { name }));
            setVersion((value) => value + 1);
          }}
        >
          <p>
            {pending.kind === 'block'
              ? t('admin.blockText', { name: localize(`${pending.user.firstName} ${pending.user.lastName}`), email: pending.user.email })
              : t('admin.unblockText', { name: localize(`${pending.user.firstName} ${pending.user.lastName}`), email: pending.user.email })}
          </p>
        </ConfirmDialog>
      )}
    </div>
  );
}
