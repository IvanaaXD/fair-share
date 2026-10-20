import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ImagePlus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { profileApi } from '../../api/profile';
import type { Profile } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { name as validateName, onlyErrors, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { UserAvatar } from '../../components/ui/UserAvatar';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { RECEIPT_TYPES, validateReceipt } from '../expenses/expenseValidation';
import { formatBankAccount, validateBankAccount } from './bankAccount';
import { CategoryProposalsSection } from '../categories/CategoryProposalsSection';
import { PaymentCardsSection } from '../payments/PaymentCardsSection';
import { isAdminRole } from '../../auth/roles';
import { PasswordForm } from './PasswordForm';
import pageStyles from '../Page.module.css';
import styles from './Profile.module.css';

type Field = 'firstName' | 'lastName' | 'bankAccountNumber';

function initialValues(user: Profile) {
  return {
    firstName: user.firstName,
    lastName: user.lastName,
    defaultCurrency: user.defaultCurrency,
    bankAccountNumber: formatBankAccount(user.bankAccountNumber),
  };
}

export function ProfilePage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [notice, setNotice] = useState<{ id: number; text: string } | null>(null);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 5000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  if (!user) {
    return null;
  }
  const show = (text: string) => setNotice({ id: Date.now(), text });

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('profile.title')}</h1>
          <p className={pageStyles.subtitle}>{t('profile.subtitle')}</p>
        </div>
      </header>

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && (
          <Alert key={notice.id} tone="success">
            {notice.text}
          </Alert>
        )}
      </div>

      <div className={styles.columns}>
        <div className={styles.column}>
          <PhotoCard user={user} onNotice={show} />
          <section className={styles.card} aria-labelledby="password-title">
            <h2 id="password-title" className={styles.cardTitle}>
              {t('profile.passwordTitle')}
            </h2>
            <PasswordForm submitLabel={t('profile.changePassword')} onChanged={() => show(t('profile.passwordChanged'))} />
          </section>
        </div>
        <div className={styles.column}>
          <DetailsCard key={user.id} user={user} onNotice={show} />
          <PaymentCardsSection onNotice={show} cardClassName={styles.card} titleClassName={styles.cardTitle} />
          {/* An administrator adds categories directly in the administration. */}
          {!isAdminRole(user.role) && (
            <CategoryProposalsSection onNotice={show} cardClassName={styles.card} titleClassName={styles.cardTitle} />
          )}
        </div>
      </div>
    </div>
  );
}

function PhotoCard({ user, onNotice }: { user: Profile; onNotice: (text: string) => void }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const { setProfile } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function upload(file: File) {
    const problem = validateReceipt(file);
    if (problem) {
      setError(t(problem));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      setProfile(await profileApi.uploadImage(file));
      onNotice(t('profile.photoSaved'));
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    setBusy(true);
    setError(null);
    try {
      await profileApi.removeImage();
      setProfile({ ...user, profileImageUrl: null });
      onNotice(t('profile.photoRemoved'));
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className={styles.card} aria-labelledby="photo-title">
      <h2 id="photo-title" className={styles.cardTitle}>
        {t('profile.photoTitle')}
      </h2>
      <div className={styles.photoRow}>
        <UserAvatar
          size="xl"
          firstName={user.firstName}
          lastName={user.lastName}
          imageUrl={user.profileImageUrl}
          color="var(--member-3)"
        />
        <div className={styles.photoInfo}>
          <p className={styles.photoName}>{localize(`${user.firstName} ${user.lastName}`)}</p>
          <p className={styles.muted}>
            {user.email}
            {isAdminRole(user.role) && <span className={styles.badge}>{t('profile.roleAdmin')}</span>}
          </p>
          <div className={styles.photoActions}>
            <Button variant="secondary" icon={<ImagePlus size={17} aria-hidden="true" />} onClick={() => inputRef.current?.click()} disabled={busy}>
              {busy ? t('profile.photoUploading') : user.profileImageUrl ? t('profile.photoReplace') : t('profile.photoChoose')}
            </Button>
            {user.profileImageUrl && (
              <Button variant="secondary" icon={<Trash2 size={17} aria-hidden="true" />} onClick={() => void remove()} disabled={busy}>
                {t('profile.photoRemove')}
              </Button>
            )}
          </div>
        </div>
      </div>
      <p className={styles.muted}>{t('profile.photoHint')}</p>
      {error && <Alert tone="error">{error}</Alert>}
      <input
        ref={inputRef}
        type="file"
        accept={RECEIPT_TYPES.join(',')}
        className="visually-hidden"
        tabIndex={-1}
        aria-hidden="true"
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = '';
          if (file) void upload(file);
        }}
      />
    </section>
  );
}

function DetailsCard({ user, onNotice }: { user: Profile; onNotice: (text: string) => void }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const { setProfile } = useAuth();

  const [values, setValues] = useState(() => initialValues(user));
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const update = (field: keyof typeof values) => (event: { target: { value: string } }) => {
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
      firstName: validateName(values.firstName),
      lastName: validateName(values.lastName),
      bankAccountNumber: validateBankAccount(values.bankAccountNumber),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSaving(true);
    try {
      const saved = await profileApi.update({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        defaultCurrency: values.defaultCurrency,
        bankAccountNumber: values.bankAccountNumber.trim() || null,
      });
      setProfile(saved);
      setValues(initialValues(saved));
      onNotice(t('profile.saved'));
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className={styles.card} aria-labelledby="details-title">
      <h2 id="details-title" className={styles.cardTitle}>
        {t('profile.detailsTitle')}
      </h2>
      <form className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}
        <div className={styles.row}>
          <TextField
            label={t('auth.firstName')}
            name="firstName"
            autoComplete="given-name"
            value={values.firstName}
            onChange={update('firstName')}
            errors={errorsFor('firstName')}
            disabled={saving}
          />
          <TextField
            label={t('auth.lastName')}
            name="lastName"
            autoComplete="family-name"
            value={values.lastName}
            onChange={update('lastName')}
            errors={errorsFor('lastName')}
            disabled={saving}
          />
        </div>
        <TextField label={t('profile.email')} name="email" value={user.email} readOnly hint={t('profile.emailHint')} />
        <SelectField
          label={t('auth.currency')}
          hint={t('profile.currencyHint')}
          value={values.defaultCurrency}
          onChange={update('defaultCurrency')}
          options={[...new Set(['BAM', 'EUR', 'RSD', values.defaultCurrency])].map((code) => ({
            value: code,
            label: code === 'BAM' ? t('auth.currencyBam') : code === 'EUR' ? t('auth.currencyEur') : code === 'RSD' ? t('auth.currencyRsd') : code,
          }))}
          errors={(serverErrors.defaultCurrency ?? []).map(localize)}
          disabled={saving}
        />

        <h3 className={styles.subTitle}>{t('profile.bankTitle')}</h3>
        {!user.bankAccountNumber && <Alert tone="info">{t('profile.bankMissing')}</Alert>}
        <TextField
          label={`${t('profile.bankAccount')} (${t('common.optional')})`}
          name="bankAccountNumber"
          inputMode="numeric"
          autoComplete="off"
          placeholder={t('profile.bankPlaceholder')}
          hint={t('profile.bankHint')}
          value={values.bankAccountNumber}
          onChange={update('bankAccountNumber')}
          errors={errorsFor('bankAccountNumber')}
          className={styles.mono}
          disabled={saving}
        />

        <div className={styles.formActions}>
          <Button type="submit" disabled={saving}>
            {saving ? t('common.saving') : t('common.save')}
          </Button>
        </div>
      </form>
    </section>
  );
}
