import { useEffect, useRef, useState } from 'react';
import { CircleAlert, CircleCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { authApi } from '../../api/auth';
import { ApiError } from '../../api/ApiError';
import { useErrorMessage } from '../../api/useErrorMessage';
import { ButtonLink } from '../../components/ui/Button';
import styles from './Auth.module.css';

type State = 'pending' | 'success' | 'invalid' | 'failed';

/**
 * Opened from the activation e-mail: /confirm-email?token=... The token is sent to the backend
 * as soon as the page opens; there is nothing for the user to fill in.
 */
export function ConfirmEmailPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [state, setState] = useState<State>(token ? 'pending' : 'invalid');
  const [failure, setFailure] = useState<string | null>(null);

  // A token can be used only once. React's StrictMode runs effects twice in development, so
  // this flag makes sure the request is sent once - the second one would always fail.
  const sent = useRef(false);

  useEffect(() => {
    if (!token || sent.current) {
      return;
    }
    sent.current = true;
    authApi
      .confirmEmail({ token })
      .then(() => setState('success'))
      .catch((error: unknown) => {
        if (error instanceof ApiError && error.status === 400) {
          setState('invalid');
        } else {
          setFailure(errorMessage(error));
          setState('failed');
        }
      });
  }, [token, errorMessage]);

  if (state === 'pending') {
    return (
      <p className={styles.text} role="status">
        {t('auth.confirm.pending')}
      </p>
    );
  }

  if (state === 'success') {
    return (
      <div className={styles.result}>
        <span className={`${styles.resultIcon} ${styles.resultIconSuccess}`} aria-hidden="true">
          <CircleCheck size={28} strokeWidth={1.75} />
        </span>
        <h1 className={styles.title}>{t('auth.confirm.successTitle')}</h1>
        <p className={styles.text}>{t('auth.confirm.successText')}</p>
        <ButtonLink to="/login">{t('auth.confirm.toLogin')}</ButtonLink>
      </div>
    );
  }

  return (
    <div className={styles.result}>
      <span className={`${styles.resultIcon} ${styles.resultIconError}`} aria-hidden="true">
        <CircleAlert size={28} strokeWidth={1.75} />
      </span>
      <h1 className={styles.title}>{t('auth.confirm.errorTitle')}</h1>
      <p className={styles.text}>
        {state === 'failed' ? failure : token ? t('auth.confirm.errorText') : t('auth.confirm.missing')}
      </p>
      <div className={styles.actions}>
        <ButtonLink to="/resend-confirmation">{t('auth.confirm.resend')}</ButtonLink>
        <ButtonLink to="/login" variant="secondary">
          {t('auth.backToLogin')}
        </ButtonLink>
      </div>
    </div>
  );
}
