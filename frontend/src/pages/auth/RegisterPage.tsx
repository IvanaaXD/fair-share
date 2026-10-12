import { useState, type FormEvent } from 'react';
import { MailCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { authApi } from '../../api/auth';
import { ApiError } from '../../api/ApiError';
import { useErrorMessage } from '../../api/useErrorMessage';
import {
  email as validateEmail,
  matches,
  name as validateName,
  newPassword,
  onlyErrors,
  type ValidationKey,
} from '../../auth/validation';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import styles from './Auth.module.css';

type Field = 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword';

const EMPTY = { firstName: '', lastName: '', email: '', password: '', confirmPassword: '' };

export function RegisterPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();

  const [values, setValues] = useState(EMPTY);
  const [currency, setCurrency] = useState('BAM');
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverFieldErrors, setServerFieldErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);

  const update = (field: Field) => (event: { target: { value: string } }) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
    setServerFieldErrors((current) => ({ ...current, [field]: [] }));
  };

  /** Client-side error first; otherwise whatever the server said about this field. */
  const errorsFor = (field: Field) => {
    const clientError = fieldErrors[field];
    if (clientError) {
      return [t(clientError)];
    }
    return (serverFieldErrors[field] ?? []).map(localize);
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerFieldErrors({});

    const errors = onlyErrors({
      firstName: validateName(values.firstName),
      lastName: validateName(values.lastName),
      email: validateEmail(values.email),
      password: newPassword(values.password),
      confirmPassword: matches(values.confirmPassword, values.password),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    setSubmitting(true);
    try {
      await authApi.register({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        email: values.email.trim(),
        password: values.password,
        defaultCurrency: currency,
      });
      setRegisteredEmail(values.email.trim());
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerFieldErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error, { 409: t('auth.register.emailTaken') }));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (registeredEmail) {
    return <RegistrationSent email={registeredEmail} />;
  }

  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <h1 className={styles.title}>{t('auth.register.title')}</h1>
        <p className={styles.text}>{t('auth.register.text')}</p>
      </div>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}

        <div className={styles.row}>
          <TextField
            label={t('auth.firstName')}
            name="firstName"
            autoComplete="given-name"
            value={values.firstName}
            onChange={update('firstName')}
            errors={errorsFor('firstName')}
          />
          <TextField
            label={t('auth.lastName')}
            name="lastName"
            autoComplete="family-name"
            value={values.lastName}
            onChange={update('lastName')}
            errors={errorsFor('lastName')}
          />
        </div>
        <TextField
          label={t('auth.email')}
          type="email"
          name="email"
          autoComplete="email"
          inputMode="email"
          value={values.email}
          onChange={update('email')}
          errors={errorsFor('email')}
        />
        <TextField
          label={t('auth.password')}
          type="password"
          name="password"
          autoComplete="new-password"
          value={values.password}
          onChange={update('password')}
          hint={t('auth.passwordHint')}
          errors={errorsFor('password')}
        />
        <TextField
          label={t('auth.confirmPassword')}
          type="password"
          name="confirmPassword"
          autoComplete="new-password"
          value={values.confirmPassword}
          onChange={update('confirmPassword')}
          errors={errorsFor('confirmPassword')}
        />
        <SelectField
          label={t('auth.currency')}
          hint={t('auth.currencyHint')}
          value={currency}
          onChange={(event) => setCurrency(event.target.value)}
          options={[
            { value: 'BAM', label: t('auth.currencyBam') },
            { value: 'EUR', label: t('auth.currencyEur') },
            { value: 'RSD', label: t('auth.currencyRsd') },
          ]}
        />

        <Button type="submit" className={styles.submit} disabled={submitting}>
          {submitting ? t('auth.register.submitting') : t('auth.register.submit')}
        </Button>
      </form>

      <div className={styles.footer}>
        <p>
          {t('auth.register.haveAccount')}{' '}
          <Link to="/login" className={styles.link}>
            {t('auth.register.login')}
          </Link>
        </p>
      </div>
    </div>
  );
}

/** After registration: the account exists but is inactive until the link from the e-mail is opened. */
function RegistrationSent({ email }: { email: string }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [state, setState] = useState<'idle' | 'sending' | 'sent'>('idle');
  const [error, setError] = useState<string | null>(null);

  async function resend() {
    setState('sending');
    setError(null);
    try {
      await authApi.resendConfirmation({ email });
      setState('sent');
    } catch (failure) {
      setError(errorMessage(failure));
      setState('idle');
    }
  }

  return (
    <div className={styles.result}>
      <span className={`${styles.resultIcon} ${styles.resultIconSuccess}`} aria-hidden="true">
        <MailCheck size={28} strokeWidth={1.75} />
      </span>
      <h1 className={styles.title}>{t('auth.register.successTitle')}</h1>
      <p className={styles.text}>{t('auth.register.successText', { email })}</p>
      {state === 'sent' && <Alert tone="success">{t('auth.register.resent')}</Alert>}
      {error && <Alert tone="error">{error}</Alert>}
      <div className={styles.actions}>
        <Button variant="secondary" onClick={() => void resend()} disabled={state !== 'idle'}>
          {state === 'sending' ? t('auth.sending') : t('auth.register.resend')}
        </Button>
        <Link to="/login" className={styles.link} style={{ alignSelf: 'center' }}>
          {t('auth.backToLogin')}
        </Link>
      </div>
    </div>
  );
}
