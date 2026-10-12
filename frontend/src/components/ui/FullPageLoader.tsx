import { useTranslation } from 'react-i18next';
import { Logo } from '../brand/Logo';
import styles from './FullPageLoader.module.css';

/** Shown for the moment it takes to restore a saved session when the app opens. */
export function FullPageLoader() {
  const { t } = useTranslation();
  return (
    <div className={styles.screen} role="status">
      <span className={styles.mark}>
        <Logo showName={false} />
      </span>
      <span className="visually-hidden">{t('app.loading')}</span>
    </div>
  );
}
