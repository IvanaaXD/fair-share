import { useState, type FormEvent } from 'react';
import { CircleAlert, CircleCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { authApi } from '../../api/auth';
import { ApiError } from '../../api/ApiError';
import { useErrorMessage } from '../../api/useErrorMessage';
import { matches, newPassword, onlyErrors, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button, ButtonLink } from '../../components/ui/Button';
import { TextField } from '../../components/ui/TextField';
import styles from './Auth.module.css';

type Field = 'password' | 'confirmPassword';

/** Opened from the password reset e-mail: /reset-password?token=... */
export function ResetPasswordPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [values, setValues] = useState({ password: '', confirmPassword: '' });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [state, setState] = useState<'form' | 'done' | 'invalid'>(token ? 'form' : 'invalid');

  const update = (field: Field) => (event: { target: { value: string } }) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const errors = onlyErrors({
      password: newPassword(values.password),
      confirmPassword: matches(values.confirmPassword, values.password),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0 || !token) {
      return;
    }

    setSubmitting(true);
    try {
      await authApi.resetPassword({ token, newPassword: values.password });
      setState('done');
    } catch (error) {
      // 400 without field errors means the token itself was rejected (expired or used).
      if (error instanceof ApiError && error.status === 400 && Object.keys(error.fieldErrors).length === 0) {
        setState('invalid');
      } else {
        setFormError(errorMessage(error));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (state === 'done') {
    return (
      <div className={styles.result}>
        <span className={`${styles.resultIcon} ${styles.resultIconSuccess}`} aria-hidden="true">
          <CircleCheck size={28} strokeWidth={1.75} />
        </span>
        <h1 className={styles.title}>{t('auth.reset.successTitle')}</h1>
        <p className={styles.text}>{t('auth.reset.successText')}</p>
        <ButtonLink to="/login">{t('auth.confirm.toLogin')}</ButtonLink>
      </div>
    );
  }

  if (state === 'invalid') {
    return (
      <div className={styles.result}>
        <span className={`${styles.resultIcon} ${styles.resultIconError}`} aria-hidden="true">
          <CircleAlert size={28} strokeWidth={1.75} />
        </span>
        <h1 className={styles.title}>{t('auth.confirm.errorTitle')}</h1>
        <p className={styles.text}>{token ? t('auth.reset.invalidText') : t('auth.reset.missing')}</p>
        <div className={styles.actions}>
          <ButtonLink to="/forgot-password">{t('auth.reset.requestNew')}</ButtonLink>
          <ButtonLink to="/login" variant="secondary">
            {t('auth.backToLogin')}
          </ButtonLink>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <h1 className={styles.title}>{t('auth.reset.title')}</h1>
        <p className={styles.text}>{t('auth.reset.text')}</p>
      </div>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}
        <TextField
          label={t('auth.newPassword')}
          type="password"
          name="password"
          autoComplete="new-password"
          value={values.password}
          onChange={update('password')}
          hint={t('auth.passwordHint')}
          errors={fieldErrors.password ? [t(fieldErrors.password)] : []}
        />
        <TextField
          label={t('auth.confirmPassword')}
          type="password"
          name="confirmPassword"
          autoComplete="new-password"
          value={values.confirmPassword}
          onChange={update('confirmPassword')}
          errors={fieldErrors.confirmPassword ? [t(fieldErrors.confirmPassword)] : []}
        />
        <Button type="submit" className={styles.submit} disabled={submitting}>
          {submitting ? t('auth.reset.submitting') : t('auth.reset.submit')}
        </Button>
      </form>

      <div className={styles.footer}>
        <Link to="/login" className={styles.link}>
          {t('auth.backToLogin')}
        </Link>
      </div>
    </div>
  );
}
