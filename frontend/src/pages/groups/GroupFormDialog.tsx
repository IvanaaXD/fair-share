import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { groupsApi } from '../../api/groups';
import type { Group } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import styles from './Groups.module.css';

const NAME_MAX = 100;
const DESCRIPTION_MAX = 500;

interface GroupFormDialogProps {
  /** null = a new group */
  group: Group | null;
  defaultCurrency: string;
  onClose: () => void;
  onSaved: (group: Group) => void;
}

/** Creating a group, or changing its name and description (owner only). */
export function GroupFormDialog({ group, defaultCurrency, onClose, onSaved }: GroupFormDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();

  const [name, setName] = useState(group?.name ?? '');
  const [description, setDescription] = useState(group?.description ?? '');
  const [currency, setCurrency] = useState(group?.currency ?? defaultCurrency);
  const [nameError, setNameError] = useState<ValidationKey | null>(null);
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const isNew = group === null;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerErrors({});

    const problem = required(name) ?? (name.trim().length > NAME_MAX ? 'validation.nameTooLong' : null);
    setNameError(problem);
    if (problem) {
      return;
    }

    setSaving(true);
    try {
      const body = { name: name.trim(), description: description.trim() || null };
      const saved = group
        ? await groupsApi.update(group.id, body)
        : await groupsApi.create({ ...body, currency });
      onSaved(saved);
    } catch (error) {
      setSaving(false);
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error));
      }
    }
  }

  const fieldErrors = (field: string) => (serverErrors[field] ?? []).map(localize);

  return (
    <Dialog
      title={isNew ? t('groupForm.createTitle') : t('groupForm.editTitle')}
      onClose={onClose}
      dismissible={!saving}
      footer={
        <div className={styles.dialogButtons}>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="group-form" disabled={saving}>
            {saving
              ? isNew
                ? t('groupForm.creating')
                : t('common.saving')
              : isNew
                ? t('groupForm.create')
                : t('common.save')}
          </Button>
        </div>
      }
    >
      <form id="group-form" className={styles.dialogForm} onSubmit={(event) => void handleSubmit(event)} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}
        <TextField
          label={t('groupForm.name')}
          name="name"
          placeholder={t('groupForm.namePlaceholder')}
          maxLength={NAME_MAX}
          value={name}
          onChange={(event) => {
            setName(event.target.value);
            setNameError(null);
          }}
          errors={nameError ? [t(nameError)] : fieldErrors('name')}
          disabled={saving}
          data-autofocus
        />
        <TextField
          label={`${t('groupForm.description')} (${t('common.optional')})`}
          name="description"
          maxLength={DESCRIPTION_MAX}
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          errors={fieldErrors('description')}
          disabled={saving}
        />
        {isNew ? (
          <SelectField
            label={t('groupForm.currency')}
            hint={t('groupForm.currencyHint')}
            value={currency}
            onChange={(event) => setCurrency(event.target.value)}
            options={[
              { value: 'BAM', label: t('auth.currencyBam') },
              { value: 'EUR', label: t('auth.currencyEur') },
              { value: 'RSD', label: t('auth.currencyRsd') },
            ]}
            errors={fieldErrors('currency')}
            disabled={saving}
          />
        ) : (
          <p className={styles.dialogNote}>{t('groupForm.currencyFixed', { currency: group.currency })}</p>
        )}
      </form>
    </Dialog>
  );
}
