import { useState } from 'react';
import { Plus, ReceiptText, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button, ButtonLink } from '../components/ui/Button';
import styles from './Page.module.css';

/** Start page. For now only the empty state; the cards are added with the expenses step. */
export function OverviewPage() {
  const { t, i18n } = useTranslation();

  // Read once when the page opens; a render must not depend on the current time.
  const [today] = useState(() => new Date());

  // "октобар 2026." / "oktobar 2026." / "October 2026" - formatted by the browser for the language.
  const currentMonth = new Intl.DateTimeFormat(i18n.language, { month: 'long', year: 'numeric' }).format(today);

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerText}>
          <h1 className={styles.title}>{t('overview.title')}</h1>
          <p className={styles.subtitle}>{currentMonth}</p>
        </div>
        <Button icon={<Plus size={18} aria-hidden="true" />}>{t('overview.addExpense')}</Button>
      </header>

      <section className={styles.empty} aria-labelledby="overview-empty-title">
        <span className={styles.emptyIcon} aria-hidden="true">
          <ReceiptText size={28} strokeWidth={1.75} />
        </span>
        <h2 id="overview-empty-title" className={styles.emptyTitle}>
          {t('overview.emptyTitle')}
        </h2>
        <p className={styles.emptyText}>{t('overview.emptyText')}</p>
        <div className={styles.actions}>
          <Button icon={<Plus size={18} aria-hidden="true" />}>{t('overview.addExpense')}</Button>
          <ButtonLink to="/groups" variant="secondary" icon={<Users size={18} aria-hidden="true" />}>
            {t('overview.createGroup')}
          </ButtonLink>
        </div>
      </section>
    </div>
  );
}
