import type { RefObject } from 'react';
import { Bell, ChartColumn, ChartPie, House, LogOut, ReceiptText, UserRound, Users, X, type LucideIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router';
import { useAuth } from '../../auth/useAuth';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { balanceStatus } from '../../groups/groupUtils';
import { useMyGroups } from '../../groups/myGroupsStore';
import { sampleUnreadNotifications } from '../../mocks/sidebarData';
import { Logo } from '../brand/Logo';
import { UserAvatar } from '../ui/UserAvatar';
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

/** How many groups fit in the quick switcher; the rest are on the groups page. */
const MAX_SIDEBAR_GROUPS = 5;

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
  const { user, logout } = useAuth();
  const { groups } = useMyGroups();

  const className = [styles.sidebar, isDrawer && styles.drawer, isDrawer && isOpen && styles.open]
    .filter(Boolean)
    .join(' ');

  const fullName = user ? localize(`${user.firstName} ${user.lastName}`) : '';

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
          {(groups ?? []).slice(0, MAX_SIDEBAR_GROUPS).map((group) => {
            const status = group.myBalance === null ? null : balanceStatus(group.myBalance);
            return (
              <li key={group.id}>
                <NavLink to={`/groups/${group.id}`} className={groupLinkClass} onClick={onNavigate}>
                  <span className={`${styles.dot} ${styles[`dot_${status ?? 'settled'}`]}`} aria-hidden="true" />
                  <span className={styles.groupName}>{localize(group.name)}</span>
                  {status && <span className="visually-hidden">, {t(`groupStatus.${status}`)}</span>}
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
        <NavLink to="/profile" className={styles.userLink} onClick={onNavigate}>
          {user && (
            <UserAvatar
              firstName={user.firstName}
              lastName={user.lastName}
              imageUrl={user.profileImageUrl}
              color="var(--member-3)"
            />
          )}
          <span className={styles.userText}>
            <span className={styles.userName}>{fullName}</span>
            <span className={styles.userEmail}>{user?.email}</span>
          </span>
        </NavLink>
        <button
          type="button"
          className={styles.logoutButton}
          aria-label={t('nav.logout')}
          title={t('nav.logout')}
          onClick={() => void logout()}
        >
          <LogOut size={18} aria-hidden="true" />
        </button>
      </div>
    </aside>
  );
}
