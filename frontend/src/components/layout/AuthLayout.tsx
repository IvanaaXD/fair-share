import { useTranslation } from 'react-i18next';
import { Link, Outlet } from 'react-router';
import { Logo } from '../brand/Logo';
import { LanguageSwitcher } from './LanguageSwitcher';
import { ThemeToggle } from './ThemeToggle';
import styles from './AuthLayout.module.css';

const MEMBER = {
  ana: { initials: 'АП', initialsLatin: 'AP', color: 'var(--member-1)' },
  marko: { initials: 'МЈ', initialsLatin: 'MJ', color: 'var(--member-2)' },
  jelena: { initials: 'ЈС', initialsLatin: 'JS', color: 'var(--member-3)' },
  nikola: { initials: 'НЂ', initialsLatin: 'NĐ', color: 'var(--member-4)' },
};

/**
 * Frame of the pages before sign-in: the form on the left, and on wide screens a panel on the
 * right that shows in one picture what the app does (who pays whom, with the fewest payments).
 */
export function AuthLayout() {
  const { t, i18n } = useTranslation();
  const latin = i18n.language !== 'sr-Cyrl';

  const amount = new Intl.NumberFormat(i18n.language === 'en' ? 'en' : 'sr-Latn', { minimumFractionDigits: 2 });

  const transfers = [
    { from: MEMBER.jelena, to: MEMBER.ana, label: t('auth.panelTransfer1'), amount: amount.format(35.75) },
    { from: MEMBER.nikola, to: MEMBER.ana, label: t('auth.panelTransfer2'), amount: amount.format(12.85) },
    { from: MEMBER.nikola, to: MEMBER.marko, label: t('auth.panelTransfer3'), amount: amount.format(13.55) },
  ];

  return (
    <div className={styles.shell}>
      <div className={styles.formSide}>
        <header className={styles.top}>
          <Link to="/login" className={styles.logoLink} aria-label="FairShare">
            <Logo tone="surface" />
          </Link>
          <div className={styles.controls}>
            <LanguageSwitcher tone="surface" />
            <ThemeToggle tone="surface" />
          </div>
        </header>

        <main className={styles.main}>
          <div className={styles.content}>
            <Outlet />
          </div>
        </main>
      </div>

      <aside className={styles.panel}>
        <div className={styles.panelInner}>
          <h2 className={styles.panelTitle}>{t('auth.panelTitle')}</h2>
          <p className={styles.panelText}>{t('auth.panelText')}</p>

          <div className={styles.example}>
            <div className={styles.exampleHeader}>
              <span className={styles.exampleGroup}>{t('auth.panelGroup')}</span>
              <span className={styles.exampleMeta}>{t('auth.panelGroupMeta')}</span>
            </div>
            <ul className={styles.transfers}>
              {transfers.map((transfer) => (
                <li key={transfer.label} className={styles.transfer}>
                  <span className={styles.avatar} style={{ background: transfer.from.color }} aria-hidden="true">
                    {latin ? transfer.from.initialsLatin : transfer.from.initials}
                  </span>
                  <svg className={styles.arrow} viewBox="0 0 28 14" aria-hidden="true">
                    <path d="M1 7h24M19 1l6 6-6 6" />
                  </svg>
                  <span className={styles.avatar} style={{ background: transfer.to.color }} aria-hidden="true">
                    {latin ? transfer.to.initialsLatin : transfer.to.initials}
                  </span>
                  <span className={styles.transferLabel}>{transfer.label}</span>
                  <span className={`money ${styles.transferAmount}`}>{transfer.amount}</span>
                </li>
              ))}
            </ul>
            <p className={styles.exampleNote}>{t('auth.panelNote')}</p>
          </div>
        </div>
      </aside>
    </div>
  );
}
