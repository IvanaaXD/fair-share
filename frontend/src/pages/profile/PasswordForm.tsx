import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { profileApi } from '../../api/profile';
import { useErrorMessage } from '../../api/useErrorMessage';
import { matches, newPassword, onlyErrors, required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/TextField';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Profile.module.css';

type Field = 'currentPassword' | 'newPassword' | 'confirmPassword';
const EMPTY = { currentPassword: '', newPassword: '', confirmPassword: '' };

interface PasswordFormProps {
  submitLabel: string;
  onChanged: () => void | Promise<void>;
}

/** Current + new password, with the same rules as the backend (PasswordRules). */
export function PasswordForm({ submitLabel, onChanged }: PasswordFormProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();

  const [values, setValues] = useState(EMPTY);
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const update = (field: Field) => (event: { target: { value: string } }) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
    setServerErrors((current) => ({ ...current, [field]: [] }));
  };
  const errorsFor = (field: Field) => {
    const clientError = fieldErrors[field];
    return clientError ? [t(clientError)] : (serverErrors[field] ?? []).map(localize);
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerErrors({});

    const errors = onlyErrors({
      currentPassword: required(values.currentPassword),
      newPassword:
        newPassword(values.newPassword) ??
        (values.newPassword === values.currentPassword ? ('validation.passwordSame' as ValidationKey) : null),
      confirmPassword: matches(values.confirmPassword, values.newPassword),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    setSaving(true);
    try {
      await profileApi.changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword });
      setValues(EMPTY);
      await onChanged();
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else if (error instanceof ApiError && error.status === 403) {
        // ForbiddenException("Тренутна лозинка није тачна.") - shown at the field it is about.
        setServerErrors({ currentPassword: [t('profile.wrongPassword')] });
      } else {
        setFormError(errorMessage(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
      {formError && <Alert tone="error">{formError}</Alert>}
      <TextField
        label={t('profile.currentPassword')}
        type="password"
        name="currentPassword"
        autoComplete="current-password"
        value={values.currentPassword}
        onChange={update('currentPassword')}
        errors={errorsFor('currentPassword')}
        disabled={saving}
      />
      <TextField
        label={t('auth.newPassword')}
        type="password"
        name="newPassword"
        autoComplete="new-password"
        hint={t('auth.passwordHint')}
        value={values.newPassword}
        onChange={update('newPassword')}
        errors={errorsFor('newPassword')}
        disabled={saving}
      />
      <TextField
        label={t('auth.confirmPassword')}
        type="password"
        name="confirmPassword"
        autoComplete="new-password"
        value={values.confirmPassword}
        onChange={update('confirmPassword')}
        errors={errorsFor('confirmPassword')}
        disabled={saving}
      />
      <div className={styles.formActions}>
        <Button type="submit" disabled={saving}>
          {saving ? t('profile.changing') : submitLabel}
        </Button>
      </div>
    </form>
  );
}
