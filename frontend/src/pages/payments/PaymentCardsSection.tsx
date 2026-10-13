import { useState } from 'react';
import { CreditCard, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { paymentsApi } from '../../api/payments';
import type { PaymentCard } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { CardForm } from './CardForm';
import { useCards } from './useCards';
import styles from './Payments.module.css';

/** Saved payment cards in the profile: list, add, remove. */
export function PaymentCardsSection({ onNotice, cardClassName, titleClassName }: {
  onNotice: (text: string) => void;
  cardClassName: string;
  titleClassName: string;
}) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const { cards, error, loading, reload } = useCards();
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<PaymentCard | null>(null);

  const label = (card: PaymentCard) => ({ brand: card.brand, last4: card.last4Digits });

  return (
    <section className={cardClassName} aria-labelledby="cards-title">
      <div>
        <h2 id="cards-title" className={titleClassName}>
          {t('card.title')}
        </h2>
        <p className={styles.muted}>{t('card.hint')}</p>
      </div>

      {error != null && <Alert tone="error">{errorMessage(error)}</Alert>}

      {cards.length > 0 ? (
        <ul className={styles.savedCards}>
          {cards.map((card) => (
            <li key={card.id} className={styles.savedCard}>
              <CreditCard size={22} aria-hidden="true" />
              <span className={styles.cardName}>
                {card.brand} •••• {card.last4Digits}
              </span>
              <span className={styles.cardMeta}>
                {card.isExpired
                  ? t('payment.expired')
                  : t('payment.expires', { date: `${String(card.expiryMonth).padStart(2, '0')}/${String(card.expiryYear).slice(-2)}` })}
              </span>
              <button
                type="button"
                className={styles.iconButton}
                aria-label={t('card.remove', label(card))}
                title={t('card.remove', label(card))}
                onClick={() => setRemoving(card)}
              >
                <Trash2 size={18} aria-hidden="true" />
              </button>
            </li>
          ))}
        </ul>
      ) : (
        !loading && !adding && <p className={styles.muted}>{t('card.none')}</p>
      )}

      {adding ? (
        <CardForm
          onSaved={() => {
            setAdding(false);
            reload();
            onNotice(t('card.added'));
          }}
          onCancel={() => setAdding(false)}
        />
      ) : (
        <div>
          <Button variant="secondary" icon={<Plus size={17} aria-hidden="true" />} onClick={() => setAdding(true)}>
            {t('payment.addCard')}
          </Button>
        </div>
      )}

      {removing && (
        <ConfirmDialog
          title={t('card.removeTitle')}
          confirmLabel={t('profile.photoRemove')}
          busyLabel={t('card.removing')}
          danger
          onClose={() => setRemoving(null)}
          onConfirm={async () => {
            await paymentsApi.removeCard(removing.id);
            setRemoving(null);
            reload();
            onNotice(t('card.removed'));
          }}
        >
          <p>{t('card.removeText', label(removing))}</p>
        </ConfirmDialog>
      )}
    </section>
  );
}
