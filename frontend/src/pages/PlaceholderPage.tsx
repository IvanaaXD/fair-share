import { Hammer } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ButtonLink } from '../components/ui/Button';
import styles from './Page.module.css';

export type PlaceholderKey = 'expenses' | 'groups' | 'budgets' | 'analytics' | 'notifications' | 'profile';

/** Temporary page for sections that are built in later steps, so every menu link works. */
export function PlaceholderPage({ titleKey }: { titleKey: PlaceholderKey }) {
  const { t } = useTranslation();

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>{t(`nav.${titleKey}`)}</h1>
      </header>
      <section className={styles.empty}>
        <span className={styles.emptyIcon} aria-hidden="true">
          <Hammer size={28} strokeWidth={1.75} />
        </span>
        <p className={styles.emptyText}>{t('placeholder.text')}</p>
        <div className={styles.actions}>
          <ButtonLink to="/" variant="secondary">
            {t('placeholder.back')}
          </ButtonLink>
        </div>
      </section>
    </div>
  );
}
