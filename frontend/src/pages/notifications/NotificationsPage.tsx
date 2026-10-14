import { createElement, useEffect, useState } from 'react';
import { Bell, BellOff, CheckCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { notificationsApi } from '../../api/notifications';
import type { AppNotification } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { formattingLocale } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { NOTIFICATION_ICONS, notificationTypeName, WARNING_TYPES } from '../../notifications/notificationTypes';
import { refreshUnreadCount, setUnreadCount } from '../../notifications/unreadStore';
import pageStyles from '../Page.module.css';
import styles from './Notifications.module.css';

/** "YYYY-MM-DD" of a moment in the user's own time zone (for grouping by day). */
function localDay(value: string | Date): string {
  const date = typeof value === 'string' ? new Date(value) : value;
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

type Filter = 'all' | 'unread';

export function NotificationsPage() {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const { user } = useAuth();
  const [filter, setFilter] = useState<Filter>('all');
  const [version, setVersion] = useState(0);
  const [loaded, setLoaded] = useState<{ key: string; items: AppNotification[]; error: unknown } | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [today] = useState(() => new Date());
  const key = `${filter}:${version}`;

  useEffect(() => {
    const controller = new AbortController();
    notificationsApi.mine(filter === 'unread' ? false : undefined, controller.signal).then(
      (items) => setLoaded({ key, items, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded({ key, items: [], error });
      },
    );
    return () => controller.abort();
  }, [filter, key]);

  const items = loaded?.items ?? [];
  const unread = items.filter((item) => !item.isRead).length;

  // Opening the page also corrects the badge in the sidebar.
  useEffect(() => {
    if (user) void refreshUnreadCount(user.id);
  }, [user, version]);

  async function markRead(notification: AppNotification) {
    if (notification.isRead || !user) return;
    // Shown as read at once; if the server refuses, the reload brings back the truth.
    setLoaded((current) =>
      current && { ...current, items: current.items.map((item) => (item.id === notification.id ? { ...item, isRead: true } : item)) },
    );
    setUnreadCount(user.id, unread - 1);
    try {
      await notificationsApi.markRead(notification.id);
    } catch (error) {
      setActionError(errorMessage(error));
      setVersion((value) => value + 1);
    }
  }

  async function markAll() {
    if (!user) return;
    setActionError(null);
    try {
      await notificationsApi.markAllRead();
      setUnreadCount(user.id, 0);
      setVersion((value) => value + 1);
    } catch (error) {
      setActionError(errorMessage(error));
    }
  }

  // Grouped by day: "Today", "Yesterday", then dates.
  const locale = formattingLocale(i18n.language);
  const dayFormat = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric' });
  const timeFormat = new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' });
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  const dayLabel = (day: string, sample: string) =>
    day === localDay(today) ? t('notificationsPage.today') : day === localDay(yesterday) ? t('notificationsPage.yesterday') : dayFormat.format(new Date(sample));

  const groups: { day: string; label: string; items: AppNotification[] }[] = [];
  for (const item of [...items].sort((a, b) => b.createdAt.localeCompare(a.createdAt))) {
    const day = localDay(item.createdAt);
    const last = groups[groups.length - 1];
    if (last?.day === day) last.items.push(item);
    else groups.push({ day, label: dayLabel(day, item.createdAt), items: [item] });
  }

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('notificationsPage.title')}</h1>
          <p className={pageStyles.subtitle}>
            {loaded
              ? unread > 0
                ? t('notificationsPage.subtitleUnread', { count: unread })
                : t('notificationsPage.subtitleAllRead')
              : t('app.loading')}
          </p>
        </div>
        {unread > 0 && (
          <Button variant="secondary" icon={<CheckCheck size={18} aria-hidden="true" />} onClick={() => void markAll()}>
            {t('notificationsPage.markAll')}
          </Button>
        )}
      </header>

      <div className={styles.segmented} role="group" aria-label={t('notificationsPage.filterLabel')}>
        {(['all', 'unread'] as Filter[]).map((value) => (
          <button
            key={value}
            type="button"
            aria-pressed={filter === value}
            className={filter === value ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
            onClick={() => setFilter(value)}
          >
            {value === 'all' ? t('notificationsPage.all') : t('notificationsPage.unread')}
          </button>
        ))}
      </div>

      {actionError && <Alert tone="error">{actionError}</Alert>}
      {loaded?.error != null && (
        <Alert tone="error">
          {t('notificationsPage.loadError')} {errorMessage(loaded.error)}
        </Alert>
      )}

      {loaded && !loaded.error && items.length === 0 && (
        <section className={pageStyles.empty}>
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            {filter === 'unread' ? <BellOff size={28} strokeWidth={1.75} /> : <Bell size={28} strokeWidth={1.75} />}
          </span>
          <h2 className={pageStyles.emptyTitle}>
            {filter === 'unread' ? t('notificationsPage.emptyUnread') : t('notificationsPage.emptyTitle')}
          </h2>
          {filter === 'all' && <p className={pageStyles.emptyText}>{t('notificationsPage.emptyText')}</p>}
        </section>
      )}

      {groups.map((group) => (
        <section key={group.day} className={styles.day} aria-labelledby={`day-${group.day}`}>
          <h2 id={`day-${group.day}`} className={styles.dayTitle}>
            {group.label}
          </h2>
          <ul className={styles.list} aria-busy={loaded?.key !== key}>
            {group.items.map((item) => {
              const type = notificationTypeName(item.type);
              return (
                <li key={item.id}>
                  <button
                    type="button"
                    className={item.isRead ? styles.item : `${styles.item} ${styles.unread}`}
                    onClick={() => void markRead(item)}
                    aria-describedby={item.isRead ? undefined : `unread-${item.id}`}
                    title={item.isRead ? undefined : t('notificationsPage.markRead')}
                  >
                    <span className={WARNING_TYPES.has(type) ? `${styles.icon} ${styles.iconWarning}` : styles.icon} aria-hidden="true">
                      {createElement(NOTIFICATION_ICONS[type], { size: 20 })}
                    </span>
                    <span className={styles.text}>
                      <span className={styles.meta}>
                        <span className={styles.type}>{t(`notificationType.${type}`)}</span>
                        <time dateTime={item.createdAt}>{timeFormat.format(new Date(item.createdAt))}</time>
                      </span>
                      <span className={styles.message}>{localize(item.message)}</span>
                    </span>
                    {!item.isRead && (
                      <span id={`unread-${item.id}`} className={styles.dot}>
                        <span className="visually-hidden">{t('notificationsPage.unreadLabel')}</span>
                      </span>
                    )}
                  </button>
                </li>
              );
            })}
          </ul>
        </section>
      ))}
    </div>
  );
}
