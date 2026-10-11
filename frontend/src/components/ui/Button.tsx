import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router';
import styles from './Button.module.css';

type Variant = 'primary' | 'secondary';

function classes(variant: Variant, extra?: string) {
  return [styles.button, styles[variant], extra].filter(Boolean).join(' ');
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  icon?: ReactNode;
}

export function Button({ variant = 'primary', icon, className, children, type = 'button', ...rest }: ButtonProps) {
  return (
    <button type={type} className={classes(variant, className)} {...rest}>
      {icon}
      {children}
    </button>
  );
}

interface ButtonLinkProps extends LinkProps {
  variant?: Variant;
  icon?: ReactNode;
}

/** A link that looks like a button - for actions that open another page. */
export function ButtonLink({ variant = 'primary', icon, className, children, ...rest }: ButtonLinkProps) {
  return (
    <Link className={classes(variant, typeof className === 'string' ? className : undefined)} {...rest}>
      {icon}
      {children}
    </Link>
  );
}
