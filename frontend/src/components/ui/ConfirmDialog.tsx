import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from './Alert';
import { Button } from './Button';
import { Dialog } from './Dialog';
import styles from './ConfirmDialog.module.css';

interface ConfirmDialogProps {
  title: string;
  children: ReactNode;
  confirmLabel: string;
  busyLabel: string;
  /** Deleting, leaving, removing someone - shown in amber. */
  danger?: boolean;
  /** The action itself. If it fails, the error is shown here and the dialog stays open. */
  onConfirm: () => Promise<void>;
  onClose: () => void;
  /** Messages for status codes that need a sentence of their own (e.g. 409). */
  errorsByStatus?: Partial<Record<number, string>>;
}

/** "Are you sure?" for actions that cannot be undone. */
export function ConfirmDialog({
  title,
  children,
  confirmLabel,
  busyLabel,
  danger,
  onConfirm,
  onClose,
  errorsByStatus,
}: ConfirmDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    setBusy(true);
    setError(null);
    try {
      await onConfirm();
    } catch (failure) {
      setError(errorMessage(failure, errorsByStatus));
      setBusy(false);
    }
  }

  return (
    <Dialog
      title={title}
      onClose={onClose}
      dismissible={!busy}
      footer={
        <div className={styles.buttons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button variant={danger ? 'danger' : 'primary'} onClick={() => void confirm()} disabled={busy} data-autofocus>
            {busy ? busyLabel : confirmLabel}
          </Button>
        </div>
      }
    >
      <div className={styles.body}>
        {error && <Alert tone="error">{error}</Alert>}
        <div className={styles.text}>{children}</div>
      </div>
    </Dialog>
  );
}
