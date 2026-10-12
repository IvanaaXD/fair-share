import { useId, useState, type InputHTMLAttributes, type ReactNode } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import styles from './Field.module.css';

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  label: string;
  /** Already translated messages; the field is marked invalid when there is at least one. */
  errors?: string[];
  hint?: string;
  /** Shown at the right end of the label row, e.g. a "Forgot password?" link. */
  labelAside?: ReactNode;
}

/**
 * Label, input, hint and errors, wired together for screen readers: the input points to its
 * hint and errors (aria-describedby) and announces itself as invalid (aria-invalid).
 */
export function TextField({ label, errors = [], hint, labelAside, type = 'text', className, ...inputProps }: TextFieldProps) {
  const { t } = useTranslation();
  const id = useId();
  const [revealed, setRevealed] = useState(false);

  const isPassword = type === 'password';
  const hasError = errors.length > 0;
  const describedBy = [hint && `${id}-hint`, hasError && `${id}-error`].filter(Boolean).join(' ') || undefined;

  return (
    <div className={[styles.field, hasError && styles.invalid, className].filter(Boolean).join(' ')}>
      <div className={styles.labelRow}>
        <label htmlFor={id} className={styles.label}>
          {label}
        </label>
        {labelAside}
      </div>
      <div className={styles.control}>
        <input
          id={id}
          type={isPassword && revealed ? 'text' : type}
          className={styles.input}
          aria-invalid={hasError || undefined}
          aria-describedby={describedBy}
          {...inputProps}
        />
        {isPassword && (
          <button
            type="button"
            className={styles.toggle}
            aria-label={revealed ? t('auth.hidePassword') : t('auth.showPassword')}
            aria-pressed={revealed}
            onClick={() => setRevealed((value) => !value)}
          >
            {revealed ? <EyeOff size={20} aria-hidden="true" /> : <Eye size={20} aria-hidden="true" />}
          </button>
        )}
      </div>
      {hint && (
        <p id={`${id}-hint`} className={styles.hint}>
          {hint}
        </p>
      )}
      {hasError && (
        <ul id={`${id}-error`} className={styles.error}>
          {errors.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
