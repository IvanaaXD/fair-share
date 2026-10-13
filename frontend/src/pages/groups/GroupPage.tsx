import { useCallback, useEffect, useState } from 'react';
import { ChevronLeft, LogOut, Pencil, Plus, Trash2, UserMinus, UserPlus } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate, useParams } from 'react-router';
import { ApiError } from '../../api/ApiError';
import { groupsApi } from '../../api/groups';
import type { Group, GroupExpense, GroupMember, MemberBalance } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/Alert';
import { Avatar, AvatarStack } from '../../components/ui/Avatar';
import { Button, ButtonLink } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { balanceStatus, initials, isOwnerRole, memberColor, membersByJoinDate } from '../../groups/groupUtils';
import { useMyGroups } from '../../groups/myGroupsStore';
import { useCategories } from '../../hooks/useCategories';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { GroupExpenseDialog, type GroupExpenseChange } from './GroupExpenseDialog';
import { GroupExpenseList } from './GroupExpenseList';
import { GroupFormDialog } from './GroupFormDialog';
import type { GroupsPageState } from './GroupsPage';
import { InviteMemberDialog } from './InviteMemberDialog';
import { SettlementsPanel } from './SettlementsPanel';
import pageStyles from '../Page.module.css';
import styles from './Groups.module.css';

interface Loaded {
  groupId: string;
  group: Group | null;
  /** null when the balances could not be loaded (the page still works without them). */
  balances: MemberBalance[] | null;
  error: unknown;
}

type Dialog =
  | { kind: 'edit' }
  | { kind: 'invite' }
  | { kind: 'remove'; member: GroupMember }
  | { kind: 'leave' }
  | { kind: 'delete' }
  | { kind: 'expense'; expense: GroupExpense | null }
  | null;

/** One group: who is in it, how everyone stands, and the group's settings. */
export function GroupPage() {
  const { groupId = '' } = useParams();
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { reload: reloadMyGroups } = useMyGroups();
  const { categories } = useCategories();

  const [loaded, setLoaded] = useState<Loaded | null>(null);
  const [version, setVersion] = useState(0);
  const [dialog, setDialog] = useState<Dialog>(null);
  const [notice, setNotice] = useState<{ id: number; text: string } | null>(() => {
    const text = (location.state as { notice?: string } | null)?.notice;
    return text ? { id: 0, text } : null;
  });

  useEffect(() => {
    if (location.state) {
      void navigate('.', { replace: true, state: null });
    }
  }, [location.state, navigate]);

  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      groupsApi.get(groupId, controller.signal),
      groupsApi.balances(groupId, controller.signal).catch((error: unknown) => {
        if (controller.signal.aborted) throw error;
        return null;
      }),
    ]).then(
      ([group, balances]) => setLoaded({ groupId, group, balances, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setLoaded({ groupId, group: null, balances: null, error });
        }
      },
    );
    return () => controller.abort();
  }, [groupId, version]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 5000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  /** After a change: this page, and the list in the sidebar (member counts, balances). */
  const refresh = useCallback(
    (message?: string) => {
      setVersion((value) => value + 1);
      void reloadMyGroups();
      if (message) {
        setNotice({ id: Date.now(), text: message });
      }
    },
    [reloadMyGroups],
  );

  const current = loaded?.groupId === groupId ? loaded : null;

  if (!current) {
    return (
      <div className={pageStyles.page}>
        <p className={pageStyles.subtitle}>{t('app.loading')}</p>
      </div>
    );
  }

  if (!current.group) {
    const missing = current.error instanceof ApiError && (current.error.status === 403 || current.error.status === 404);
    return (
      <div className={pageStyles.page}>
        <Link to="/groups" className={styles.backLink}>
          <ChevronLeft size={16} aria-hidden="true" />
          {t('group.back')}
        </Link>
        {missing ? (
          <section className={pageStyles.empty}>
            <h1 className={pageStyles.emptyTitle}>{t('group.notFoundTitle')}</h1>
            <p className={pageStyles.emptyText}>{t('group.notFoundText')}</p>
            <div className={pageStyles.actions}>
              <ButtonLink to="/groups" variant="secondary">
                {t('group.back')}
              </ButtonLink>
            </div>
          </section>
        ) : (
          <Alert tone="error">
            {errorMessage(current.error)}{' '}
            <button type="button" className={styles.inlineButton} onClick={() => setVersion((value) => value + 1)}>
              {t('common.retry')}
            </button>
          </Alert>
        )}
      </div>
    );
  }

  const group = current.group;
  const members = membersByJoinDate(group);
  const colorOf = new Map(members.map((member, index) => [member.userId, memberColor(index)]));
  const me = members.find((member) => member.userId === user?.id);
  const iAmOwner = me ? isOwnerRole(me.role) : false;
  const owner = members.find((member) => isOwnerRole(member.role));
  const fullName = (member: { firstName: string; lastName: string }) => localize(`${member.firstName} ${member.lastName}`);
  const balanceOf = new Map((current.balances ?? []).map((balance) => [balance.userId, balance.netBalance]));
  const myBalance = user ? balanceOf.get(user.id) : undefined;
  const money = (amount: number) => format.money(amount, group.currency);

  const goToGroups = (message: string) => {
    void reloadMyGroups();
    const state: GroupsPageState = { notice: message };
    void navigate('/groups', { state });
  };

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={styles.groupHeading}>
          <Link to="/groups" className={styles.backLink}>
            <ChevronLeft size={16} aria-hidden="true" />
            {t('group.back')}
          </Link>
          <h1 className={pageStyles.title}>{localize(group.name)}</h1>
          {group.description && <p className={styles.groupDescription}>{localize(group.description)}</p>}
          <div className={styles.groupSummary}>
            <AvatarStack
              size="md"
              max={5}
              people={members.map((member) => ({
                key: member.userId,
                initials: localize(initials(member.firstName, member.lastName)),
                color: colorOf.get(member.userId) ?? memberColor(0),
              }))}
            />
            <span>
              {t('group.summary', {
                members: t('groups.members', { count: members.length }),
                owner: owner ? fullName(owner) : '–',
                currency: group.currency,
              })}
            </span>
          </div>
        </div>
        <div className={styles.headerActions}>
          {iAmOwner && (
            <Button variant="secondary" icon={<Pencil size={17} aria-hidden="true" />} onClick={() => setDialog({ kind: 'edit' })}>
              {t('group.edit')}
            </Button>
          )}
          <Button variant="secondary" icon={<UserPlus size={18} aria-hidden="true" />} onClick={() => setDialog({ kind: 'invite' })}>
            {t('group.invite')}
          </Button>
          <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setDialog({ kind: 'expense', expense: null })}>
            {t('groupExpense.add')}
          </Button>
        </div>
      </header>

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && (
          <Alert key={notice.id} tone="success">
            {notice.text}
          </Alert>
        )}
      </div>

      <MyBalanceCard balance={myBalance} money={money} />

      <div className={styles.columns}>
        <div className={styles.column}>


          <section className={styles.panel} aria-labelledby="expenses-title">
            <h2 id="expenses-title" className={styles.panelTitle}>
              {t('group.expensesTitle')}
            </h2>
            <GroupExpenseList
              groupId={group.id}
              currency={group.currency}
              version={version}
              myId={user?.id}
              colorOf={colorOf}
              onOpen={(expense) => setDialog({ kind: 'expense', expense })}
            />
          </section>

          <section className={styles.panel} aria-labelledby="members-title">
            <h2 id="members-title" className={styles.panelTitle}>
              {t('group.membersTitle')}
            </h2>
            <ul className={styles.memberList}>
              {members.map((member) => {
                const isMe = member.userId === user?.id;
                return (
                  <li key={member.userId} className={styles.member}>
                    <Avatar
                      initials={localize(initials(member.firstName, member.lastName))}
                      color={colorOf.get(member.userId) ?? memberColor(0)}
                    />
                    <span className={styles.memberText}>
                      <span className={styles.memberName}>
                        {fullName(member)} {isMe && <span className={styles.muted}>{t('group.youSuffix')}</span>}
                        {isOwnerRole(member.role) && <span className={styles.roleBadge}>{t('group.owner')}</span>}
                      </span>
                      <span className={styles.memberMeta}>
                        {member.email} · {t('group.joined', { date: format.longDate(member.joinedAt) })}
                      </span>
                    </span>
                    {iAmOwner && !isMe && (
                      <button
                        type="button"
                        className={styles.iconButton}
                        aria-label={t('group.removeMember', { name: fullName(member) })}
                        title={t('group.removeMember', { name: fullName(member) })}
                        onClick={() => setDialog({ kind: 'remove', member })}
                      >
                        <UserMinus size={18} aria-hidden="true" />
                      </button>
                    )}
                  </li>
                );
              })}
            </ul>
          </section>
        </div>

        <div className={styles.column}>
          <SettlementsPanel
            groupId={group.id}
            currency={group.currency}
            version={version}
            members={members}
            colorOf={colorOf}
            balanceOf={current.balances ? balanceOf : null}
            myId={user?.id}
            onChanged={(message) => refresh(message)}
          />

          {current.balances && (
            <BalancesPanel
              members={members}
              balanceOf={balanceOf}
              colorOf={colorOf}
              myId={user?.id}
              money={money}
              fullName={fullName}
            />
          )}

          <section className={styles.panel} aria-labelledby="settings-title">
            <h2 id="settings-title" className={styles.panelTitle}>
              {t('group.settingsTitle')}
            </h2>
            <div className={styles.settingRow}>
              <div>
                <p className={styles.settingName}>{t('group.leave')}</p>
                <p className={styles.settingHint}>{t('group.leaveHint')}</p>
              </div>
              <Button variant="secondary" icon={<LogOut size={17} aria-hidden="true" />} onClick={() => setDialog({ kind: 'leave' })}>
                {t('group.leave')}
              </Button>
            </div>
            {iAmOwner && (
              <div className={styles.settingRow}>
                <div>
                  <p className={styles.settingName}>{t('group.delete')}</p>
                  <p className={styles.settingHint}>{t('group.deleteHint')}</p>
                </div>
                <Button variant="danger" icon={<Trash2 size={17} aria-hidden="true" />} onClick={() => setDialog({ kind: 'delete' })}>
                  {t('group.delete')}
                </Button>
              </div>
            )}
          </section>
        </div>
      </div>

      {dialog?.kind === 'expense' && user && (
        <GroupExpenseDialog
          group={group}
          expense={dialog.expense}
          categories={categories}
          currentUserId={user.id}
          onClose={() => setDialog(null)}
          onDone={(change: GroupExpenseChange) => {
            setDialog(null);
            refresh(t(`groupExpense.${change}`));
          }}
          onChanged={() => refresh()}
        />
      )}

      {dialog?.kind === 'edit' && (
        <GroupFormDialog
          group={group}
          defaultCurrency={group.currency}
          onClose={() => setDialog(null)}
          onSaved={() => {
            setDialog(null);
            refresh(t('group.updated'));
          }}
        />
      )}

      {dialog?.kind === 'invite' && (
        <InviteMemberDialog
          groupId={group.id}
          onClose={() => setDialog(null)}
          onAdded={(member) => {
            setDialog(null);
            refresh(t('group.memberAdded', { name: fullName(member) }));
          }}
        />
      )}

      {dialog?.kind === 'remove' && (
        <ConfirmDialog
          title={t('confirm.removeTitle')}
          confirmLabel={t('confirm.removeConfirm')}
          busyLabel={t('confirm.removing')}
          danger
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await groupsApi.removeMember(group.id, dialog.member.userId);
            setDialog(null);
            refresh(t('group.memberRemoved', { name: fullName(dialog.member) }));
          }}
        >
          <p>{t('confirm.removeText', { name: fullName(dialog.member) })}</p>
        </ConfirmDialog>
      )}

      {dialog?.kind === 'leave' && (
        <ConfirmDialog
          title={t('confirm.leaveTitle')}
          confirmLabel={t('confirm.leaveConfirm')}
          busyLabel={t('confirm.leaving')}
          danger
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await groupsApi.leave(group.id);
            goToGroups(t('groups.left'));
          }}
        >
          <p>{t('confirm.leaveText', { name: localize(group.name) })}</p>
          {iAmOwner && members.length > 1 && <p>{t('confirm.leaveOwnerText')}</p>}
        </ConfirmDialog>
      )}

      {dialog?.kind === 'delete' && (
        <ConfirmDialog
          title={t('confirm.deleteTitle')}
          confirmLabel={t('confirm.deleteConfirm')}
          busyLabel={t('confirm.deleting')}
          danger
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await groupsApi.remove(group.id);
            goToGroups(t('groups.deleted'));
          }}
        >
          <p>{t('confirm.deleteText', { name: localize(group.name) })}</p>
        </ConfirmDialog>
      )}
    </div>
  );
}

/** The signed-in user's own position, in the dark band from the design. */
function MyBalanceCard({ balance, money }: { balance: number | undefined; money: (amount: number) => string }) {
  const { t } = useTranslation();
  const status = balance === undefined ? null : balanceStatus(balance);

  return (
    <section className={styles.hero} aria-labelledby="my-balance-title">
      <h2 id="my-balance-title" className={styles.heroTitle}>
        {t('group.myBalanceTitle')}
      </h2>
      {status === null && <p className={styles.heroText}>{t('group.balanceUnknown')}</p>}
      {status === 'settled' && <p className={styles.heroText}>{t('group.settled')}</p>}
      {(status === 'owe' || status === 'owed') && balance !== undefined && (
        <p className={styles.heroAmountRow}>
          <span className={styles.heroText}>{status === 'owe' ? t('group.youOwe') : t('group.youAreOwed')}</span>
          <span className={`${styles.heroAmount} ${status === 'owe' ? styles.heroOwe : styles.heroOwed} money`}>
            {money(Math.abs(balance))}
          </span>
        </p>
      )}
    </section>
  );
}

interface BalancesPanelProps {
  members: GroupMember[];
  balanceOf: Map<string, number>;
  colorOf: Map<string, string>;
  myId: string | undefined;
  money: (amount: number) => string;
  fullName: (member: GroupMember) => string;
}

/**
 * Every member's balance as a bar from the middle line: to the left (amber) what they owe,
 * to the right (petrol) what the others owe them. Bars are relative to the largest balance.
 */
function BalancesPanel({ members, balanceOf, colorOf, myId, money, fullName }: BalancesPanelProps) {
  const { t } = useTranslation();
  const localize = useLocalizedText();
  const largest = Math.max(0, ...members.map((member) => Math.abs(balanceOf.get(member.userId) ?? 0)));
  const allSettled = members.every((member) => balanceStatus(balanceOf.get(member.userId) ?? 0) === 'settled');

  return (
    <section className={styles.panel} aria-labelledby="balances-title">
      <div>
        <h2 id="balances-title" className={styles.panelTitle}>
          {t('group.balancesTitle')}
        </h2>
        <p className={styles.panelHint}>{allSettled ? t('group.balancesEmpty') : t('group.balancesHint')}</p>
      </div>
      <ul className={styles.balanceList}>
        {members.map((member) => {
          const balance = balanceOf.get(member.userId) ?? 0;
          const status = balanceStatus(balance);
          const width = largest > 0 ? `${(Math.abs(balance) / largest) * 100}%` : '0%';
          const name = member.userId === myId ? t('group.you') : localize(member.firstName);
          const description =
            status === 'owe'
              ? t('group.balanceOwes', { name: fullName(member), amount: money(Math.abs(balance)) })
              : status === 'owed'
                ? t('group.balanceIsOwed', { name: fullName(member), amount: money(balance) })
                : t('group.balanceSettled', { name: fullName(member) });

          return (
            <li key={member.userId} className={styles.balanceRow}>
              <span className="visually-hidden">{description}</span>
              <span className={styles.balanceName} aria-hidden="true">
                <Avatar
                  size="sm"
                  initials={localize(initials(member.firstName, member.lastName))}
                  color={colorOf.get(member.userId) ?? memberColor(0)}
                />
                <span className={member.userId === myId ? styles.balanceMe : undefined}>{name}</span>
              </span>
              <span className={styles.barLeft} aria-hidden="true">
                {status === 'owe' && <span className={styles.barOwe} style={{ width }} />}
              </span>
              <span className={styles.barRight} aria-hidden="true">
                {status === 'owed' && <span className={styles.barOwed} style={{ width }} />}
              </span>
              <span className={`${styles.balanceAmount} ${styles[`amount_${status}`]} money`} aria-hidden="true">
                {status === 'settled' ? money(0) : (status === 'owe' ? '−' : '+') + money(Math.abs(balance))}
              </span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
