import { createElement, useId, useState, type FormEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import type { SaveCategoryRequest } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { TextField } from '../../components/ui/TextField';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { CATEGORY_ICON_KEYS, iconForKey } from '../expenses/categoryIcons';
import styles from './Categories.module.css';

/** The same limits as CreateCategoryRequestValidator and the database column. */
const MIN_NAME = 2;
const MAX_NAME = 50;

interface CategoryFormDialogProps {
  title: string;
  submitLabel: string;
  busyLabel: string;
  initial?: SaveCategoryRequest;
  /** Shown above the form, e.g. "An administrator will review your proposal". */
  intro?: ReactNode;
  onSubmit: (request: SaveCategoryRequest) => Promise<void>;
  onClose: () => void;
}

/**
 * Name and icon of a category. Used for a user's proposal and for the administrator's new
 * category, rename and approval (where the name and icon of a proposal can be corrected).
 */
export function CategoryFormDialog({ title, submitLabel, busyLabel, initial, intro, onSubmit, onClose }: CategoryFormDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const pickerLabel = useId();

  const [name, setName] = useState(initial?.name ?? '');
  const [icon, setIcon] = useState<string | null>(initial?.icon ?? null);
  const [nameError, setNameError] = useState<string | null>(null);
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    const problem = !trimmed
      ? t('categoryForm.nameRequired')
      : trimmed.length < MIN_NAME
        ? t('categoryForm.nameTooShort', { min: MIN_NAME })
        : trimmed.length > MAX_NAME
          ? t('categoryForm.nameTooLong', { max: MAX_NAME })
          : null;
    setNameError(problem);
    setServerErrors({});
    setFormError(null);
    if (problem) return;

    setBusy(true);
    try {
      await onSubmit({ name: trimmed, icon });
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) setServerErrors(error.fieldErrors);
      else setFormError(errorMessage(error));
      setBusy(false);
    }
  }

  const nameErrors = nameError ? [nameError] : (serverErrors.name ?? []).map(localize);
  const otherErrors = Object.entries(serverErrors)
    .filter(([field]) => field !== 'name')
    .flatMap(([, messages]) => messages)
    .map(localize);

  return (
    <Dialog
      title={title}
      onClose={onClose}
      dismissible={!busy}
      footer={
        <div className={styles.footerButtons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="category-form" disabled={busy}>
            {busy ? busyLabel : submitLabel}
          </Button>
        </div>
      }
    >
      <form id="category-form" className={styles.form} onSubmit={(event) => void submit(event)} noValidate>
        {intro && <div className={styles.intro}>{intro}</div>}
        {formError && <Alert tone="error">{formError}</Alert>}
        {otherErrors.length > 0 && <Alert tone="error">{otherErrors.join(' ')}</Alert>}

        <TextField
          label={t('categoryForm.name')}
          name="name"
          value={name}
          maxLength={MAX_NAME}
          placeholder={t('categoryForm.namePlaceholder')}
          autoComplete="off"
          onChange={(event) => {
            setName(event.target.value);
            setNameError(null);
          }}
          errors={nameErrors}
          disabled={busy}
          data-autofocus
        />

        <fieldset className={styles.pickerField} disabled={busy}>
          <legend id={pickerLabel} className={styles.legend}>
            {t('categoryForm.icon')}
          </legend>
          <div className={styles.picker} role="radiogroup" aria-labelledby={pickerLabel}>
            <button
              type="button"
              role="radio"
              aria-checked={icon === null}
              className={icon === null ? `${styles.iconChoice} ${styles.iconChoiceActive}` : styles.iconChoice}
              title={t('categoryForm.iconAuto')}
              aria-label={t('categoryForm.iconAuto')}
              onClick={() => setIcon(null)}
            >
              {createElement(iconForKey(null, name), { size: 20, 'aria-hidden': true })}
              <span className={styles.autoMark} aria-hidden="true">A</span>
            </button>
            {CATEGORY_ICON_KEYS.map((key) => (
              <button
                key={key}
                type="button"
                role="radio"
                aria-checked={icon === key}
                aria-label={key}
                title={key}
                className={icon === key ? `${styles.iconChoice} ${styles.iconChoiceActive}` : styles.iconChoice}
                onClick={() => setIcon(key)}
              >
                {createElement(iconForKey(key), { size: 20, 'aria-hidden': true })}
              </button>
            ))}
          </div>
          <p className={styles.hint}>{t('categoryForm.iconHint')}</p>
        </fieldset>
      </form>
    </Dialog>
  );
}

interface RejectCategoryDialogProps {
  categoryName: string;
  onReject: (reason: string) => Promise<void>;
  onClose: () => void;
}

const MAX_REASON = 300;

/** The reason is shown to the user who proposed the category. */
export function RejectCategoryDialog({ categoryName, onReject, onClose }: RejectCategoryDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const fieldId = useId();
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const trimmed = reason.trim();
    if (!trimmed) {
      setError(t('categoryReject.reasonRequired'));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await onReject(trimmed);
    } catch (failure) {
      setError(errorMessage(failure));
      setBusy(false);
    }
  }

  return (
    <Dialog
      title={t('categoryReject.title')}
      onClose={onClose}
      dismissible={!busy}
      footer={
        <div className={styles.footerButtons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button variant="danger" type="submit" form="category-reject-form" disabled={busy}>
            {busy ? t('categoryReject.rejecting') : t('categoryReject.confirm')}
          </Button>
        </div>
      }
    >
      <form id="category-reject-form" className={styles.form} onSubmit={(event) => void submit(event)} noValidate>
        <p className={styles.intro}>{t('categoryReject.text', { name: categoryName })}</p>
        <label htmlFor={fieldId} className={styles.legend}>
          {t('categoryReject.reason')}
        </label>
        <textarea
          id={fieldId}
          className={styles.textarea}
          rows={3}
          maxLength={MAX_REASON}
          value={reason}
          placeholder={t('categoryReject.placeholder')}
          onChange={(event) => {
            setReason(event.target.value);
            setError(null);
          }}
          aria-invalid={error !== null || undefined}
          disabled={busy}
          data-autofocus
        />
        <p className={styles.hint}>{`${reason.length}/${MAX_REASON}`}</p>
        {error && <Alert tone="error">{error}</Alert>}
      </form>
    </Dialog>
  );
}
