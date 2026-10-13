import { useEffect, useState } from 'react';
import { ArrowRight, Check, ChevronDown } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { settlementsApi } from '../../api/groupExpenses';
import type { GroupMember, Settlement } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { balanceStatus, initials, isSettledStatus, memberColor, SETTLED_TOLERANCE } from '../../groups/groupUtils';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Groups.module.css';

interface SettlementsPanelProps {
  groupId: string;
  currency: string;
  /** Changes whenever the settlements must be loaded again. */
  version: number;
  members: GroupMember[];
  colorOf: Map<string, string>;
  /** Current balance of every member; null when not available. */
  balanceOf: Map<string, number> | null;
  myId: string | undefined;
  /** Something changed (suggestion calculated, payment marked) - reload balances etc. */
  onChanged: (message: string) => void;
}

interface Loaded {
  key: string;
  settlements: Settlement[];
  error: unknown;
}

/**
 * Do the open suggestions still match the balances? Each suggestion moves money from the
 * debtor to the creditor; after all of them everyone should be at zero. If an expense was
 * added or changed after the suggestion was calculated, that no longer holds.
 */
function isStale(proposed: Settlement[], balanceOf: Map<string, number>): boolean {
  const afterPayments = new Map(balanceOf);
  for (const settlement of proposed) {
    afterPayments.set(settlement.debtorUserId, (afterPayments.get(settlement.debtorUserId) ?? 0) + settlement.amount);
    afterPayments.set(settlement.creditorUserId, (afterPayments.get(settlement.creditorUserId) ?? 0) - settlement.amount);
  }
  return [...afterPayments.values()].some((value) => Math.abs(value) > SETTLED_TOLERANCE * 2);
}

/** "Who pays whom" - the debt simplification result, and marking payments as done. */
export function SettlementsPanel({ groupId, currency, version, members, colorOf, balanceOf, myId, onChanged }: SettlementsPanelProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();

  const key = `${groupId}:${version}`;
  const [loaded, setLoaded] = useState<Loaded | null>(null);
  const [useExact, setUseExact] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirming, setConfirming] = useState<Settlement | null>(null);
  const [showSettled, setShowSettled] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    settlementsApi.list(groupId, undefined, controller.signal).then(
      (settlements) => setLoaded({ key, settlements, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded({ key, settlements: [], error });
      },
    );
    return () => controller.abort();
  }, [groupId, key]);

  const memberById = new Map(members.map((member) => [member.userId, member]));
  const shortName = (userId: string, fallback: string) => {
    if (userId === myId) return t('group.you');
    const member = memberById.get(userId);
    return localize(member ? member.firstName : fallback);
  };
  const avatarOf = (userId: string, fallback: string) => {
    const member = memberById.get(userId);
    const [first = '', last = ''] = fallback.split(/\s+/);
    return (
      <Avatar
        size="sm"
        initials={localize(member ? initials(member.firstName, member.lastName) : initials(first, last))}
        color={colorOf.get(userId) ?? memberColor(5)}
      />
    );
  };
  const money = (amount: number) => format.money(amount, currency);

  const settlements = loaded?.settlements ?? [];
  const proposed = settlements.filter((settlement) => !isSettledStatus(settlement.status));
  const settled = settlements.filter((settlement) => isSettledStatus(settlement.status));
  const everyoneAtZero = balanceOf ? [...balanceOf.values()].every((value) => balanceStatus(value) === 'settled') : false;
  const stale = balanceOf !== null && proposed.length > 0 && isStale(proposed, balanceOf);

  async function generate() {
    setGenerating(true);
    setActionError(null);
    try {
      await settlementsApi.generate(groupId, useExact);
      onChanged(t('settlements.generated'));
    } catch (error) {
      setActionError(errorMessage(error));
    } finally {
      setGenerating(false);
    }
  }

  return (
    <section className={styles.panel} aria-labelledby="settlements-title">
      <div>
        <h2 id="settlements-title" className={styles.panelTitle}>
          {t('settlements.title')}
        </h2>
        <p className={styles.panelHint}>{t('settlements.text')}</p>
      </div>

      {loaded?.error != null && <Alert tone="error">{t('settlements.loadError')} {errorMessage(loaded.error)}</Alert>}
      {actionError && <Alert tone="error">{actionError}</Alert>}
      {stale && <Alert tone="info">{t('settlements.stale')}</Alert>}

      {proposed.length > 0 ? (
        <ul className={styles.transferList}>
          {proposed.map((settlement) => {
            const from = shortName(settlement.debtorUserId, settlement.debtorName);
            const to = shortName(settlement.creditorUserId, settlement.creditorName);
            const involvesMe = settlement.debtorUserId === myId || settlement.creditorUserId === myId;
            return (
              <li key={settlement.id} className={involvesMe ? `${styles.transfer} ${styles.transferMine}` : styles.transfer}>
                <span className="visually-hidden">
                  {t('settlements.transferLabel', { from, to, amount: money(settlement.amount) })}
                </span>
                <span className={styles.transferPeople} aria-hidden="true">
                  {avatarOf(settlement.debtorUserId, settlement.debtorName)}
                  <ArrowRight size={16} className={styles.transferArrow} />
                  {avatarOf(settlement.creditorUserId, settlement.creditorName)}
                  <span className={styles.transferNames}>{t('settlements.transfer', { from, to })}</span>
                </span>
                <span className={`${styles.transferAmount} money`} aria-hidden="true">
                  {money(settlement.amount)}
                </span>
                {involvesMe && (
                  <Button variant="secondary" className={styles.transferButton} onClick={() => setConfirming(settlement)}>
                    <Check size={16} aria-hidden="true" />
                    {t('settlements.markPaid')}
                  </Button>
                )}
              </li>
            );
          })}
        </ul>
      ) : (
        loaded && <p className={styles.panelHint}>{everyoneAtZero ? t('settlements.allSettled') : t('settlements.empty')}</p>
      )}

      {!everyoneAtZero && (
        <div className={styles.generate}>
          <label className={styles.exactOption}>
            <input type="checkbox" checked={useExact} onChange={(event) => setUseExact(event.target.checked)} />
            <span>
              {t('settlements.exact')}
              <span className={styles.exactHint}>{t('settlements.exactHint')}</span>
            </span>
          </label>
          <Button variant={proposed.length > 0 && !stale ? 'secondary' : 'primary'} onClick={() => void generate()} disabled={generating}>
            {generating ? t('settlements.generating') : proposed.length > 0 ? t('settlements.regenerate') : t('settlements.generate')}
          </Button>
        </div>
      )}

      {settled.length > 0 && (
        <div>
          <button
            type="button"
            className={styles.disclosure}
            aria-expanded={showSettled}
            onClick={() => setShowSettled((value) => !value)}
          >
            <ChevronDown size={16} aria-hidden="true" className={showSettled ? styles.rotated : undefined} />
            {t('settlements.settledTitle', { count: settled.length })}
          </button>
          {showSettled && (
            <ul className={styles.settledList}>
              {settled.map((settlement) => (
                <li key={settlement.id}>
                  <span>
                    {t('settlements.transfer', {
                      from: shortName(settlement.debtorUserId, settlement.debtorName),
                      to: shortName(settlement.creditorUserId, settlement.creditorName),
                    })}
                  </span>
                  <span className="money">{money(settlement.amount)}</span>
                  <span className={styles.settledDate}>{format.shortDate(settlement.createdAt)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {confirming && (
        <ConfirmDialog
          title={t('settlements.confirmTitle')}
          confirmLabel={t('settlements.confirm')}
          busyLabel={t('settlements.confirming')}
          onClose={() => setConfirming(null)}
          onConfirm={async () => {
            await settlementsApi.settle(groupId, confirming.id);
            setConfirming(null);
            onChanged(t('settlements.paid'));
          }}
        >
          <p>
            {t('settlements.confirmText', {
              from: shortName(confirming.debtorUserId, confirming.debtorName),
              to: shortName(confirming.creditorUserId, confirming.creditorName),
              amount: money(confirming.amount),
            })}
          </p>
        </ConfirmDialog>
      )}
    </section>
  );
}
