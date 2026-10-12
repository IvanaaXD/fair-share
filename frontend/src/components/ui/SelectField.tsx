import { useId, type SelectHTMLAttributes } from 'react';
import styles from './Field.module.css';

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> {
  label: string;
  options: { value: string; label: string }[];
  hint?: string;
}

export function SelectField({ label, options, hint, ...selectProps }: SelectFieldProps) {
  const id = useId();

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.control}>
        <select id={id} className={styles.input} aria-describedby={hint ? `${id}-hint` : undefined} {...selectProps}>
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
    </div>
  );
}
