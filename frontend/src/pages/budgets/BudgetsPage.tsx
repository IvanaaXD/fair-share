import { createElement, useEffect, useState, type FormEvent } from 'react';
import { ChevronLeft, ChevronRight, Copy, History, Pencil, PiggyBank, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/ApiError';
import { budgetsApi } from '../../api/budgets';
import type { Budget, Category } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { useAuth } from '../../auth/useAuth';
import { required, type ValidationKey } from '../../auth/validation';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { Dialog } from '../../components/ui/Dialog';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { useCategories } from '../../hooks/useCategories';
import { addMonths, currentMonth, useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { categoryIcon } from '../expenses/categoryIcons';
import { parseAmount, validateAmount } from '../expenses/expenseValidation';
import { decimalText, moneyText } from '../groups/splitMath';
import pageStyles from '../Page.module.css';
import styles from './Budgets.module.css';

/** From 80% on the bar turns amber, like the warning notification the backend sends. */
const WARNING_PERCENT = 80;

type Level = 'ok' | 'warning' | 'over';

function levelOf(budget: Budget): Level {
  if (budget.percentageUsed >= 100) return 'over';
  if (budget.percentageUsed >= WARNING_PERCENT) return 'warning';
  return 'ok';
}

const MONTH_PATTERN = /^\d{4}-(0[1-9]|1[0-2])$/;

/** The same limit as the backend (Budget.MaxMonthsAhead). */
const MAX_MONTHS_AHEAD = 12;

type DialogState =
  | { kind: 'add' }
  | { kind: 'edit'; budget: Budget }
  | { kind: 'delete'; budget: Budget }
  | null;

/** Monthly limits per category, with how much of each is already spent. */
export function BudgetsPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const { user } = useAuth();
  const { categories } = useCategories();
  const currency = user?.defaultCurrency ?? 'BAM';
  const money = (amount: number) => format.money(amount, currency);

  // The month is in the address (?month=2026-09), so Back and a refresh keep it.
  // Past months can only be viewed (the backend refuses changes too); the future is limited
  // to a year ahead. Months are compared as "YYYY-MM" text, which sorts like dates.
  const [params, setParams] = useSearchParams();
  const thisMonth = currentMonth();
  const lastMonth = addMonths(thisMonth, MAX_MONTHS_AHEAD);
  const requested = params.get('month') ?? '';
  const month = MONTH_PATTERN.test(requested) && requested <= lastMonth ? requested : thisMonth;
  const isPast = month < thisMonth;
  const [notice, setNotice] = useState<{ id: number; text: string } | null>(null);
  const setMonth = (value: string) => {
    // A message about the previous month ("Budget deleted") would be confusing here.
    setNotice(null);
    setParams(value === thisMonth ? {} : { month: value }, { replace: true, preventScrollReset: true });
  };

  const [version, setVersion] = useState(0);
  const [loaded, setLoaded] = useState<{ key: string; budgets: Budget[] | null; error: unknown } | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);
  const [copying, setCopying] = useState(false);
  const [copyError, setCopyError] = useState<string | null>(null);
  const key = `${month}:${version}`;

  useEffect(() => {
    const controller = new AbortController();
    budgetsApi.mine(month, controller.signal).then(
      (budgets) => setLoaded({ key, budgets, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded({ key, budgets: null, error });
      },
    );
    return () => controller.abort();
  }, [month, key]);

  // Last month's budgets, offered for copying when this month has none yet.
  const previousMonth = addMonths(month, -1);
  const [previous, setPrevious] = useState<{ month: string; budgets: Budget[] } | null>(null);
  const budgets = loaded?.key === key ? loaded.budgets : loaded?.budgets ?? null;
  const isEmpty = loaded?.key === key && budgets !== null && budgets.length === 0;

  useEffect(() => {
    if (!isEmpty || isPast) return;
    const controller = new AbortController();
    budgetsApi.mine(previousMonth, controller.signal).then(
      (result) => setPrevious({ month: previousMonth, budgets: result }),
      () => undefined,
    );
    return () => controller.abort();
  }, [isEmpty, isPast, previousMonth]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 5000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  const done = (text: string) => {
    setDialog(null);
    setNotice({ id: Date.now(), text });
    setVersion((value) => value + 1);
  };

  async function copyPrevious(source: Budget[]) {
    setCopying(true);
    setCopyError(null);
    let copied = 0;
    try {
      // One request per budget; a category that already has a budget (409) is simply skipped.
      for (const budget of source) {
        try {
          await budgetsApi.create({ categoryId: budget.categoryId, monthlyLimit: budget.monthlyLimit, month });
          copied++;
        } catch (error) {
          if (!(error instanceof ApiError && error.status === 409)) throw error;
        }
      }
      done(t('budgets.copied', { count: copied }));
    } catch (error) {
      setCopyError(errorMessage(error));
      setVersion((value) => value + 1);
    } finally {
      setCopying(false);
    }
  }

  const iconKeys = new Map(categories.map((category) => [category.id, category.icon]));
  const totalLimit = (budgets ?? []).reduce((sum, budget) => sum + budget.monthlyLimit, 0);
  const totalSpent = (budgets ?? []).reduce((sum, budget) => sum + budget.currentSpending, 0);
  const sorted = [...(budgets ?? [])].sort((a, b) => b.percentageUsed - a.percentageUsed);
  const previousForCopy = previous?.month === previousMonth ? previous.budgets : [];

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('budgets.title')}</h1>
          <p className={pageStyles.subtitle}>{t('budgets.subtitle')}</p>
        </div>
        {!isPast && (
          <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setDialog({ kind: 'add' })}>
            {t('budgets.add')}
          </Button>
        )}
      </header>

      <div className={styles.monthBar} role="group" aria-label={t('budgets.monthLabel')}>
        <button type="button" className={styles.monthButton} aria-label={t('budgets.previousMonth')} onClick={() => setMonth(addMonths(month, -1))}>
          <ChevronLeft size={20} aria-hidden="true" />
        </button>
        <span className={styles.monthName} aria-live="polite">
          {format.month(month)}
        </span>
        <button
          type="button"
          className={styles.monthButton}
          aria-label={t('budgets.nextMonth')}
          title={month >= lastMonth ? t('budgets.maxAhead', { count: MAX_MONTHS_AHEAD }) : undefined}
          onClick={() => setMonth(addMonths(month, 1))}
          disabled={month >= lastMonth}
        >
          <ChevronRight size={20} aria-hidden="true" />
        </button>
      </div>

      {isPast && (
        <p className={styles.pastNote}>
          <History size={17} aria-hidden="true" />
          <span>{t('budgets.pastNote')}</span>
          <button type="button" className={styles.linkButton} onClick={() => setMonth(thisMonth)}>
            {t('budgets.toCurrentMonth')}
          </button>
        </p>
      )}

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && (
          <Alert key={notice.id} tone="success">
            {notice.text}
          </Alert>
        )}
      </div>

      {loaded?.error != null && loaded.key === key && (
        <Alert tone="error">
          {t('budgets.loadError')} {errorMessage(loaded.error)}
        </Alert>
      )}

      {budgets && budgets.length > 0 && (
        <section className={styles.totals} aria-label={t('budgets.totalLimit')}>
          <div>
            <span className={styles.totalLabel}>{t('budgets.totalSpent')}</span>
            <span className={`${styles.totalValue} money`}>{money(totalSpent)}</span>
          </div>
          <div>
            <span className={styles.totalLabel}>{t('budgets.totalLimit')}</span>
            <span className={`${styles.totalValue} money`}>{money(totalLimit)}</span>
          </div>
          <div>
            <span className={styles.totalLabel}>{t('budgets.totalLeft')}</span>
            <span className={`${styles.totalValue} money ${totalSpent > totalLimit ? styles.overText : ''}`}>
              {money(totalLimit - totalSpent)}
            </span>
          </div>
        </section>
      )}

      {isEmpty && (
        <section className={pageStyles.empty} aria-labelledby="budgets-empty-title">
          <span className={pageStyles.emptyIcon} aria-hidden="true">
            <PiggyBank size={28} strokeWidth={1.75} />
          </span>
          <h2 id="budgets-empty-title" className={pageStyles.emptyTitle}>
            {t(isPast ? 'budgets.emptyPastTitle' : 'budgets.emptyTitle')}
          </h2>
          <p className={pageStyles.emptyText}>{t(isPast ? 'budgets.emptyPastText' : 'budgets.emptyText')}</p>
          {copyError && <Alert tone="error">{copyError}</Alert>}
          {!isPast && (
            <div className={pageStyles.actions}>
              <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setDialog({ kind: 'add' })}>
                {t('budgets.add')}
              </Button>
              {previousForCopy.length > 0 && (
                <Button
                  variant="secondary"
                  icon={<Copy size={17} aria-hidden="true" />}
                  onClick={() => void copyPrevious(previousForCopy)}
                  disabled={copying}
                >
                  {copying ? t('budgets.copying') : t('budgets.copyFrom', { month: format.month(previousMonth) })}
                </Button>
              )}
            </div>
          )}
        </section>
      )}

      {sorted.length > 0 && (
        <ul className={styles.list} aria-busy={loaded?.key !== key}>
          {sorted.map((budget) => {
            const level = levelOf(budget);
            const name = localize(budget.categoryName);
            const remaining = budget.monthlyLimit - budget.currentSpending;
            const percent = Math.round(budget.percentageUsed);
            return (
              <li key={budget.id} className={`${styles.card} ${styles[`card_${level}`]}`}>
                <div className={styles.cardTop}>
                  <span className={styles.icon} aria-hidden="true">
                    {createElement(categoryIcon(budget.categoryName, iconKeys.get(budget.categoryId)), { size: 20 })}
                  </span>
                  <h2 className={styles.name}>{name}</h2>
                  {!isPast && (
                    <div className={styles.cardActions}>
                      <button
                        type="button"
                        className={styles.iconButton}
                        aria-label={t('budgets.edit', { name })}
                        title={t('budgets.edit', { name })}
                        onClick={() => setDialog({ kind: 'edit', budget })}
                      >
                        <Pencil size={17} aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        className={`${styles.iconButton} ${styles.danger}`}
                        aria-label={t('budgets.delete', { name })}
                        title={t('budgets.delete', { name })}
                        onClick={() => setDialog({ kind: 'delete', budget })}
                      >
                        <Trash2 size={17} aria-hidden="true" />
                      </button>
                    </div>
                  )}
                </div>
                <div className={styles.cardMeta}>
                  <p className={styles.status}>
                    {t(
                      level === 'over'
                        ? remaining < -0.005
                          ? 'budgets.statusOver'
                          : 'budgets.statusReached'
                        : level === 'warning'
                          ? 'budgets.statusWarning'
                          : 'budgets.statusOk',
                    )}
                  </p>
                  <span className={`${styles.percent} money`}>{t('budgets.percent', { percent })}</span>
                </div>
                <div
                  className={styles.track}
                  role="progressbar"
                  aria-label={name}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={Math.min(percent, 100)}
                  aria-valuetext={t('budgets.spentOf', { spent: money(budget.currentSpending), limit: money(budget.monthlyLimit) })}
                >
                  <span className={styles.fill} style={{ width: `${Math.min(budget.percentageUsed, 100)}%` }} />
                  <span className={styles.mark} aria-hidden="true" />
                </div>
                <div className={styles.cardBottom}>
                  <span>{t('budgets.spentOf', { spent: money(budget.currentSpending), limit: money(budget.monthlyLimit) })}</span>
                  <span className={remaining < 0 ? styles.overText : undefined}>
                    {remaining < 0 ? t('budgets.over', { amount: money(-remaining) }) : t('budgets.left', { amount: money(remaining) })}
                  </span>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      {budgets && budgets.length > 0 && <p className={styles.note}>{t('budgets.note')}</p>}

      {dialog?.kind === 'add' && (
        <BudgetDialog
          month={month}
          monthLabel={format.month(month)}
          currency={currency}
          categories={categories.filter((category) => !(budgets ?? []).some((budget) => budget.categoryId === category.id))}
          onClose={() => setDialog(null)}
          onSaved={() => done(t('budgets.added'))}
        />
      )}
      {dialog?.kind === 'edit' && (
        <BudgetDialog
          budget={dialog.budget}
          month={month}
          monthLabel={format.month(month)}
          currency={currency}
          categories={[]}
          onClose={() => setDialog(null)}
          onSaved={() => done(t('budgets.updated'))}
        />
      )}
      {dialog?.kind === 'delete' && (
        <ConfirmDialog
          title={t('budgetForm.deleteTitle')}
          confirmLabel={t('budgetForm.deleteConfirm')}
          busyLabel={t('confirm.deleting')}
          danger
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await budgetsApi.remove(dialog.budget.id);
            done(t('budgets.deleted'));
          }}
        >
          <p>{t('budgetForm.deleteText', { name: localize(dialog.budget.categoryName), month: format.month(month) })}</p>
        </ConfirmDialog>
      )}
    </div>
  );
}

interface BudgetDialogProps {
  /** Present when editing - then only the limit can change. */
  budget?: Budget;
  month: string;
  monthLabel: string;
  currency: string;
  /** Categories that can still get a budget this month. */
  categories: Category[];
  onClose: () => void;
  onSaved: () => void;
}

function BudgetDialog({ budget, month, monthLabel, currency, categories, onClose, onSaved }: BudgetDialogProps) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const [categoryId, setCategoryId] = useState('');
  const [limit, setLimit] = useState(budget ? moneyText(budget.monthlyLimit, i18n.language) : '');
  const [errors, setErrors] = useState<{ categoryId?: ValidationKey | null; limit?: ValidationKey | null }>({});
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setServerErrors({});
    const next = { categoryId: budget ? null : required(categoryId), limit: validateAmount(limit) };
    setErrors(next);
    if (next.categoryId || next.limit) return;

    const monthlyLimit = parseAmount(limit) ?? 0;
    setSaving(true);
    try {
      if (budget) {
        await budgetsApi.update(budget.id, { monthlyLimit });
      } else {
        await budgetsApi.create({ categoryId, monthlyLimit, month });
      }
      onSaved();
    } catch (error) {
      setSaving(false);
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setServerErrors(error.fieldErrors);
      } else {
        setFormError(errorMessage(error));
      }
    }
  }

  const noCategories = !budget && categories.length === 0;

  return (
    <Dialog
      title={budget ? t('budgetForm.editTitle', { name: localize(budget.categoryName) }) : t('budgetForm.addTitle')}
      onClose={onClose}
      dismissible={!saving}
      footer={
        <div className={styles.dialogButtons}>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="budget-form" disabled={saving || noCategories}>
            {saving ? t('common.saving') : t('common.save')}
          </Button>
        </div>
      }
    >
      <form id="budget-form" className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className={styles.dialogMonth}>{t('budgetForm.month', { month: monthLabel })}</p>
        {formError && <Alert tone="error">{formError}</Alert>}
        {noCategories && <Alert tone="info">{t('budgetForm.allBudgeted')}</Alert>}
        {!budget && !noCategories && (
          <SelectField
            label={t('budgetForm.category')}
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setErrors((current) => ({ ...current, categoryId: null }));
            }}
            options={[
              { value: '', label: t('expenseForm.chooseCategory') },
              ...categories.map((category) => ({ value: category.id, label: localize(category.name) })),
            ]}
            errors={errors.categoryId ? [t(errors.categoryId)] : (serverErrors.categoryId ?? []).map(localize)}
            disabled={saving}
          />
        )}
        {!noCategories && (
          <TextField
            label={`${t('budgetForm.limit')} (${currency})`}
            name="monthlyLimit"
            inputMode="decimal"
            autoComplete="off"
            placeholder={decimalText(200, i18n.language)}
            value={limit}
            onChange={(event) => {
              setLimit(event.target.value);
              setErrors((current) => ({ ...current, limit: null }));
            }}
            errors={errors.limit ? [t(errors.limit)] : (serverErrors.monthlyLimit ?? []).map(localize)}
            disabled={saving}
            data-autofocus={budget ? true : undefined}
          />
        )}
      </form>
    </Dialog>
  );
}
