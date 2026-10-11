import { useTranslation } from 'react-i18next';
import { ButtonLink } from '../components/ui/Button';
import styles from './Page.module.css';

export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <div className={styles.page}>
      <section className={styles.empty}>
        <h1 className={styles.emptyTitle}>{t('notFound.title')}</h1>
        <p className={styles.emptyText}>{t('notFound.text')}</p>
        <div className={styles.actions}>
          <ButtonLink to="/">{t('notFound.back')}</ButtonLink>
        </div>
      </section>
    </div>
  );
}
