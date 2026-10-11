import { Moon, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../theme/useTheme';
import styles from './SidebarControls.module.css';

/** Moon in the light theme (switches to dark), sun in the dark theme (switches to light). */
export function ThemeToggle() {
  const { t } = useTranslation();
  const { theme, toggleTheme } = useTheme();
  const isLight = theme === 'light';

  return (
    <button
      type="button"
      className={styles.iconButton}
      aria-label={isLight ? t('settings.themeToDark') : t('settings.themeToLight')}
      title={isLight ? t('settings.themeToDark') : t('settings.themeToLight')}
      onClick={toggleTheme}
    >
      {isLight ? <Moon size={18} aria-hidden="true" /> : <Sun size={18} aria-hidden="true" />}
    </button>
  );
}
