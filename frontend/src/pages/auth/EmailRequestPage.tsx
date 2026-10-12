import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { authApi } from '../../api/auth';
import { useErrorMessage } from '../../api/useErrorMessage';
import { email as validateEmail, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/TextField';
import styles from './Auth.module.css';

/**
 * "Forgot password" and "send the activation link again" - both ask only for an e-mail address.
 * The answer is the same whether the address has an account or not (the backend does the same),
 * so the page cannot be used to find out who is registered.
 */
export function EmailRequestPage({ mode }: { mode: 'forgot' | 'resend' }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();

  const [email, setEmail] = useState('');
  const [fieldError, setFieldError] = useState<ValidationKey | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [sentTo, setSentTo] = useState<string | null>(null);

  const texts =
    mode === 'forgot'
      ? { title: t('auth.forgot.title'), text: t('auth.forgot.text'), submit: t('auth.forgot.submit') }
      : { title: t('auth.resend.title'), text: t('auth.resend.text'), submit: t('auth.resend.submit') };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const error = validateEmail(email);
    setFieldError(error);
    if (error) {
      return;
    }

    setSubmitting(true);
    try {
      const request = { email: email.trim() };
      await (mode === 'forgot' ? authApi.forgotPassword(request) : authApi.resendConfirmation(request));
      setSentTo(email.trim());
    } catch (failure) {
      setFormError(errorMessage(failure));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <h1 className={styles.title}>{texts.title}</h1>
        <p className={styles.text}>{texts.text}</p>
      </div>

      {sentTo ? (
        <Alert tone="success">
          {mode === 'forgot'
            ? t('auth.forgot.successText', { email: sentTo })
            : t('auth.resend.successText', { email: sentTo })}
        </Alert>
      ) : (
        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          {formError && <Alert tone="error">{formError}</Alert>}
          <TextField
            label={t('auth.email')}
            type="email"
            name="email"
            autoComplete="email"
            inputMode="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              setFieldError(null);
            }}
            errors={fieldError ? [t(fieldError)] : []}
          />
          <Button type="submit" className={styles.submit} disabled={submitting}>
            {submitting ? t('auth.sending') : texts.submit}
          </Button>
        </form>
      )}

      <div className={styles.footer}>
        <Link to="/login" className={styles.link}>
          {t('auth.backToLogin')}
        </Link>
      </div>
    </div>
  );
}
