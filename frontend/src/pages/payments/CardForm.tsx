import { useState, type FormEvent } from 'react';
import { Lock } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { paymentsApi } from '../../api/payments';
import type { PaymentCard } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { onlyErrors, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/TextField';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import {
  digitsOnly,
  formatCardNumber,
  formatExpiry,
  parseExpiry,
  validateCardNumber,
  validateCvv,
  validateExpiry,
} from './cardUtils';
import styles from './Payments.module.css';

type Field = 'cardNumber' | 'expiry' | 'cvv';

interface CardFormProps {
  onSaved: (card: PaymentCard) => void;
  onCancel?: () => void;
}

/**
 * A new payment card. The number and CVV are sent once to the backend, which exchanges them
 * with the payment simulator for a token; the form is cleared right after.
 */
export function CardForm({ onSaved, onCancel }: CardFormProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();

  const [values, setValues] = useState({ cardNumber: '', expiry: '', cvv: '', holderName: '' });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const set = (field: keyof typeof values, value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
  };
  const errorsFor = (field: Field, serverFields: string[]) => {
    const clientError = fieldErrors[field];
    return clientError ? [t(clientError)] : serverFields.flatMap((name) => serverErrors[name] ?? []).map(localize);
  };

  // A nested <form> is not allowed inside the payment dialog's form, so this handler is
  // called from the button; the wrapper is a <div role="group">.
  async function save(event?: FormEvent) {
    event?.preventDefault();
    setFormError(null);
    setServerErrors({});

    const errors = onlyErrors({
      cardNumber: validateCardNumber(values.cardNumber),
      expiry: validateExpiry(values.expiry),
      cvv: validateCvv(values.cvv),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    const expiry = parseExpiry(values.expiry)!;
    setSaving(true);
    try {
      const card = await paymentsApi.addCard({
        cardNumber: digitsOnly(values.cardNumber),
        expiryMonth: expiry.month,
        expiryYear: expiry.year,
        cvv: values.cvv,
        holderName: values.holderName.trim() || null,
      });
      // The card data leaves the page's memory as soon as it is no longer needed.
      setValues({ cardNumber: '', expiry: '', cvv: '', holderName: '' });
      onSaved(card);
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className={styles.cardForm}
      role="group"
      aria-label={t('payment.addCard')}
      onKeyDown={(event) => {
        if (event.key === 'Enter' && event.target instanceof HTMLInputElement) {
          event.preventDefault();
          void save();
        }
      }}
    >
      {formError && <Alert tone="error">{formError}</Alert>}
      <TextField
        label={t('card.number')}
        name="cardNumber"
        inputMode="numeric"
        autoComplete="cc-number"
        placeholder="4242 4242 4242 4242"
        value={values.cardNumber}
        onChange={(event) => set('cardNumber', formatCardNumber(event.target.value))}
        errors={errorsFor('cardNumber', ['cardNumber'])}
        className={styles.mono}
        disabled={saving}
      />
      <div className={styles.cardRow}>
        <TextField
          label={t('card.expiry')}
          name="expiry"
          inputMode="numeric"
          autoComplete="cc-exp"
          placeholder="08/28"
          value={values.expiry}
          onChange={(event) => set('expiry', formatExpiry(event.target.value))}
          errors={errorsFor('expiry', ['expiryMonth', 'expiryYear'])}
          disabled={saving}
        />
        <TextField
          label={t('card.cvv')}
          name="cvv"
          type="password"
          inputMode="numeric"
          autoComplete="cc-csc"
          maxLength={4}
          value={values.cvv}
          onChange={(event) => set('cvv', digitsOnly(event.target.value).slice(0, 4))}
          errors={errorsFor('cvv', ['cvv'])}
          disabled={saving}
        />
      </div>
      <TextField
        label={`${t('card.holder')} (${t('common.optional')})`}
        name="holderName"
        autoComplete="cc-name"
        value={values.holderName}
        onChange={(event) => set('holderName', event.target.value)}
        errors={(serverErrors.holderName ?? []).map(localize)}
        disabled={saving}
      />
      <p className={styles.secure}>
        <Lock size={14} aria-hidden="true" />
        {t('card.secureNote')}
      </p>
      <div className={styles.formButtons}>
        {onCancel && (
          <Button variant="secondary" onClick={onCancel} disabled={saving}>
            {t('card.cancel')}
          </Button>
        )}
        <Button onClick={() => void save()} disabled={saving}>
          {saving ? t('card.saving') : t('card.save')}
        </Button>
      </div>
    </div>
  );
}
