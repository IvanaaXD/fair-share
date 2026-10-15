import { useState } from 'react';
import { ArrowRight, BellRing, Check, ChevronDown, CreditCard, QrCode, ScanLine } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { settlementsApi } from '../../api/groupExpenses';
import type { GroupMember, Settlement } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { balanceStatus, initials, isSettledStatus, memberColor, settlementsAreStale } from '../../groups/groupUtils';
import type { PayTab } from '../payments/PayDialog';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { ReminderDialog } from './ReminderDialog';
import styles from './Groups.module.css';

const REMINDER_INTERVAL_MS = 24 * 60 * 60 * 1000;

/** The backend stores UTC; a value without a zone is read as UTC too. */
function asUtc(value: string): string {
  return /[zZ]|[+-]\d\d:?\d\d$/.test(value) ? value : `${value}Z`;
}

interface SettlementsPanelProps {
  groupId: string;
  /** For the text of a debt reminder. */
  groupName: string;
  currency: string;
  /** Loaded by the group page (it also needs them for the "your balance" band); null = failed. */
  settlements: Settlement[] | null;
  loadError: unknown;
  members: GroupMember[];
  colorOf: Map<string, string>;
  /** Current balance of every member; null when not available. */
  balanceOf: Map<string, number> | null;
  myId: string | undefined;
  /** Something changed (suggestion calculated, payment marked) - reload balances etc. */
  onChanged: (message: string) => void;
  /** Opens the payment window (QR code or card) for a suggestion. */
  onPay: (settlement: Settlement, tab: PayTab) => void;
  onScan: () => void;
}

/** "Who pays whom" - the debt simplification result, and marking payments as done. */
export function SettlementsPanel({
  groupId,
  groupName,
  currency,
  settlements: loadedSettlements,
  loadError,
  members,
  colorOf,
  balanceOf,
  myId,
  onChanged,
  onPay,
  onScan,
}: SettlementsPanelProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();

  const [useExact, setUseExact] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirming, setConfirming] = useState<Settlement | null>(null);
  const [showSettled, setShowSettled] = useState(false);
  const [reminding, setReminding] = useState<Settlement | null>(null);
  // The moment the panel was shown; enough to tell whether the last reminder is older than 24 h.
  const [now] = useState(() => Date.now());

  /** When the next reminder is possible, or null if one can be sent now. */
  const nextReminderAt = (settlement: Settlement): number | null => {
    if (!settlement.lastReminderSentAt) return null;
    const next = Date.parse(asUtc(settlement.lastReminderSentAt)) + REMINDER_INTERVAL_MS;
    return next > now ? next : null;
  };

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

  const settlements = loadedSettlements ?? [];
  const proposed = settlements.filter((settlement) => !isSettledStatus(settlement.status));
  const settled = settlements.filter((settlement) => isSettledStatus(settlement.status));
  const everyoneAtZero = balanceOf ? [...balanceOf.values()].every((value) => balanceStatus(value) === 'settled') : false;
  const stale = balanceOf !== null && proposed.length > 0 && settlementsAreStale(proposed, balanceOf);

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

      {loadError != null && <Alert tone="error">{t('settlements.loadError')} {errorMessage(loadError)}</Alert>}
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
                  <span className={styles.transferActions}>
                    {settlement.debtorUserId === myId ? (
                      <>
                        <Button className={styles.transferButton} onClick={() => onPay(settlement, 'card')} disabled={stale}>
                          <CreditCard size={16} aria-hidden="true" />
                          {t('settlements.pay')}
                        </Button>
                        <Button variant="secondary" className={styles.transferButton} onClick={() => onPay(settlement, 'qr')} disabled={stale}>
                          <QrCode size={16} aria-hidden="true" />
                          {t('settlements.showQr')}
                        </Button>
                      </>
                    ) : (
                      <>
                        <RemindButton
                          settlement={settlement}
                          nextAt={nextReminderAt(settlement)}
                          disabled={stale}
                          onClick={() => setReminding(settlement)}
                        />
                        <Button variant="secondary" className={styles.transferButton} onClick={() => onPay(settlement, 'qr')} disabled={stale}>
                          <QrCode size={16} aria-hidden="true" />
                          {t('settlements.showQr')}
                        </Button>
                      </>
                    )}
                    <Button variant="secondary" className={styles.transferButton} onClick={() => setConfirming(settlement)}>
                      <Check size={16} aria-hidden="true" />
                      {t('settlements.markPaid')}
                    </Button>
                  </span>
                )}
              </li>
            );
          })}
        </ul>
      ) : (
        loadedSettlements && <p className={styles.panelHint}>{everyoneAtZero ? t('settlements.allSettled') : t('settlements.empty')}</p>
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

      <button type="button" className={styles.disclosure} onClick={onScan}>
        <ScanLine size={16} aria-hidden="true" />
        {t('scan.open')}
      </button>

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

      {reminding && (
        <ReminderDialog
          groupId={groupId}
          groupName={localize(groupName)}
          settlement={reminding}
          debtorName={localize(reminding.debtorName)}
          amount={money(reminding.amount)}
          onClose={() => setReminding(null)}
          onSent={(message) => {
            setReminding(null);
            onChanged(message);
          }}
        />
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

interface RemindButtonProps {
  settlement: Settlement;
  /** When the next reminder is possible (ms); null if one can be sent now. */
  nextAt: number | null;
  disabled: boolean;
  onClick: () => void;
}

/** "Remind" - or, within 24 hours of the last reminder, when it was sent and when the next one is possible. */
function RemindButton({ settlement, nextAt, disabled, onClick }: RemindButtonProps) {
  const { t } = useTranslation();
  const format = useFormatters();

  if (nextAt !== null && settlement.lastReminderSentAt) {
    const sent = format.dateTime(asUtc(settlement.lastReminderSentAt));
    const next = format.dateTime(new Date(nextAt).toISOString());
    return (
      <span className={styles.reminderSent} title={t('settlements.remindAgainAt', { when: next })}>
        <BellRing size={15} aria-hidden="true" />
        {t('settlements.reminderSentAt', { when: sent })}
        <span className="visually-hidden">. {t('settlements.remindAgainAt', { when: next })}</span>
      </span>
    );
  }

  return (
    <Button variant="secondary" className={styles.transferButton} onClick={onClick} disabled={disabled}>
      <BellRing size={16} aria-hidden="true" />
      {t('settlements.remind')}
    </Button>
  );
}
