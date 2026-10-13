import { Pencil, ReceiptText, Repeat } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { Expense } from '../../api/types';
import { useMediaQuery } from '../../hooks/useMediaQuery';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { categoryIcon } from './categoryIcons';
import styles from './ExpensesPage.module.css';

interface ExpenseListProps {
  expenses: Expense[];
  onEdit: (expense: Expense) => void;
}

/** A table on wider screens; on a phone a list of cards, each one opening the expense. */
export function ExpenseList({ expenses, onEdit }: ExpenseListProps) {
  const isWide = useMediaQuery('(min-width: 768px)');
  return isWide ? <ExpenseTable expenses={expenses} onEdit={onEdit} /> : <ExpenseCards expenses={expenses} onEdit={onEdit} />;
}

function useExpenseText() {
  const { t } = useTranslation();
  const localize = useLocalizedText();
  const format = useFormatters();

  return {
    title: (expense: Expense) => localize(expense.description?.trim() || expense.categoryName),
    category: (expense: Expense) => localize(expense.categoryName),
    amount: (expense: Expense) => format.money(expense.amount, expense.currency),
    date: (expense: Expense) => format.shortDate(expense.date),
    repeats: (expense: Expense) =>
      expense.isRecurring && expense.recurrenceInterval !== 'None' ? t(`repeats.${expense.recurrenceInterval}`) : null,
    t,
  };
}

/** Small icons that say "repeats" and "has a receipt"; the text is there for screen readers. */
function Markers({ expense, repeats }: { expense: Expense; repeats: string | null }) {
  const { t } = useTranslation();
  return (
    <>
      {repeats && (
        <span className={styles.marker} title={repeats}>
          <Repeat size={16} aria-hidden="true" />
          <span className="visually-hidden">{repeats}</span>
        </span>
      )}
      {expense.receiptImageUrl && (
        <span className={styles.marker} title={t('expenses.hasReceipt')}>
          <ReceiptText size={16} aria-hidden="true" />
          <span className="visually-hidden">{t('expenses.hasReceipt')}</span>
        </span>
      )}
    </>
  );
}

function ExpenseTable({ expenses, onEdit }: ExpenseListProps) {
  const text = useExpenseText();
  const { t } = text;

  return (
    <div className={styles.tableScroll}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th scope="col">{t('expenses.table.date')}</th>
            <th scope="col">{t('expenses.table.description')}</th>
            <th scope="col">{t('expenses.table.category')}</th>
            <th scope="col" className={styles.amountCell}>
              {t('expenses.table.amount')}
            </th>
            <th scope="col" className={styles.actionsCell}>
              <span className="visually-hidden">{t('expenses.table.actions')}</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {expenses.map((expense) => {
            const Icon = categoryIcon(expense.categoryName);
            const hasDescription = Boolean(expense.description?.trim());
            const repeats = text.repeats(expense);
            return (
              <tr key={expense.id}>
                <td className={styles.dateCell}>
                  <time dateTime={expense.date.slice(0, 10)}>{text.date(expense)}</time>
                </td>
                <td className={hasDescription ? styles.titleCell : `${styles.titleCell} ${styles.noDescription}`}>
                  {text.title(expense)}
                </td>
                <td>
                  <span className={styles.chip}>
                    <Icon size={14} aria-hidden="true" />
                    {text.category(expense)}
                  </span>
                </td>
                <td className={`${styles.amountCell} money`}>{text.amount(expense)}</td>
                <td className={styles.actionsCell}>
                  <div className={styles.rowActions}>
                    <Markers expense={expense} repeats={repeats} />
                    <button
                      type="button"
                      className={styles.editButton}
                      aria-label={t('expenses.edit', { name: text.title(expense) })}
                      onClick={() => onEdit(expense)}
                    >
                      <Pencil size={17} aria-hidden="true" />
                    </button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function ExpenseCards({ expenses, onEdit }: ExpenseListProps) {
  const text = useExpenseText();

  return (
    <ul className={styles.cards}>
      {expenses.map((expense) => {
        const Icon = categoryIcon(expense.categoryName);
        const repeats = text.repeats(expense);
        return (
          <li key={expense.id}>
            <button
              type="button"
              className={styles.card}
              onClick={() => onEdit(expense)}
            >
              <span className={styles.cardIcon} aria-hidden="true">
                <Icon size={20} />
              </span>
              <span className={styles.cardMain}>
                <span className={styles.cardTitle}>{text.title(expense)}</span>
                <span className={styles.cardMeta}>
                  <time dateTime={expense.date.slice(0, 10)}>{text.date(expense)}</time>
                  <span aria-hidden="true">·</span>
                  <span className={styles.cardCategory}>{text.category(expense)}</span>
                  <Markers expense={expense} repeats={repeats} />
                </span>
              </span>
              <span className={`${styles.cardAmount} money`}>{text.amount(expense)}</span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
