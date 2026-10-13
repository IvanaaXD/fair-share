import { createElement, useEffect, useMemo, useState } from 'react';
import { Plus, Search, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/Alert';
import { AvatarStack } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/TextField';
import { balanceStatus, groupIcon, initials, memberColor, membersByJoinDate, type BalanceStatus } from '../../groups/groupUtils';
import { useMyGroups, type GroupSummary } from '../../groups/myGroupsStore';
import { useFormatters } from '../../i18n/format';
import { toLatin } from '../../i18n/transliterate';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { GroupFormDialog } from './GroupFormDialog';
import pageStyles from '../Page.module.css';
import styles from './Groups.module.css';

/** Other pages can open this one with the "new group" window already open (see OverviewPage). */
export interface GroupsPageState {
  createGroup?: boolean;
  /** A message to show after arriving here, e.g. "Group deleted." */
  notice?: string;
}

type Filter = 'all' | BalanceStatus;
const FILTERS: Filter[] = ['all', 'owe', 'owed', 'settled'];

/** "35,75 КМ + 10,00 €" - totals are added up per currency, never across currencies. */
function totalsByCurrency(groups: GroupSummary[], sign: -1 | 1) {
  const totals = new Map<string, number>();
  for (const group of groups) {
    const balance = group.myBalance;
    if (balance !== null && balanceStatus(balance) === (sign < 0 ? 'owe' : 'owed')) {
      totals.set(group.currency, (totals.get(group.currency) ?? 0) + Math.abs(balance));
    }
  }
  return [...totals.entries()];
}

export function GroupsPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const format = useFormatters();
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { groups, error, loading, reload } = useMyGroups();

  const arrivedWith = location.state as GroupsPageState | null;
  const [creating, setCreating] = useState(() => Boolean(arrivedWith?.createGroup));
  const [notice] = useState(() => arrivedWith?.notice ?? null);
  const [filter, setFilter] = useState<Filter>('all');
  const [search, setSearch] = useState('');

  // Forget the request from the other page, so a refresh does not repeat it.
  useEffect(() => {
    if (location.state) {
      void navigate('.', { replace: true, state: null });
    }
  }, [location.state, navigate]);

  const visible = useMemo(() => {
    // Both sides in Latin, so "tar", "тар" and "Tar" all find "Викенд на Тари".
    const term = toLatin(search.trim()).toLowerCase();
    return (groups ?? []).filter((group) => {
      if (filter !== 'all' && (group.myBalance === null || balanceStatus(group.myBalance) !== filter)) {
        return false;
      }
      return !term || toLatin(group.name).toLowerCase().includes(term);
    });
  }, [groups, filter, search]);

  const owe = totalsByCurrency(groups ?? [], -1);
  const owed = totalsByCurrency(groups ?? [], 1);
  const formatTotals = (totals: [string, number][]) =>
    totals.length === 0 ? format.money(0, user?.defaultCurrency ?? 'BAM') : totals.map(([currency, amount]) => format.money(amount, currency)).join(' + ');

  const newGroupButton = (
    <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setCreating(true)}>
      {t('groups.newGroup')}
    </Button>
  );

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('groups.title')}</h1>
          <p className={pageStyles.subtitle}>
            {groups ? t('groups.count', { count: groups.length }) : loading ? t('app.loading') : ''}
          </p>
        </div>
        {newGroupButton}
      </header>

      {notice && <Alert tone="success">{notice}</Alert>}

      {error !== null && (
        <Alert tone="error">
          {t('groups.loadError')} {errorMessage(error)}{' '}
          <button type="button" className={styles.inlineButton} onClick={() => void reload()}>
            {t('common.retry')}
          </button>
        </Alert>
      )}

      {groups && groups.length === 0 && (
        <section className={pageStyles.empty} aria-labelledby="groups-empty-title">
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            <Users size={28} strokeWidth={1.75} />
          </span>
          <h2 id="groups-empty-title" className={pageStyles.emptyTitle}>
            {t('groups.emptyTitle')}
          </h2>
          <p className={pageStyles.emptyText}>{t('groups.emptyText')}</p>
          <div className={pageStyles.actions}>{newGroupButton}</div>
        </section>
      )}

      {groups && groups.length > 0 && (
        <>
          <section className={styles.totals}>
            <div className={styles.totalCard}>
              <span className={styles.totalLabel}>{t('groups.totalOwe')}</span>
              <span className={`${styles.totalValue} ${styles.negative} money`}>{formatTotals(owe)}</span>
            </div>
            <div className={styles.totalCard}>
              <span className={styles.totalLabel}>{t('groups.totalOwed')}</span>
              <span className={`${styles.totalValue} ${styles.positive} money`}>{formatTotals(owed)}</span>
            </div>
          </section>

          <div className={styles.toolbar}>
            <div className={styles.segmented} role="group" aria-label={t('groups.filterLabel')}>
              {FILTERS.map((value) => (
                <button
                  key={value}
                  type="button"
                  className={filter === value ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
                  aria-pressed={filter === value}
                  onClick={() => setFilter(value)}
                >
                  {t(
                    value === 'all'
                      ? 'groups.filterAll'
                      : value === 'owe'
                        ? 'groups.filterOwe'
                        : value === 'owed'
                          ? 'groups.filterOwed'
                          : 'groups.filterSettled',
                  )}
                </button>
              ))}
            </div>
            <TextField
              compact
              type="search"
              label={t('groups.search')}
              placeholder={t('groups.searchPlaceholder')}
              leadingIcon={<Search size={18} />}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              className={styles.searchField}
            />
          </div>

          {visible.length > 0 ? (
            <ul className={styles.grid}>
              {visible.map((group) => (
                <li key={group.id}>
                  <GroupCard group={group} />
                </li>
              ))}
            </ul>
          ) : (
            <p className={styles.noMatches}>{t('groups.noMatches')}</p>
          )}
        </>
      )}

      {creating && (
        <GroupFormDialog
          group={null}
          defaultCurrency={user?.defaultCurrency ?? 'BAM'}
          onClose={() => setCreating(false)}
          onSaved={(group) => {
            setCreating(false);
            void reload();
            void navigate(`/groups/${group.id}`, { state: { notice: t('groups.created') } });
          }}
        />
      )}
    </div>
  );
}

function GroupCard({ group }: { group: GroupSummary }) {
  const { t } = useTranslation();
  const localize = useLocalizedText();
  const format = useFormatters();
  const status = group.myBalance === null ? null : balanceStatus(group.myBalance);

  const people = membersByJoinDate(group).map((member, index) => ({
    key: member.userId,
    initials: localize(initials(member.firstName, member.lastName)),
    color: memberColor(index),
  }));

  return (
    <Link to={`/groups/${group.id}`} className={styles.card}>
      <div className={styles.cardTop}>
        <span className={styles.cardIcon} aria-hidden="true">
          {createElement(groupIcon(group.name), { size: 22, strokeWidth: 1.75 })}
        </span>
        <span className={styles.cardText}>
          <span className={styles.cardName}>{localize(group.name)}</span>
          <span className={styles.cardMeta}>
            {group.description ? localize(group.description) : t('groups.createdOn', { date: format.longDate(group.createdAt) })}
          </span>
        </span>
      </div>
      <div className={styles.cardBottom}>
        <span className={styles.cardMembers}>
          <AvatarStack people={people} />
          <span>{t('groups.members', { count: group.members.length })}</span>
        </span>
        {status && group.myBalance !== null && (
          <span className={`${styles.chip} ${styles[`chip_${status}`]}`}>
            {status === 'owe'
              ? t('groups.oweAmount', { amount: format.money(Math.abs(group.myBalance), group.currency) })
              : status === 'owed'
                ? t('groups.owedAmount', { amount: format.money(group.myBalance, group.currency) })
                : t('groupStatus.settled')}
          </span>
        )}
      </div>
    </Link>
  );
}
