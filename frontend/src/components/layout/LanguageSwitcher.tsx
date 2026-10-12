import { useTranslation } from 'react-i18next';
import { LANGUAGES } from '../../i18n';
import styles from './SidebarControls.module.css';

/**
 * Three buttons: Cyrillic, Latin, English. The active one is marked with aria-pressed.
 * tone="nav" for the dark sidebar, tone="surface" for light pages (login, registration).
 */
export function LanguageSwitcher({ tone = 'nav' }: { tone?: 'nav' | 'surface' }) {
  const { t, i18n } = useTranslation();

  return (
    <div role="group" aria-label={t('settings.language')} className={`${styles.segmented} ${styles[tone]}`}>
      {LANGUAGES.map((language) => {
        const isActive = i18n.language === language.code;
        return (
          <button
            key={language.code}
            type="button"
            lang={language.code}
            aria-label={language.label}
            aria-pressed={isActive}
            className={isActive ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
            onClick={() => void i18n.changeLanguage(language.code)}
          >
            {language.short}
          </button>
        );
      })}
    </div>
  );
}
