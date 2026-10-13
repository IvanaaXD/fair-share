import { useEffect, useId, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import styles from './Dialog.module.css';

interface DialogProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  /** Buttons at the bottom; they stay visible while the content scrolls. */
  footer?: ReactNode;
  /** false while something is being saved, so the dialog cannot be closed halfway. */
  dismissible?: boolean;
  /** lg: wider window for forms with tables (e.g. splitting a group expense); xl: side-by-side content (QR code + details). */
  size?: 'md' | 'lg' | 'xl';
}

/**
 * A modal window, shown while it is rendered. Built on the native <dialog> element, which
 * already keeps keyboard focus inside, closes on Escape and hides the page from screen
 * readers - so none of that has to be re-implemented here.
 * On a phone it fills the screen; from 640px up it is a centered window.
 * Put data-autofocus on the element that should have focus when the dialog opens.
 */
export function Dialog({ title, onClose, children, footer, dismissible = true, size = 'md' }: DialogProps) {
  const { t } = useTranslation();
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) {
      return;
    }
    // Focus goes back to the element that opened the dialog when it closes.
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    dialog.showModal();
    // showModal() focuses the first button (the X). A field marked with data-autofocus,
    // e.g. the amount of a new expense, is a better place to start typing.
    dialog.querySelector<HTMLElement>('[data-autofocus]')?.focus();
    return () => {
      dialog.close();
      opener?.focus();
    };
  }, []);

  return (
    <dialog
      ref={ref}
      className={size === 'md' ? styles.dialog : `${styles.dialog} ${size === 'lg' ? styles.large : styles.extraLarge}`}
      aria-labelledby={titleId}
      onCancel={(event) => {
        // Escape: the parent decides (and removes the dialog), not the browser.
        event.preventDefault();
        if (dismissible) {
          onClose();
        }
      }}
    >
      <div className={styles.header}>
        <h2 id={titleId} className={styles.title}>
          {title}
        </h2>
        <button
          type="button"
          className={styles.close}
          aria-label={t('common.close')}
          onClick={onClose}
          disabled={!dismissible}
        >
          <X size={22} aria-hidden="true" />
        </button>
      </div>
      <div className={styles.body}>{children}</div>
      {footer && <div className={styles.footer}>{footer}</div>}
    </dialog>
  );
}
