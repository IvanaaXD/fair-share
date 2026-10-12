import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate, type Location } from 'react-router';
import { ApiError } from '../../api/ApiError';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { email as validateEmail, onlyErrors, required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Checkbox } from '../../components/ui/Checkbox';
import { TextField } from '../../components/ui/TextField';
import styles from './Auth.module.css';

type Field = 'email' | 'password';

export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const errorMessage = useErrorMessage();
  const navigate = useNavigate();
  const location = useLocation();

  const [values, setValues] = useState({ email: '', password: '' });
  const [remember, setRemember] = useState(true);
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const update = (field: Field) => (event: { target: { value: string } }) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const errors = onlyErrors({ email: validateEmail(values.email), password: required(values.password) });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    setSubmitting(true);
    try {
      await login(values.email, values.password, remember);
      // Back to the page the user wanted to open before being sent to sign in.
      const from = (location.state as { from?: Location } | null)?.from;
      void navigate(from ? `${from.pathname}${from.search}` : '/', { replace: true });
    } catch (error) {
      setFormError(errorMessage(error, { 401: t('auth.login.invalid') }));
      if (error instanceof ApiError && error.status === 401) {
        setValues((current) => ({ ...current, password: '' }));
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <h1 className={styles.title}>{t('auth.login.title')}</h1>
        <p className={styles.text}>{t('auth.login.text')}</p>
      </div>

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}

        <TextField
          label={t('auth.email')}
          type="email"
          name="email"
          autoComplete="email"
          inputMode="email"
          value={values.email}
          onChange={update('email')}
          errors={fieldErrors.email ? [t(fieldErrors.email)] : []}
        />
        <TextField
          label={t('auth.password')}
          type="password"
          name="password"
          autoComplete="current-password"
          value={values.password}
          onChange={update('password')}
          errors={fieldErrors.password ? [t(fieldErrors.password)] : []}
          labelAside={
            <Link to="/forgot-password" className={styles.smallLink}>
              {t('auth.login.forgot')}
            </Link>
          }
        />
        <Checkbox
          label={t('auth.rememberMe')}
          checked={remember}
          onChange={(event) => setRemember(event.target.checked)}
        />

        <Button type="submit" className={styles.submit} disabled={submitting}>
          {submitting ? t('auth.login.submitting') : t('auth.login.submit')}
        </Button>
      </form>

      <div className={styles.footer}>
        <p>
          {t('auth.login.noAccount')}{' '}
          <Link to="/register" className={styles.link}>
            {t('auth.login.register')}
          </Link>
        </p>
        <p>
          {t('auth.login.noActivation')}{' '}
          <Link to="/resend-confirmation" className={styles.link}>
            {t('auth.login.resend')}
          </Link>
        </p>
      </div>
    </div>
  );
}
