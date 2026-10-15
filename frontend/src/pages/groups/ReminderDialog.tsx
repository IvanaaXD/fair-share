import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { settlementsApi } from '../../api/groupExpenses';
import type { Settlement } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import fieldStyles from '../../components/ui/Field.module.css';
import styles from './ReminderDialog.module.css';

/** The same limit as SendReminderRequestValidator on the backend. */
export const REMINDER_MAX_LENGTH = 300;

interface ReminderDialogProps {
  groupId: string;
  groupName: string;
  settlement: Settlement;
  debtorName: string;
  amount: string;
  onClose: () => void;
  onSent: (message: string) => void;
}

/**
 * "Remind" for a debt owed to the user: the debtor gets a notification in the app and an
 * e-mail. The message is pre-filled and can be changed or removed.
 */
export function ReminderDialog({ groupId, groupName, settlement, debtorName, amount, onClose, onSent }: ReminderDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const fieldId = useId();
  const [message, setMessage] = useState(() => t('reminder.defaultMessage', { group: groupName }));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const tooLong = message.length > REMINDER_MAX_LENGTH;

  async function send() {
    if (tooLong) return;
    setBusy(true);
    setError(null);
    try {
      await settlementsApi.remind(groupId, settlement.id, message.trim() || null);
      onSent(t('reminder.sent', { name: debtorName }));
    } catch (failure) {
      setError(errorMessage(failure));
      setBusy(false);
    }
  }

  return (
    <Dialog
      title={t('reminder.title')}
      onClose={onClose}
      dismissible={!busy}
      footer={
        <div className={styles.buttons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button onClick={() => void send()} disabled={busy || tooLong}>
            {busy ? t('reminder.sending') : t('reminder.send')}
          </Button>
        </div>
      }
    >
      <div className={styles.body}>
        <p className={styles.intro}>{t('reminder.text', { name: debtorName, amount })}</p>

        <div className={tooLong ? `${fieldStyles.field} ${fieldStyles.invalid}` : fieldStyles.field}>
          <div className={fieldStyles.labelRow}>
            <label htmlFor={fieldId} className={fieldStyles.label}>
              {t('reminder.message')}
            </label>
            <span className={tooLong ? `${styles.counter} ${styles.counterOver}` : styles.counter} aria-hidden="true">
              {message.length}/{REMINDER_MAX_LENGTH}
            </span>
          </div>
          <div className={fieldStyles.control}>
            <textarea
              id={fieldId}
              className={styles.textarea}
              rows={4}
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              aria-invalid={tooLong || undefined}
              aria-describedby={`${fieldId}-hint`}
              disabled={busy}
              data-autofocus
            />
          </div>
          <p id={`${fieldId}-hint`} className={tooLong ? fieldStyles.error : fieldStyles.hint}>
            {tooLong ? t('reminder.tooLong', { max: REMINDER_MAX_LENGTH }) : t('reminder.hint')}
          </p>
        </div>

        <div aria-live="polite">{error && <Alert tone="error">{error}</Alert>}</div>
      </div>
    </Dialog>
  );
}
