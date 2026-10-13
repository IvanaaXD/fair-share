import { useEffect, useId, useRef, useState } from 'react';
import { CreditCard, Download, Plus, QrCode } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { ApiError } from '../../api/ApiError';
import { settlementsApi } from '../../api/groupExpenses';
import { paymentsApi, qrApi } from '../../api/payments';
import type { QrPayload, Settlement } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { useProtectedImage } from '../../hooks/useProtectedImage';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { formatBankAccount } from '../profile/bankAccount';
import { CardForm } from './CardForm';
import { newIdempotencyKey } from './cardUtils';
import { useCards } from './useCards';
import styles from './Payments.module.css';

export type PayTab = 'qr' | 'card';

/** Error codes of the payment simulator (PaymentErrorCodes on the backend) -> messages. */
const FAILURE_MESSAGES = {
  card_declined: 'payment.failed_card_declined',
  insufficient_funds: 'payment.failed_insufficient_funds',
  expired_card: 'payment.failed_expired_card',
  invalid_card: 'payment.failed_invalid_card',
  processing_error: 'payment.failed_processing_error',
} as const;

function failureKey(reason: string | null) {
  return reason && reason in FAILURE_MESSAGES
    ? FAILURE_MESSAGES[reason as keyof typeof FAILURE_MESSAGES]
    : 'payment.failedGeneric';
}

interface PayDialogProps {
  groupId: string;
  currency: string;
  settlement: Settlement;
  myId: string | undefined;
  /** Name of the member who receives the money, as shown in the group. */
  creditorName: string;
  initialTab?: PayTab;
  onClose: () => void;
  /** The settlement is settled (card payment or marked as paid). */
  onPaid: (message: string) => void;
}

/**
 * Paying one settlement: with the IPS QR code in any banking app, or with a saved card
 * through the payment simulator. The member who receives the money sees only the QR code,
 * to show it to the one who pays.
 */
export function PayDialog({ groupId, currency, settlement, myId, creditorName, initialTab = 'qr', onClose, onPaid }: PayDialogProps) {
  const { t } = useTranslation();
  const format = useFormatters();
  const iAmDebtor = settlement.debtorUserId === myId;
  const [tab, setTab] = useState<PayTab>(iAmDebtor ? initialTab : 'qr');
  const [busy, setBusy] = useState(false);
  const tabsId = useId();

  return (
    <Dialog title={t('payment.title', { name: creditorName })} onClose={onClose} dismissible={!busy} size="xl">
      <div className={styles.dialogBody}>
        <div className={styles.topRow}>
          {iAmDebtor && (
            <div className={styles.tabs} role="tablist" aria-label={t('payment.tabsLabel')}>
              {(['qr', 'card'] as PayTab[]).map((value) => (
                <button
                  key={value}
                  type="button"
                  role="tab"
                  id={`${tabsId}-${value}`}
                  aria-selected={tab === value}
                  aria-controls={`${tabsId}-panel`}
                  className={tab === value ? `${styles.tab} ${styles.tabActive}` : styles.tab}
                  onClick={() => setTab(value)}
                  disabled={busy}
                >
                  {value === 'qr' ? <QrCode size={17} aria-hidden="true" /> : <CreditCard size={17} aria-hidden="true" />}
                  {value === 'qr' ? t('payment.tabQr') : t('payment.tabCard')}
                </button>
              ))}
            </div>
          )}
          <p className={styles.amountLine}>
            <span>{t('payment.amount')}</span>
            <span className="money">{format.money(settlement.amount, currency)}</span>
          </p>
        </div>

        <div id={`${tabsId}-panel`} role={iAmDebtor ? 'tabpanel' : undefined} aria-labelledby={iAmDebtor ? `${tabsId}-${tab}` : undefined}>
          {tab === 'qr' ? (
            <QrPanel groupId={groupId} settlement={settlement} iAmDebtor={iAmDebtor} onPaid={onPaid} onBusy={setBusy} />
          ) : (
            <CardPanel groupId={groupId} currency={currency} settlement={settlement} onPaid={onPaid} onBusy={setBusy} />
          )}
        </div>
      </div>
    </Dialog>
  );
}

interface PanelProps {
  groupId: string;
  settlement: Settlement;
  onPaid: (message: string) => void;
  onBusy: (busy: boolean) => void;
}

function QrPanel({ groupId, settlement, iAmDebtor, onPaid, onBusy }: PanelProps & { iAmDebtor: boolean }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const [state, setState] = useState<{ payload: QrPayload | null; error: unknown } | null>(null);
  const [marking, setMarking] = useState(false);
  const [markError, setMarkError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    qrApi.payload(groupId, settlement.id, controller.signal).then(
      (payload) => setState({ payload, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setState({ payload: null, error });
      },
    );
    return () => controller.abort();
  }, [groupId, settlement.id]);

  // The image is requested only when the payload worked (e.g. the recipient has an account).
  const image = useProtectedImage(state?.payload ? qrApi.imageUrl(groupId, settlement.id) : null);

  async function markPaid() {
    setMarking(true);
    onBusy(true);
    setMarkError(null);
    try {
      await settlementsApi.settle(groupId, settlement.id);
      onPaid(t('settlements.paid'));
    } catch (error) {
      setMarkError(errorMessage(error));
      setMarking(false);
      onBusy(false);
    }
  }

  if (!state) {
    return <p className={styles.muted}>{t('payment.qrLoading')}</p>;
  }

  if (!state.payload) {
    const noAccount = state.error instanceof ApiError && state.error.status === 409;
    if (!noAccount) {
      return <Alert tone="error">{errorMessage(state.error)}</Alert>;
    }
    return (
      <Alert tone="info">
        {iAmDebtor ? (
          t('payment.qrNoAccount')
        ) : (
          <>
            {t('payment.qrNoAccountMine')} <Link to="/profile">{t('nav.profile')}</Link>
          </>
        )}
      </Alert>
    );
  }

  const payload = state.payload;
  return (
    <div className={styles.qr}>
      <div className={styles.qrImageBox}>
        {image.url ? (
          <img src={image.url} alt={t('payment.qrAlt')} className={styles.qrImage} />
        ) : (
          <span className={styles.muted}>{t('payment.qrLoading')}</span>
        )}
      </div>
      <div className={styles.qrSide}>
        <p className={styles.help}>{iAmDebtor ? t('payment.qrHelp') : t('payment.qrHelpCreditor')}</p>
        <dl className={styles.details}>
          <dt>{t('payment.recipient')}</dt>
          <dd>{localize(payload.recipientName)}</dd>
          <dt>{t('payment.account')}</dt>
          <dd className={styles.mono}>{formatBankAccount(payload.recipientAccount)}</dd>
          <dt>{t('payment.amount')}</dt>
          <dd className="money">{format.money(payload.amount, payload.currency)}</dd>
          {payload.purpose && (
            <>
              <dt>{t('payment.purpose')}</dt>
              <dd>{localize(payload.purpose)}</dd>
            </>
          )}
          <dt>{t('payment.reference')}</dt>
          <dd className={styles.mono}>{payload.referenceCode}</dd>
        </dl>
        {markError && <Alert tone="error">{markError}</Alert>}
        <div className={styles.formButtons}>
          {image.url && (
            <a className={styles.linkButton} href={image.url} download={`fairshare-qr-${payload.referenceCode.slice(0, 8)}.png`}>
              <Download size={16} aria-hidden="true" />
              {t('payment.download')}
            </a>
          )}
          <Button onClick={() => void markPaid()} disabled={marking}>
            {marking ? t('settlements.confirming') : t('payment.markPaid')}
          </Button>
        </div>
      </div>
    </div>
  );
}

function CardPanel({ groupId, currency, settlement, onPaid, onBusy }: PanelProps & { currency: string }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const format = useFormatters();
  const { cards, loading, error, reload } = useCards();
  const [chosen, setChosen] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [paying, setPaying] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);
  // One key per attempt. Kept after a network error (the retry must not charge twice),
  // replaced after a definite answer (a declined card may be tried again as a new attempt).
  const idempotencyKey = useRef(newIdempotencyKey());

  const usable = cards.filter((card) => !card.isExpired);
  const selected = chosen ?? usable[0]?.id ?? null;
  const showForm = adding || (!loading && cards.length === 0);

  async function pay() {
    if (!selected) return;
    setPaying(true);
    onBusy(true);
    setFailure(null);
    try {
      const result = await paymentsApi.payWithCard(groupId, settlement.id, selected, idempotencyKey.current);
      if (result.status === 'Succeeded' || result.status === 1) {
        onPaid(t('payment.paid'));
        return;
      }
      idempotencyKey.current = newIdempotencyKey();
      setFailure(`${t(failureKey(result.failureReason))} ${t('payment.tryOther')}`);
    } catch (problem) {
      if (problem instanceof ApiError && !problem.isNetworkError) {
        idempotencyKey.current = newIdempotencyKey();
      }
      setFailure(errorMessage(problem));
    }
    setPaying(false);
    onBusy(false);
  }

  return (
    <div className={styles.cardPanel}>
      {error != null && <Alert tone="error">{errorMessage(error)}</Alert>}
      {loading && cards.length === 0 && <p className={styles.muted}>{t('payment.cardsLoading')}</p>}

      {cards.length > 0 && (
        <fieldset className={styles.cardList} disabled={paying}>
          <legend className={styles.legend}>{t('payment.chooseCard')}</legend>
          {cards.map((card) => (
            <label key={card.id} className={card.isExpired ? `${styles.cardOption} ${styles.cardExpired}` : styles.cardOption}>
              <input
                type="radio"
                name="paymentCard"
                value={card.id}
                checked={selected === card.id}
                disabled={card.isExpired}
                onChange={() => setChosen(card.id)}
              />
              <CreditCard size={20} aria-hidden="true" />
              <span className={styles.cardName}>
                {card.brand} •••• {card.last4Digits}
              </span>
              <span className={styles.cardMeta}>
                {card.isExpired
                  ? t('payment.expired')
                  : t('payment.expires', { date: `${String(card.expiryMonth).padStart(2, '0')}/${String(card.expiryYear).slice(-2)}` })}
              </span>
            </label>
          ))}
        </fieldset>
      )}

      {!loading && cards.length === 0 && <p className={styles.muted}>{t('payment.noCards')}</p>}

      {showForm ? (
        <CardForm
          onSaved={(card) => {
            setAdding(false);
            setChosen(card.id);
            reload();
          }}
          onCancel={cards.length > 0 ? () => setAdding(false) : undefined}
        />
      ) : (
        <button type="button" className={styles.addCard} onClick={() => setAdding(true)} disabled={paying}>
          <Plus size={16} aria-hidden="true" />
          {t('payment.addCard')}
        </button>
      )}

      {failure && (
        <Alert tone="error">
          <strong>{t('payment.failedTitle')}.</strong> {failure}
        </Alert>
      )}

      {!showForm && (
        <Button className={styles.payButton} onClick={() => void pay()} disabled={paying || !selected}>
          {paying ? t('payment.paying') : t('payment.payNow', { amount: format.money(settlement.amount, currency) })}
        </Button>
      )}
      <p className={styles.simulator}>{t('payment.simulatorNote')}</p>
    </div>
  );
}
