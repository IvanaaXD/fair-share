import type { InputHTMLAttributes } from 'react';
import styles from './Field.module.css';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string;
}

export function Checkbox({ label, ...inputProps }: CheckboxProps) {
  return (
    <label className={styles.checkbox}>
      <input type="checkbox" {...inputProps} />
      {label}
    </label>
  );
}
