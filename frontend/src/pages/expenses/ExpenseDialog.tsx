import { useState, type FormEvent } from 'react';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { expensesApi } from '../../api/expenses';
import { useErrorMessage } from '../../api/useErrorMessage';
import {
  RECURRENCE_INTERVALS,
  type Category,
  type Expense,
  type RecurrenceInterval,
  type SaveExpenseRequest,
} from '../../api/types';
import { onlyErrors, required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Checkbox } from '../../components/ui/Checkbox';
import { Dialog } from '../../components/ui/Dialog';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { todayDateOnly, toDateOnly, useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import {
  MAX_DESCRIPTION,
  MIN_DATE,
  parseAmount,
  validateAmount,
  validateDate,
  validateDescription,
  validateReceipt,
} from './expenseValidation';
import { LocationField, type Coordinates } from './LocationField';
import { ReceiptField } from './ReceiptField';
import styles from './ExpenseDialog.module.css';

export type ExpenseChange = 'added' | 'updated' | 'deleted';

interface ExpenseDialogProps {
  /** null = a new expense */
  expense: Expense | null;
  categories: Category[];
  categoriesFailed: boolean;
  defaultCurrency: string;
  onClose: () => void;
  /** The expense was saved or deleted and the dialog should close. */
  onDone: (change: ExpenseChange) => void;
  /** The list should be reloaded, but the dialog stays open (e.g. the receipt failed). */
  onChanged: () => void;
}

type Field = 'amount' | 'date' | 'categoryId' | 'description';

interface Values {
  amount: string;
  currency: string;
  date: string;
  categoryId: string;
  description: string;
  isRecurring: boolean;
  interval: Exclude<RecurrenceInterval, 'None'>;
}

const CURRENCIES = ['BAM', 'EUR', 'RSD'] as const;

/** 12.5 -> "12,50" in Serbian, "12.50" in English - the way the user would type it. */
function amountToText(amount: number, language: string): string {
  const text = amount.toFixed(2);
  return language.startsWith('sr') ? text.replace('.', ',') : text;
}

function initialValues(expense: Expense | null, defaultCurrency: string, language: string): Values {
  if (!expense) {
    return {
      amount: '',
      currency: defaultCurrency,
      date: todayDateOnly(),
      categoryId: '',
      description: '',
      isRecurring: false,
      interval: 'Monthly',
    };
  }
  return {
    amount: amountToText(expense.amount, language),
    currency: expense.currency,
    date: toDateOnly(expense.date),
    categoryId: expense.categoryId,
    description: expense.description ?? '',
    isRecurring: expense.isRecurring,
    interval: expense.recurrenceInterval === 'None' ? 'Monthly' : expense.recurrenceInterval,
  };
}

/** Adding and editing a personal expense, with its receipt photo; deleting from edit mode. */
export function ExpenseDialog({
  expense: initialExpense,
  categories,
  categoriesFailed,
  defaultCurrency,
  onClose,
  onDone,
  onChanged,
}: ExpenseDialogProps) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();

  // Becomes the saved expense if adding succeeded but the receipt upload did not, so that a
  // second "Save" edits that expense instead of creating a duplicate.
  const [expense, setExpense] = useState(initialExpense);
  const [values, setValues] = useState(() => initialValues(initialExpense, defaultCurrency, i18n.language));
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [phase, setPhase] = useState<'editing' | 'saving' | 'confirmDelete' | 'deleting'>('editing');

  const [location, setLocation] = useState<Coordinates | null>(() =>
    initialExpense?.latitude != null && initialExpense.longitude != null
      ? { latitude: initialExpense.latitude, longitude: initialExpense.longitude }
      : null,
  );
  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [receiptRemoved, setReceiptRemoved] = useState(false);
  const [receiptError, setReceiptError] = useState<string | null>(null);

  const isNew = expense === null;
  const busy = phase === 'saving' || phase === 'deleting';

  const set = <K extends keyof Values>(field: K, value: Values[K]) => {
    setValues((current) => ({ ...current, [field]: value }));
    setFieldErrors((current) => ({ ...current, [field]: undefined }));
    setServerErrors((current) => ({ ...current, [field]: [] }));
  };

  const errorsFor = (field: Field) => {
    const clientError = fieldErrors[field];
    if (clientError) {
      return [t(clientError)];
    }
    return (serverErrors[field] ?? []).map(localize);
  };

  // A currency the expense already has stays selectable even if it is not one of the usual three.
  const currencyOptions = [...new Set<string>([...CURRENCIES, values.currency])].map((code) => ({
    value: code,
    label:
      code === 'BAM'
        ? t('expenseForm.currencyBam')
        : code === 'EUR'
          ? t('expenseForm.currencyEur')
          : code === 'RSD'
            ? t('expenseForm.currencyRsd')
            : code,
  }));

  const categoryOptions = [
    { value: '', label: t('expenseForm.chooseCategory') },
    ...categories.map((category) => ({ value: category.id, label: localize(category.name) })),
  ];

  // Errors the server reports for fields that have no input of their own (e.g. "Location").
  const otherServerErrors = Object.entries(serverErrors)
    .filter(([field]) => !['amount', 'date', 'categoryId', 'description', 'currency'].includes(field))
    .flatMap(([, messages]) => messages)
    .map(localize);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerErrors({});

    const errors = onlyErrors({
      amount: validateAmount(values.amount),
      date: validateDate(values.date),
      categoryId: required(values.categoryId),
      description: validateDescription(values.description),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    const request: SaveExpenseRequest = {
      amount: parseAmount(values.amount) ?? 0,
      currency: values.currency,
      date: values.date,
      categoryId: values.categoryId,
      description: values.description.trim() || null,
      latitude: location?.latitude ?? null,
      longitude: location?.longitude ?? null,
      isRecurring: values.isRecurring,
      recurrenceInterval: values.isRecurring ? values.interval : 'None',
    };

    setPhase('saving');
    let saved: Expense;
    try {
      saved = expense ? await expensesApi.update(expense.id, request) : await expensesApi.create(request);
    } catch (error) {
      setPhase('editing');
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error, { 404: t('expenseForm.notFound') }));
      }
      return;
    }

    // The expense is saved; now the receipt, which has its own endpoint.
    try {
      if (receiptFile) {
        await expensesApi.uploadReceipt(saved.id, receiptFile);
      } else if (receiptRemoved && saved.receiptImageUrl) {
        await expensesApi.removeReceipt(saved.id);
      }
    } catch (error) {
      setExpense(saved);
      setPhase('editing');
      setFormError(t('expenseForm.receiptFailed', { reason: errorMessage(error) }));
      onChanged();
      return;
    }

    onDone(isNew ? 'added' : 'updated');
  }

  async function handleDelete() {
    if (!expense) {
      return;
    }
    setPhase('deleting');
    setFormError(null);
    try {
      await expensesApi.remove(expense.id);
      onDone('deleted');
    } catch (error) {
      // Already deleted elsewhere - the goal is reached, the list just has to be refreshed.
      if (error instanceof ApiError && error.status === 404) {
        onDone('deleted');
        return;
      }
      setPhase('confirmDelete');
      setFormError(errorMessage(error));
    }
  }

  const footer =
    phase === 'confirmDelete' || phase === 'deleting' ? (
      <div
        className={styles.confirm}
        role="group"
        aria-labelledby="delete-question"
        aria-describedby={expense?.isRecurring ? 'delete-note' : undefined}
      >
        <div className={styles.confirmText}>
          <p id="delete-question" className={styles.confirmQuestion}>
            {t('expenseForm.deleteQuestion')}
          </p>
          {expense?.isRecurring && (
            <p id="delete-note" className={styles.confirmNote}>
              {t('expenseForm.deleteRecurringNote')}
            </p>
          )}
        </div>
        <div className={styles.footerButtons}>
          <Button variant="secondary" onClick={() => setPhase('editing')} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button variant="danger" onClick={() => void handleDelete()} disabled={busy} autoFocus>
            {phase === 'deleting' ? t('expenseForm.deleting') : t('expenseForm.deleteConfirm')}
          </Button>
        </div>
      </div>
    ) : (
      <>
        {!isNew && (
          <button
            type="button"
            className={styles.deleteButton}
            onClick={() => setPhase('confirmDelete')}
            disabled={busy}
          >
            <Trash2 size={18} aria-hidden="true" />
            {t('expenseForm.delete')}
          </button>
        )}
        <div className={styles.footerButtons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="expense-form" disabled={busy}>
            {phase === 'saving' ? t('common.saving') : t('common.save')}
          </Button>
        </div>
      </>
    );

  return (
    <Dialog
      title={isNew ? t('expenseForm.addTitle') : t('expenseForm.editTitle')}
      onClose={onClose}
      dismissible={!busy}
      footer={footer}
    >
      <form id="expense-form" className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
        {formError && <Alert tone="error">{formError}</Alert>}
        {otherServerErrors.length > 0 && <Alert tone="error">{otherServerErrors.join(' ')}</Alert>}
        {categoriesFailed && <Alert tone="error">{t('expenseForm.categoriesError')}</Alert>}

        <div className={styles.amountRow}>
          <TextField
            label={t('expenseForm.amount')}
            name="amount"
            inputMode="decimal"
            autoComplete="off"
            placeholder={amountToText(0, i18n.language)}
            value={values.amount}
            onChange={(event) => set('amount', event.target.value)}
            errors={errorsFor('amount')}
            className={styles.amountField}
            disabled={busy}
            data-autofocus={isNew || undefined}
          />
          <SelectField
            label={t('expenseForm.currency')}
            name="currency"
            value={values.currency}
            onChange={(event) => set('currency', event.target.value)}
            options={currencyOptions}
            errors={(serverErrors.currency ?? []).map(localize)}
            disabled={busy}
          />
        </div>

        <div className={styles.row}>
          <TextField
            label={t('expenseForm.date')}
            type="date"
            name="date"
            min={MIN_DATE}
            max={todayDateOnly()}
            value={values.date}
            onChange={(event) => set('date', event.target.value)}
            errors={errorsFor('date')}
            disabled={busy}
          />
          <SelectField
            label={t('expenseForm.category')}
            name="categoryId"
            value={values.categoryId}
            onChange={(event) => set('categoryId', event.target.value)}
            options={categoryOptions}
            errors={errorsFor('categoryId')}
            disabled={busy}
          />
        </div>

        <TextField
          label={`${t('expenseForm.description')} (${t('common.optional')})`}
          name="description"
          placeholder={t('expenseForm.descriptionPlaceholder')}
          maxLength={MAX_DESCRIPTION}
          value={values.description}
          onChange={(event) => set('description', event.target.value)}
          errors={errorsFor('description')}
          disabled={busy}
        />

        <div className={styles.recurring}>
          <Checkbox
            label={t('expenseForm.recurring')}
            checked={values.isRecurring}
            onChange={(event) => set('isRecurring', event.target.checked)}
            disabled={busy}
          />
          {values.isRecurring && (
            <>
              <p className={styles.hint}>{t('expenseForm.recurringHint')}</p>
              <SelectField
                label={t('expenseForm.interval')}
                name="recurrenceInterval"
                value={values.interval}
                onChange={(event) => set('interval', event.target.value as Values['interval'])}
                options={RECURRENCE_INTERVALS.map((interval) => ({ value: interval, label: t(`recurrence.${interval}`) }))}
                errors={(serverErrors.recurrenceInterval ?? []).map(localize)}
                disabled={busy}
              />
              {expense?.isRecurring && expense.nextOccurrenceDate && (
                <p className={styles.hint}>
                  {t('expenseForm.nextOccurrence', { date: format.longDate(expense.nextOccurrenceDate) })}
                </p>
              )}
            </>
          )}
        </div>

        <LocationField value={location} onChange={setLocation} disabled={busy} />

        <ReceiptField
          savedUrl={expense?.receiptImageUrl ?? null}
          file={receiptFile}
          removed={receiptRemoved}
          error={receiptError}
          disabled={busy}
          onSelect={(file) => {
            const problem = validateReceipt(file);
            setReceiptError(problem ? t(problem) : null);
            if (!problem) {
              setReceiptFile(file);
            }
          }}
          onClearFile={() => {
            setReceiptFile(null);
            setReceiptError(null);
          }}
          onRemoveSaved={() => setReceiptRemoved(true)}
          onUndoRemove={() => setReceiptRemoved(false)}
        />
      </form>
    </Dialog>
  );
}
