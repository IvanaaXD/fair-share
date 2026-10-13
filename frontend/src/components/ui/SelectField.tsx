import { useId, type SelectHTMLAttributes } from 'react';
import styles from './Field.module.css';

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> {
  label: string;
  options: { value: string; label: string }[];
  hint?: string;
  /** Already translated messages; the field is marked invalid when there is at least one. */
  errors?: string[];
  /** Smaller control for filter bars. */
  compact?: boolean;
}

export function SelectField({ label, options, hint, errors = [], compact, className, ...selectProps }: SelectFieldProps) {
  const id = useId();
  const hasError = errors.length > 0;
  const describedBy = [hint && `${id}-hint`, hasError && `${id}-error`].filter(Boolean).join(' ') || undefined;

  return (
    <div className={[styles.field, hasError && styles.invalid, compact && styles.compact, className].filter(Boolean).join(' ')}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.control}>
        <select
          id={id}
          className={styles.input}
          aria-invalid={hasError || undefined}
          aria-describedby={describedBy}
          {...selectProps}
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
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
