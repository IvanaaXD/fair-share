import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { groupsApi } from '../../api/groups';
import type { GroupMember } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { email as validateEmail, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { TextField } from '../../components/ui/TextField';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Groups.module.css';

interface InviteMemberDialogProps {
  groupId: string;
  onClose: () => void;
  onAdded: (member: GroupMember) => void;
}

/** Adds a registered user to the group by e-mail (POST /api/groups/{id}/members). */
export function InviteMemberDialog({ groupId, onClose, onAdded }: InviteMemberDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();

  const [value, setValue] = useState('');
  const [fieldError, setFieldError] = useState<ValidationKey | null>(null);
  const [serverError, setServerError] = useState<string[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerError([]);

    const problem = validateEmail(value);
    setFieldError(problem);
    if (problem) {
      return;
    }

    setSaving(true);
    try {
      const member = await groupsApi.addMember(groupId, { email: value.trim() });
      onAdded(member);
    } catch (error) {
      setSaving(false);
      if (error instanceof ApiError && error.fieldErrors.email) {
        setServerError(error.fieldErrors.email.map(localize));
      } else {
        setFormError(errorMessage(error, { 404: t('invite.notFound'), 409: t('invite.alreadyMember') }));
      }
    }
  }

  return (
    <Dialog
      title={t('invite.title')}
      onClose={onClose}
      dismissible={!saving}
      footer={
        <div className={styles.dialogButtons}>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="invite-form" disabled={saving}>
            {saving ? t('invite.submitting') : t('invite.submit')}
          </Button>
        </div>
      }
    >
      <form id="invite-form" className={styles.dialogForm} onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className={styles.dialogNote}>{t('invite.text')}</p>
        {formError && <Alert tone="error">{formError}</Alert>}
        <TextField
          label={t('invite.email')}
          type="email"
          name="email"
          inputMode="email"
          autoComplete="off"
          value={value}
          onChange={(event) => {
            setValue(event.target.value);
            setFieldError(null);
            setServerError([]);
          }}
          errors={fieldError ? [t(fieldError)] : serverError}
          disabled={saving}
          data-autofocus
        />
      </form>
    </Dialog>
  );
}
