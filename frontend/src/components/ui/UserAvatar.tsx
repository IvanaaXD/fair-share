import { useProtectedImage } from '../../hooks/useProtectedImage';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Avatar.module.css';

interface UserAvatarProps {
  firstName: string;
  lastName: string;
  /** Address of the profile picture; without one the initials are shown. */
  imageUrl: string | null;
  color: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

/**
 * A person's profile picture, or their initials while it loads or when there is none.
 * The picture needs the access token, so it is downloaded with it (useProtectedImage).
 */
export function UserAvatar({ firstName, lastName, imageUrl, color, size = 'md', className }: UserAvatarProps) {
  const localize = useLocalizedText();
  const image = useProtectedImage(imageUrl);
  const classes = [styles.avatar, styles[size], className].filter(Boolean).join(' ');

  if (image.url) {
    return <img src={image.url} alt="" className={`${classes} ${styles.photo}`} />;
  }
  return (
    <span className={classes} style={{ background: color }} aria-hidden="true">
      {localize(`${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase())}
    </span>
  );
}
