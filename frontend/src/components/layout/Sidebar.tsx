import type { RefObject } from 'react';
import { Bell, ChartColumn, ChartPie, House, ReceiptText, UserRound, Users, X, type LucideIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { sampleGroups, sampleUnreadNotifications, sampleUser } from '../../mocks/sidebarData';
import { Logo } from '../brand/Logo';
import { LanguageSwitcher } from './LanguageSwitcher';
import { ThemeToggle } from './ThemeToggle';
import styles from './Sidebar.module.css';

type NavKey = 'overview' | 'expenses' | 'groups' | 'budgets' | 'analytics' | 'notifications' | 'profile';

interface NavItem {
  key: NavKey;
  to: string;
  icon: LucideIcon;
}

const NAV_ITEMS: NavItem[] = [
  { key: 'overview', to: '/', icon: House },
  { key: 'expenses', to: '/expenses', icon: ReceiptText },
  { key: 'groups', to: '/groups', icon: Users },
  { key: 'budgets', to: '/budgets', icon: ChartPie },
  { key: 'analytics', to: '/analytics', icon: ChartColumn },
  { key: 'notifications', to: '/notifications', icon: Bell },
  { key: 'profile', to: '/profile', icon: UserRound },
];

interface SidebarProps {
  id: string;
  /** Below the desktop breakpoint the sidebar is a drawer that slides in over the page. */
  isDrawer: boolean;
  isOpen: boolean;
  onClose: () => void;
  /** Called when a link is followed, so the drawer can close. */
  onNavigate: () => void;
  closeButtonRef: RefObject<HTMLButtonElement | null>;
}

function navLinkClass({ isActive }: { isActive: boolean }) {
  return isActive ? `${styles.navLink} ${styles.navLinkActive}` : styles.navLink;
}

function groupLinkClass({ isActive }: { isActive: boolean }) {
  return isActive ? `${styles.groupLink} ${styles.groupLinkActive}` : styles.groupLink;
}

export function Sidebar({ id, isDrawer, isOpen, onClose, onNavigate, closeButtonRef }: SidebarProps) {
  const { t } = useTranslation();
  const localize = useLocalizedText();

  const className = [styles.sidebar, isDrawer && styles.drawer, isDrawer && isOpen && styles.open]
    .filter(Boolean)
    .join(' ');

  const initials = localize(sampleUser.firstName[0] + sampleUser.lastName[0]);

  return (
    // A closed drawer is "inert": hidden from the keyboard and screen readers, not only from the eye.
    <aside id={id} className={className} inert={isDrawer && !isOpen}>
      <div className={styles.top}>
        <NavLink to="/" className={styles.logoLink} onClick={onNavigate}>
          <Logo />
        </NavLink>
        {isDrawer && (
          <button
            ref={closeButtonRef}
            type="button"
            className={styles.closeButton}
            aria-label={t('nav.closeMenu')}
            onClick={onClose}
          >
            <X size={22} aria-hidden="true" />
          </button>
        )}
      </div>

      <nav aria-label={t('nav.mainMenu')}>
        <ul className={styles.list}>
          {NAV_ITEMS.map(({ key, to, icon: Icon }) => (
            <li key={key}>
              <NavLink to={to} end={to === '/'} className={navLinkClass} onClick={onNavigate}>
                <Icon size={20} strokeWidth={1.75} aria-hidden="true" />
                <span className={styles.navLabel}>{t(`nav.${key}`)}</span>
                {key === 'notifications' && sampleUnreadNotifications > 0 && (
                  <>
                    <span className={styles.badge} aria-hidden="true">
                      {sampleUnreadNotifications}
                    </span>
                    <span className="visually-hidden">
                      {t('nav.unread', { count: sampleUnreadNotifications })}
                    </span>
                  </>
                )}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <section className={styles.groups} aria-labelledby={`${id}-groups`}>
        <h2 id={`${id}-groups`} className={styles.groupsTitle}>
          {t('nav.yourGroups')}
        </h2>
        <ul className={styles.list}>
          {sampleGroups.map((group) => {
            const status = group.balance < 0 ? 'owe' : group.balance > 0 ? 'owed' : 'settled';
            return (
              <li key={group.id}>
                <NavLink to={`/groups/${group.id}`} className={groupLinkClass} onClick={onNavigate}>
                  <span className={`${styles.dot} ${styles[`dot_${status}`]}`} aria-hidden="true" />
                  <span className={styles.groupName}>{localize(group.name)}</span>
                  <span className="visually-hidden">, {t(`groupStatus.${status}`)}</span>
                </NavLink>
              </li>
            );
          })}
          <li>
            <NavLink to="/groups" end className={styles.allGroupsLink} onClick={onNavigate}>
              {t('nav.allGroups')}
            </NavLink>
          </li>
        </ul>
      </section>

      <div className={styles.controls}>
        <LanguageSwitcher />
        <ThemeToggle />
      </div>

      <div className={styles.user}>
        <span className={styles.avatar} aria-hidden="true">
          {initials}
        </span>
        <span className={styles.userText}>
          <span className={styles.userName}>
            {localize(`${sampleUser.firstName} ${sampleUser.lastName}`)}
          </span>
          <span className={styles.userEmail}>{sampleUser.email}</span>
        </span>
      </div>
    </aside>
  );
}
