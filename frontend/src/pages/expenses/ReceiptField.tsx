import { useEffect, useId, useRef, useState, type ReactNode } from 'react';
import { ExternalLink, ImagePlus, RefreshCw, Trash2, Undo2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useProtectedImage } from '../../hooks/useProtectedImage';
import { RECEIPT_TYPES } from './expenseValidation';
import styles from './ExpenseDialog.module.css';

interface ReceiptFieldProps {
  /** Address of the receipt already saved on the server. */
  savedUrl: string | null;
  /** A new photo chosen in this form, not uploaded yet. */
  file: File | null;
  /** The saved receipt is marked for removal. */
  removed: boolean;
  error: string | null;
  disabled: boolean;
  onSelect: (file: File) => void;
  onClearFile: () => void;
  onRemoveSaved: () => void;
  onUndoRemove: () => void;
}

/**
 * Receipt photo of an expense. Nothing is uploaded here: the form uploads the chosen photo
 * (or removes the saved one) only after the expense itself has been saved.
 */
export function ReceiptField({
  savedUrl,
  file,
  removed,
  error,
  disabled,
  onSelect,
  onClearFile,
  onRemoveSaved,
  onUndoRemove,
}: ReceiptFieldProps) {
  const { t } = useTranslation();
  const id = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const saved = useProtectedImage(file || removed ? null : savedUrl);

  const choose = () => inputRef.current?.click();

  let content;
  if (file) {
    content = (
      <Preview src={<LocalImage file={file} alt={t('expenseForm.receiptAlt')} />} caption={file.name}>
        <ActionButton icon={<RefreshCw size={16} />} onClick={choose} disabled={disabled}>
          {t('expenseForm.replaceReceipt')}
        </ActionButton>
        <ActionButton icon={<Trash2 size={16} />} onClick={onClearFile} disabled={disabled}>
          {t('expenseForm.removeReceipt')}
        </ActionButton>
      </Preview>
    );
  } else if (savedUrl && removed) {
    content = (
      <div className={styles.receiptRemoved}>
        <p>{t('expenseForm.receiptRemoved')}</p>
        <ActionButton icon={<Undo2 size={16} />} onClick={onUndoRemove} disabled={disabled}>
          {t('expenseForm.undo')}
        </ActionButton>
      </div>
    );
  } else if (savedUrl) {
    content = (
      <Preview
        src={
          saved.url ? (
            <img src={saved.url} alt={t('expenseForm.receiptAlt')} className={styles.receiptImage} />
          ) : (
            <span className={styles.receiptPlaceholder}>
              {saved.status === 'error' ? t('expenseForm.receiptLoadError') : t('expenseForm.receiptLoading')}
            </span>
          )
        }
        caption={
          saved.url ? (
            <a href={saved.url} target="_blank" rel="noreferrer" className={styles.receiptLink}>
              {t('expenseForm.openReceipt')}
              <ExternalLink size={14} aria-hidden="true" />
            </a>
          ) : null
        }
      >
        <ActionButton icon={<RefreshCw size={16} />} onClick={choose} disabled={disabled}>
          {t('expenseForm.replaceReceipt')}
        </ActionButton>
        <ActionButton icon={<Trash2 size={16} />} onClick={onRemoveSaved} disabled={disabled}>
          {t('expenseForm.removeReceipt')}
        </ActionButton>
      </Preview>
    );
  } else {
    content = (
      <button
        type="button"
        className={styles.receiptPicker}
        onClick={choose}
        disabled={disabled}
        aria-describedby={`${id}-hint`}
      >
        <ImagePlus size={22} aria-hidden="true" />
        {t('expenseForm.chooseReceipt')}
      </button>
    );
  }

  return (
    <fieldset className={styles.receipt} aria-describedby={error ? `${id}-error` : undefined}>
      <legend className={styles.legend}>
        {t('expenseForm.receipt')} <span className={styles.optional}>({t('common.optional')})</span>
      </legend>
      {content}
      <p id={`${id}-hint`} className={styles.hint}>
        {t('expenseForm.receiptHint')}
      </p>
      {error && (
        <p id={`${id}-error`} className={styles.fieldError}>
          {error}
        </p>
      )}
      <input
        ref={inputRef}
        type="file"
        accept={RECEIPT_TYPES.join(',')}
        className="visually-hidden"
        tabIndex={-1}
        aria-hidden="true"
        onChange={(event) => {
          const chosen = event.target.files?.[0];
          // Cleared so that choosing the same file again still fires onChange.
          event.target.value = '';
          if (chosen) {
            onSelect(chosen);
          }
        }}
      />
    </fieldset>
  );
}

function Preview({ src, caption, children }: { src: ReactNode; caption: ReactNode; children: ReactNode }) {
  return (
    <div className={styles.receiptPreview}>
      <div className={styles.receiptThumb}>{src}</div>
      <div className={styles.receiptInfo}>
        {caption && <div className={styles.receiptCaption}>{caption}</div>}
        <div className={styles.receiptActions}>{children}</div>
      </div>
    </div>
  );
}

function ActionButton({
  icon,
  onClick,
  disabled,
  children,
}: {
  icon: ReactNode;
  onClick: () => void;
  disabled: boolean;
  children: ReactNode;
}) {
  return (
    <button type="button" className={styles.receiptAction} onClick={onClick} disabled={disabled}>
      <span aria-hidden="true">{icon}</span>
      {children}
    </button>
  );
}

/** Preview of a file that is still only on the user's device (read as a data URL). */
function LocalImage({ file, alt }: { file: File; alt: string }) {
  const [preview, setPreview] = useState<{ file: File; url: string } | null>(null);

  useEffect(() => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        setPreview({ file, url: reader.result });
      }
    };
    reader.readAsDataURL(file);
    return () => reader.abort();
  }, [file]);

  // A preview of the previously chosen file is never shown for a new one.
  return preview && preview.file === file ? <img src={preview.url} alt={alt} className={styles.receiptImage} /> : null;
}
