import styles from './Avatar.module.css';

interface AvatarProps {
  /** Already localized initials, e.g. "АП" / "AP". */
  initials: string;
  color: string;
  size?: 'sm' | 'md' | 'lg';
  /** Ring in the page background color, used when avatars overlap. */
  ringed?: boolean;
}

/** Round initials of a person. Decorative: the name is always written next to it. */
export function Avatar({ initials, color, size = 'md', ringed }: AvatarProps) {
  return (
    <span
      className={[styles.avatar, styles[size], ringed && styles.ringed].filter(Boolean).join(' ')}
      style={{ background: color }}
      aria-hidden="true"
    >
      {initials}
    </span>
  );
}

interface AvatarStackProps {
  people: { key: string; initials: string; color: string }[];
  /** How many are drawn; the rest becomes "+3". */
  max?: number;
  size?: 'sm' | 'md';
}

export function AvatarStack({ people, max = 4, size = 'sm' }: AvatarStackProps) {
  const shown = people.slice(0, max);
  const hidden = people.length - shown.length;
  return (
    <span className={styles.stack} aria-hidden="true">
      {shown.map((person) => (
        <Avatar key={person.key} initials={person.initials} color={person.color} size={size} ringed />
      ))}
      {hidden > 0 && <span className={`${styles.avatar} ${styles[size]} ${styles.ringed} ${styles.more}`}>+{hidden}</span>}
    </span>
  );
}
