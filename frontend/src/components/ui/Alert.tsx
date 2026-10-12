import type { ReactNode } from 'react';
import { CircleAlert, CircleCheck, Info } from 'lucide-react';
import styles from './Alert.module.css';

type Tone = 'error' | 'success' | 'info';

const ICONS = { error: CircleAlert, success: CircleCheck, info: Info };

/**
 * A message box. Errors use role="alert" so screen readers read them out immediately;
 * success and info messages use role="status", which is read without interrupting.
 */
export function Alert({ tone, children }: { tone: Tone; children: ReactNode }) {
  const Icon = ICONS[tone];
  return (
    <div className={`${styles.alert} ${styles[tone]}`} role={tone === 'error' ? 'alert' : 'status'}>
      <Icon size={20} aria-hidden="true" className={styles.icon} />
      <div className={styles.text}>{children}</div>
    </div>
  );
}
