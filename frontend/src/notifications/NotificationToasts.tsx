import { createElement, useEffect, useState } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { AppNotification } from '../api/types';
import { useLocalizedText } from '../i18n/useLocalizedText';
import { NOTIFICATION_ICONS, notificationTypeName, WARNING_TYPES } from './notificationTypes';
import { onNotificationCreated } from './realtime';
import styles from './NotificationToasts.module.css';

const MAX_TOASTS = 3;
const SHOW_MS = 7_000;

/**
 * Short pop-ups for notifications that arrive while the app is open. On the notifications page
 * itself they are not needed - the new item appears at the top of the list.
 */
export function NotificationToasts() {
  const { t } = useTranslation();
  const localize = useLocalizedText();
  const [toasts, setToasts] = useState<AppNotification[]>([]);

  useEffect(
    () =>
      onNotificationCreated((notification) => {
        // Read when the notification arrives, not during render.
        if (window.location.pathname === '/notifications') return;
        setToasts((current) => [notification, ...current.filter((item) => item.id !== notification.id)].slice(0, MAX_TOASTS));
      }),
    [],
  );

  const dismiss = (id: string) => setToasts((current) => current.filter((item) => item.id !== id));

  return (
    <div className={styles.region} role="region" aria-label={t('toast.region')}>
      <ul className={styles.list} aria-live="polite">
        {toasts.map((notification) => (
          <Toast key={notification.id} notification={notification} onDismiss={() => dismiss(notification.id)}>
            {localize(notification.message)}
          </Toast>
        ))}
      </ul>
    </div>
  );
}

function Toast({ notification, onDismiss, children }: { notification: AppNotification; onDismiss: () => void; children: string }) {
  const { t } = useTranslation();
  const type = notificationTypeName(notification.type);
  const [paused, setPaused] = useState(false);

  // Disappears by itself, but not while the pointer or the keyboard focus is on it.
  useEffect(() => {
    if (paused) return;
    const timer = window.setTimeout(onDismiss, SHOW_MS);
    return () => window.clearTimeout(timer);
  }, [paused, onDismiss]);

  return (
    <li
      className={styles.toast}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocus={() => setPaused(true)}
      onBlur={() => setPaused(false)}
    >
      <span className={WARNING_TYPES.has(type) ? `${styles.icon} ${styles.iconWarning}` : styles.icon} aria-hidden="true">
        {createElement(NOTIFICATION_ICONS[type], { size: 18 })}
      </span>
      <span className={styles.body}>
        <span className={styles.type}>{t(`notificationType.${type}`)}</span>
        <span className={styles.message}>{children}</span>
        <Link to="/notifications" className={styles.link} onClick={onDismiss}>
          {t('toast.open')}
        </Link>
      </span>
      <button type="button" className={styles.close} aria-label={t('toast.close')} onClick={onDismiss}>
        <X size={16} aria-hidden="true" />
      </button>
    </li>
  );
}
