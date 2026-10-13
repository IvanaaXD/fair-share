import { useState } from 'react';
import { KeyRound } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Navigate, useNavigate } from 'react-router';
import { useAuth } from '../../auth/useAuth';
import { Button } from '../../components/ui/Button';
import { PasswordForm } from '../profile/PasswordForm';
import styles from './Auth.module.css';

/**
 * Shown instead of the app to an account with a predefined password (MustChangePassword),
 * e.g. the seeded administrator. Every other page redirects here until the password is changed.
 */
export function ChangePasswordPage() {
  const { t } = useTranslation();
  const { user, reloadProfile, logout } = useAuth();
  const navigate = useNavigate();
  // Set right after a successful change, so the redirect below does not win over "go home".
  const [finished, setFinished] = useState(false);

  // Nothing to do here for a normal account - the password is changed in the profile.
  if (user && !user.mustChangePassword && !finished) {
    return <Navigate to="/profile" replace />;
  }

  return (
    <div className={styles.page}>
      <div className={styles.heading}>
        <span className={`${styles.resultIcon} ${styles.resultIconSuccess}`} aria-hidden="true">
          <KeyRound size={28} strokeWidth={1.75} />
        </span>
        <h1 className={styles.title}>{t('forcedPassword.title')}</h1>
        <p className={styles.text}>{t('forcedPassword.text')}</p>
      </div>
      <PasswordForm
        submitLabel={t('forcedPassword.submit')}
        onChanged={async () => {
          // The new profile no longer has MustChangePassword, so the app opens normally.
          setFinished(true);
          await reloadProfile();
          void navigate('/', { replace: true });
        }}
      />
      <div className={styles.footer}>
        <Button variant="secondary" onClick={() => void logout()}>
          {t('nav.logout')}
        </Button>
      </div>
    </div>
  );
}
