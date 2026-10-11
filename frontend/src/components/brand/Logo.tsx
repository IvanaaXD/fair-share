import styles from './Logo.module.css';

/**
 * The FairShare mark: one circle split into two unequal shares (petrol and amber, the same
 * two colors that mean "owed to you" and "you owe"), followed by the wordmark.
 */
export function Logo({ showName = true }: { showName?: boolean }) {
  return (
    <span className={styles.logo}>
      <svg className={styles.mark} viewBox="0 0 40 40" aria-hidden="true" focusable="false">
        <circle cx="20" cy="20" r="18" fill="#0b6375" />
        <path d="M20 2 A18 18 0 0 1 20 38 Z" fill="#b45309" />
        <rect x="19" y="2" width="2" height="36" style={{ fill: 'var(--color-nav-bg)' }} />
      </svg>
      {showName && <span className={styles.name}>FairShare</span>}
    </span>
  );
}
