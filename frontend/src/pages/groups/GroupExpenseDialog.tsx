import { useMemo, useState, type FormEvent } from 'react';
import { CircleAlert, CircleCheck, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/ApiError';
import { groupExpensesApi } from '../../api/groupExpenses';
import type { Category, Group, GroupExpense, SaveGroupExpenseRequest } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { onlyErrors, required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { initials, isOwnerRole, memberColor, membersByJoinDate, splitTypeName } from '../../groups/groupUtils';
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
} from '../expenses/expenseValidation';
import { ReceiptField } from '../expenses/ReceiptField';
import { computeSplit, decimalText, equalCents, equalPercents, moneyText, toCents, type SplitMode, type SplitStatus } from './splitMath';
import dialogStyles from '../expenses/ExpenseDialog.module.css';
import styles from './GroupExpenseDialog.module.css';

export type GroupExpenseChange = 'added' | 'updated' | 'deleted';

interface GroupExpenseDialogProps {
  group: Group;
  /** null = a new expense */
  expense: GroupExpense | null;
  categories: Category[];
  currentUserId: string;
  onClose: () => void;
  onDone: (change: GroupExpenseChange) => void;
  /** Saved, but the dialog stays open (the receipt failed) - the page should still refresh. */
  onChanged: () => void;
}

type Field = 'amount' | 'date' | 'categoryId' | 'paidByUserId' | 'description';

const MODES: SplitMode[] = ['Equal', 'Percentage', 'ExactAmount'];

/**
 * Adding or editing a group expense: who paid, how much, and how it is split between the
 * members - equally, by percentages or by exact amounts - with a live preview of every share.
 */
export function GroupExpenseDialog({
  group,
  expense: initialExpense,
  categories,
  currentUserId,
  onClose,
  onDone,
  onChanged,
}: GroupExpenseDialogProps) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const language = i18n.language;

  const members = useMemo(() => membersByJoinDate(group), [group]);
  const me = members.find((member) => member.userId === currentUserId);

  const [expense, setExpense] = useState(initialExpense);
  const isNew = expense === null;
  // The backend allows changes only to the member who paid and to the group owner.
  const canEdit = isNew || expense.paidByUserId === currentUserId || (me ? isOwnerRole(me.role) : false);

  const [description, setDescription] = useState(initialExpense?.description ?? '');
  const [amount, setAmount] = useState(() =>
    initialExpense ? moneyText(initialExpense.amount, language) : '',
  );
  const [paidBy, setPaidBy] = useState(initialExpense?.paidByUserId ?? currentUserId);
  const [categoryId, setCategoryId] = useState(initialExpense?.categoryId ?? '');
  const [date, setDate] = useState(initialExpense ? toDateOnly(initialExpense.date) : todayDateOnly());
  const [mode, setMode] = useState<SplitMode>(initialExpense ? splitTypeName(initialExpense.splitType) : 'Equal');

  // Who takes part. A new expense starts with everyone; an edit with the saved participants.
  const [included, setIncluded] = useState<Set<string>>(
    () => new Set(initialExpense ? initialExpense.splits.map((split) => split.userId) : members.map((m) => m.userId)),
  );
  const [percentText, setPercentText] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      (initialExpense?.splits ?? [])
        .filter((split) => split.percentage !== null)
        .map((split) => [split.userId, decimalText(split.percentage as number, language)]),
    ),
  );
  const [exactText, setExactText] = useState<Record<string, string>>(() =>
    Object.fromEntries((initialExpense?.splits ?? []).map((split) => [split.userId, moneyText(split.amount, language)])),
  );

  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, ValidationKey>>>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [splitTouched, setSplitTouched] = useState(false);
  const [phase, setPhase] = useState<'editing' | 'saving' | 'confirmDelete' | 'deleting'>('editing');

  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [receiptRemoved, setReceiptRemoved] = useState(false);
  const [receiptError, setReceiptError] = useState<string | null>(null);

  const busy = phase === 'saving' || phase === 'deleting';
  const disabled = busy || !canEdit;

  const includedIds = members.map((member) => member.userId).filter((id) => included.has(id));
  const amountValue = validateAmount(amount) ? null : parseAmount(amount);
  const totalCents = amountValue === null ? null : toCents(amountValue);
  const preview = computeSplit(totalCents, mode, includedIds, percentText, exactText);
  const colorOf = new Map(members.map((member, index) => [member.userId, memberColor(index)]));
  const money = (cents: number) => format.money(cents / 100, group.currency);

  const clearServerError = (field: string) => setServerErrors((current) => ({ ...current, [field]: [] }));
  const errorsFor = (field: Field) => {
    const clientError = fieldErrors[field];
    return clientError ? [t(clientError)] : (serverErrors[field] ?? []).map(localize);
  };

  /** Switching to percentages or exact amounts fills in an equal split as a starting point. */
  function changeMode(next: SplitMode) {
    setMode(next);
    clearServerError('participants');
    if (next === 'Percentage') {
      const percents = equalPercents(includedIds.length);
      setPercentText(Object.fromEntries(includedIds.map((id, index) => [id, decimalText(percents[index], language)])));
    }
    if (next === 'ExactAmount' && totalCents !== null) {
      const parts = equalCents(totalCents, includedIds.length);
      setExactText(Object.fromEntries(includedIds.map((id, index) => [id, moneyText(parts[index] / 100, language)])));
    }
  }

  function toggle(userId: string) {
    setIncluded((current) => {
      const next = new Set(current);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
    clearServerError('participants');
  }

  function statusText(status: SplitStatus): string {
    const total = totalCents === null ? '' : money(totalCents);
    switch (status.kind) {
      case 'noAmount':
        return t('groupExpense.statusNoAmount');
      case 'tooFew':
        return t('groupExpense.statusTooFew');
      case 'equal':
        return status.remainder
          ? t('groupExpense.statusEqualRemainder', { amount: money(status.eachCents) })
          : t('groupExpense.statusEqual', { amount: money(status.eachCents) });
      case 'percentOk':
        return t('groupExpense.statusPercentOk');
      case 'percentInvalid':
        return t('groupExpense.statusPercentInvalid');
      case 'percentWrong':
        return t('groupExpense.statusPercentWrong', { sum: decimalText(status.sum, language) });
      case 'exactOk':
        return t('groupExpense.statusExactOk', { total });
      case 'exactInvalid':
        return t('groupExpense.statusExactInvalid');
      case 'exactMissing':
        return t('groupExpense.statusExactMissing', { sum: money(status.sumCents), total, diff: money(status.diffCents) });
      case 'exactOver':
        return t('groupExpense.statusExactOver', { sum: money(status.sumCents), diff: money(status.diffCents) });
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canEdit) return;
    setFormError(null);
    setServerErrors({});
    setSplitTouched(true);

    const errors = onlyErrors({
      amount: validateAmount(amount),
      date: validateDate(date),
      categoryId: required(categoryId),
      paidByUserId: paidBy ? null : ('validation.paidByRequired' as ValidationKey),
      description: validateDescription(description),
    });
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0 || !preview.valid) {
      return;
    }

    const request: SaveGroupExpenseRequest = {
      categoryId,
      paidByUserId: paidBy,
      amount: (totalCents as number) / 100,
      description: description.trim() || null,
      date,
      splitType: mode,
      participants: includedIds.map((userId) => ({
        userId,
        amount: mode === 'ExactAmount' ? (preview.shares.get(userId) ?? 0) / 100 : null,
        percentage: mode === 'Percentage' ? parseAmount(percentText[userId] ?? '') : null,
      })),
    };

    setPhase('saving');
    let saved: GroupExpense;
    try {
      saved = expense
        ? await groupExpensesApi.update(group.id, expense.id, request)
        : await groupExpensesApi.create(group.id, request);
    } catch (error) {
      setPhase('editing');
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error));
      }
      return;
    }

    try {
      if (receiptFile) {
        await groupExpensesApi.uploadReceipt(group.id, saved.id, receiptFile);
      } else if (receiptRemoved && saved.receiptImageUrl) {
        await groupExpensesApi.removeReceipt(group.id, saved.id);
      }
    } catch (error) {
      setExpense(saved);
      setPhase('editing');
      setFormError(t('groupExpense.receiptFailed', { reason: errorMessage(error) }));
      onChanged();
      return;
    }

    onDone(isNew ? 'added' : 'updated');
  }

  async function handleDelete() {
    if (!expense) return;
    setPhase('deleting');
    setFormError(null);
    try {
      await groupExpensesApi.remove(group.id, expense.id);
      onDone('deleted');
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        onDone('deleted');
        return;
      }
      setPhase('confirmDelete');
      setFormError(errorMessage(error));
    }
  }

  // Server errors about the split (sum of percentages...) arrive under "participants".
  const splitServerErrors = (serverErrors.participants ?? []).map(localize);
  const otherServerErrors = Object.entries(serverErrors)
    .filter(([field]) => !['amount', 'date', 'categoryId', 'paidByUserId', 'description', 'participants'].includes(field))
    .flatMap(([, messages]) => messages)
    .map(localize);
  const showSplitProblem = !preview.valid && (splitTouched || preview.status.kind === 'percentWrong' || preview.status.kind.startsWith('exact'));

  const footer =
    phase === 'confirmDelete' || phase === 'deleting' ? (
      <div className={dialogStyles.confirm} role="group" aria-labelledby="group-expense-delete" aria-describedby="group-expense-delete-note">
        <div className={dialogStyles.confirmText}>
          <p id="group-expense-delete" className={dialogStyles.confirmQuestion}>
            {t('groupExpense.deleteQuestion')}
          </p>
          <p id="group-expense-delete-note" className={dialogStyles.confirmNote}>
            {t('groupExpense.deleteNote')}
          </p>
        </div>
        <div className={dialogStyles.footerButtons}>
          <Button variant="secondary" onClick={() => setPhase('editing')} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button variant="danger" onClick={() => void handleDelete()} disabled={busy} autoFocus>
            {phase === 'deleting' ? t('groupExpense.deleting') : t('groupExpense.deleteConfirm')}
          </Button>
        </div>
      </div>
    ) : (
      <>
        {!isNew && canEdit && (
          <button type="button" className={dialogStyles.deleteButton} onClick={() => setPhase('confirmDelete')} disabled={busy}>
            <Trash2 size={18} aria-hidden="true" />
            {t('groupExpense.delete')}
          </button>
        )}
        <div className={dialogStyles.footerButtons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {canEdit ? t('common.cancel') : t('common.close')}
          </Button>
          {canEdit && (
            <Button type="submit" form="group-expense-form" disabled={busy}>
              {phase === 'saving' ? t('common.saving') : t('common.save')}
            </Button>
          )}
        </div>
      </>
    );

  const categoryOptions = [
    { value: '', label: t('expenseForm.chooseCategory') },
    ...categories.map((category) => ({ value: category.id, label: localize(category.name) })),
  ];
  // A category or payer that is no longer in the lists stays selectable when editing.
  if (expense && !categories.some((category) => category.id === expense.categoryId)) {
    categoryOptions.push({ value: expense.categoryId, label: localize(expense.categoryName) });
  }
  const payerOptions = members.map((member) => ({
    value: member.userId,
    label:
      member.userId === currentUserId
        ? t('groupExpense.me', { name: localize(member.firstName) })
        : localize(`${member.firstName} ${member.lastName}`),
  }));
  if (expense && !members.some((member) => member.userId === expense.paidByUserId)) {
    payerOptions.push({ value: expense.paidByUserId, label: localize(expense.paidByName) });
  }

  return (
    <Dialog
      title={isNew ? t('groupExpense.addTitle') : t('groupExpense.editTitle')}
      onClose={onClose}
      dismissible={!busy}
      footer={footer}
      size="lg"
    >
      <form id="group-expense-form" className={dialogStyles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className={styles.groupName}>{localize(group.name)}</p>
        {!canEdit && <Alert tone="info">{t('groupExpense.readOnly')}</Alert>}
        {formError && <Alert tone="error">{formError}</Alert>}
        {otherServerErrors.length > 0 && <Alert tone="error">{otherServerErrors.join(' ')}</Alert>}

        <div className={styles.topRow}>
          <TextField
            label={`${t('groupExpense.description')} (${t('common.optional')})`}
            name="description"
            placeholder={t('groupExpense.descriptionPlaceholder')}
            maxLength={MAX_DESCRIPTION}
            value={description}
            onChange={(event) => {
              setDescription(event.target.value);
              setFieldErrors((current) => ({ ...current, description: undefined }));
            }}
            errors={errorsFor('description')}
            disabled={disabled}
          />
          <TextField
            label={`${t('groupExpense.amount')} (${group.currency})`}
            name="amount"
            inputMode="decimal"
            autoComplete="off"
            placeholder={language.startsWith('sr') ? '0,00' : '0.00'}
            value={amount}
            onChange={(event) => {
              setAmount(event.target.value);
              setFieldErrors((current) => ({ ...current, amount: undefined }));
              clearServerError('amount');
            }}
            errors={errorsFor('amount')}
            className={dialogStyles.amountField}
            disabled={disabled}
            data-autofocus={isNew || undefined}
          />
        </div>

        <div className={styles.threeRow}>
          <SelectField
            label={t('groupExpense.paidBy')}
            name="paidByUserId"
            value={paidBy}
            onChange={(event) => {
              setPaidBy(event.target.value);
              setFieldErrors((current) => ({ ...current, paidByUserId: undefined }));
            }}
            options={payerOptions}
            errors={errorsFor('paidByUserId')}
            disabled={disabled}
          />
          <SelectField
            label={t('groupExpense.category')}
            name="categoryId"
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setFieldErrors((current) => ({ ...current, categoryId: undefined }));
            }}
            options={categoryOptions}
            errors={errorsFor('categoryId')}
            disabled={disabled}
          />
          <TextField
            label={t('groupExpense.date')}
            type="date"
            name="date"
            min={MIN_DATE}
            max={todayDateOnly()}
            value={date}
            onChange={(event) => {
              setDate(event.target.value);
              setFieldErrors((current) => ({ ...current, date: undefined }));
            }}
            errors={errorsFor('date')}
            disabled={disabled}
          />
        </div>

        <fieldset className={styles.split} disabled={disabled}>
          <legend className={dialogStyles.legend}>{t('groupExpense.split')}</legend>
          <div className={styles.modes} role="group" aria-label={t('groupExpense.split')}>
            {MODES.map((value) => (
              <button
                key={value}
                type="button"
                className={mode === value ? `${styles.mode} ${styles.modeActive}` : styles.mode}
                aria-pressed={mode === value}
                onClick={() => changeMode(value)}
              >
                {t(value === 'Equal' ? 'groupExpense.modeEqual' : value === 'Percentage' ? 'groupExpense.modePercentage' : 'groupExpense.modeExact')}
              </button>
            ))}
          </div>
          <p className={dialogStyles.hint}>{t('groupExpense.participantsHint')}</p>

          <ul className={styles.people}>
            {members.map((member) => {
              const checked = included.has(member.userId);
              const name = member.userId === currentUserId
                ? t('groupExpense.me', { name: localize(member.firstName) })
                : localize(`${member.firstName} ${member.lastName}`);
              const share = preview.shares.get(member.userId);
              const inputId = `split-${member.userId}`;
              return (
                <li key={member.userId} className={checked ? styles.person : `${styles.person} ${styles.personOff}`}>
                  <input
                    id={inputId}
                    type="checkbox"
                    className={styles.check}
                    checked={checked}
                    onChange={() => toggle(member.userId)}
                  />
                  <Avatar
                    size="sm"
                    initials={localize(initials(member.firstName, member.lastName))}
                    color={colorOf.get(member.userId) ?? memberColor(0)}
                  />
                  <label htmlFor={inputId} className={styles.personName}>
                    {name}
                  </label>
                  {checked && mode !== 'Equal' && (
                    <span className={styles.shareInput}>
                      <input
                        type="text"
                        inputMode="decimal"
                        aria-label={t(mode === 'Percentage' ? 'groupExpense.percentLabel' : 'groupExpense.exactLabel', { name })}
                        value={(mode === 'Percentage' ? percentText : exactText)[member.userId] ?? ''}
                        onChange={(event) => {
                          const setter = mode === 'Percentage' ? setPercentText : setExactText;
                          setter((current) => ({ ...current, [member.userId]: event.target.value }));
                          clearServerError('participants');
                        }}
                      />
                      <span aria-hidden="true">{mode === 'Percentage' ? '%' : group.currency}</span>
                    </span>
                  )}
                  <span className={`${styles.shareAmount} money`}>{checked && share !== undefined ? money(share) : '–'}</span>
                </li>
              );
            })}
          </ul>

          <div className={styles.bar} role="img" aria-label={t('groupExpense.splitBar')}>
            {includedIds.map((id) => {
              const cents = preview.shares.get(id) ?? 0;
              return cents > 0 ? <span key={id} style={{ flexGrow: cents, background: colorOf.get(id) }} /> : null;
            })}
          </div>
          <p
            className={`${styles.status} ${preview.valid ? styles.statusOk : showSplitProblem ? styles.statusError : ''}`}
            aria-live="polite"
          >
            {preview.valid ? (
              <CircleCheck size={18} aria-hidden="true" />
            ) : showSplitProblem ? (
              <CircleAlert size={18} aria-hidden="true" />
            ) : null}
            {statusText(preview.status)}
          </p>
          {splitServerErrors.length > 0 && <p className={dialogStyles.fieldError}>{splitServerErrors.join(' ')}</p>}
        </fieldset>

        <ReceiptField
          savedUrl={expense?.receiptImageUrl ?? null}
          file={receiptFile}
          removed={receiptRemoved}
          error={receiptError}
          disabled={busy}
          readOnly={!canEdit}
          hint={t('groupExpense.receiptHint')}
          onSelect={(file) => {
            const problem = validateReceipt(file);
            setReceiptError(problem ? t(problem) : null);
            if (!problem) setReceiptFile(file);
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
