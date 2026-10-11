import { useCallback, useEffect, useRef, useState } from 'react';
import { Menu } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Outlet } from 'react-router';
import { useMediaQuery } from '../../hooks/useMediaQuery';
import { Logo } from '../brand/Logo';
import { Sidebar } from './Sidebar';
import styles from './AppLayout.module.css';

const SIDEBAR_ID = 'app-sidebar';

/**
 * The frame around every page of the signed-in app: the sidebar and the page itself.
 * From 1024px up the sidebar is always visible; below that it becomes a drawer opened from
 * the top bar, so on a phone the whole width belongs to the content.
 */
export function AppLayout() {
  const { t } = useTranslation();
  const isDesktop = useMediaQuery('(min-width: 1024px)');
  const [menuOpen, setMenuOpen] = useState(false);
  const drawerOpen = !isDesktop && menuOpen;

  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  const closeMenu = useCallback(() => {
    setMenuOpen(false);
    // Back to the button that opened the menu, so keyboard users do not lose their place.
    menuButtonRef.current?.focus();
  }, []);

  // While the drawer is open: Escape closes it, focus starts on its close button and the
  // page behind it does not scroll.
  useEffect(() => {
    if (!drawerOpen) {
      return;
    }

    closeButtonRef.current?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeMenu();
      }
    };
    document.addEventListener('keydown', onKeyDown);

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', onKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [drawerOpen, closeMenu]);

  return (
    <div className={styles.shell}>
      <a href="#main-content" className={styles.skipLink}>
        {t('app.skipToContent')}
      </a>

      {!isDesktop && (
        <header className={styles.topBar}>
          <button
            ref={menuButtonRef}
            type="button"
            className={styles.menuButton}
            aria-label={t('nav.openMenu')}
            aria-expanded={drawerOpen}
            aria-controls={SIDEBAR_ID}
            onClick={() => setMenuOpen(true)}
          >
            <Menu size={24} aria-hidden="true" />
          </button>
          <Logo />
        </header>
      )}

      <Sidebar
        id={SIDEBAR_ID}
        isDrawer={!isDesktop}
        isOpen={drawerOpen}
        onClose={closeMenu}
        onNavigate={() => setMenuOpen(false)}
        closeButtonRef={closeButtonRef}
      />

      {drawerOpen && <div className={styles.backdrop} aria-hidden="true" onClick={closeMenu} />}

      <main id="main-content" tabIndex={-1} className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
}
