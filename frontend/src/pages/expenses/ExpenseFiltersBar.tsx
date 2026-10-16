import { useId, useState } from 'react';
import { Search, SlidersHorizontal, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { Category } from '../../api/types';
import { SelectField } from '../../components/ui/SelectField';
import { TextField } from '../../components/ui/TextField';
import { useMediaQuery } from '../../hooks/useMediaQuery';
import { todayDateOnly } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { hasActiveFilters, isDateRangeValid, type ExpenseFilters, type SortOption, type TypeOption } from './expenseFilters';
import styles from './ExpensesPage.module.css';

interface ExpenseFiltersBarProps {
  filters: ExpenseFilters;
  /** What is typed in the search box right now (the list follows it after a short pause). */
  searchText: string;
  onSearchTextChange: (value: string) => void;
  onChange: (change: Partial<ExpenseFilters>) => void;
  onClear: () => void;
  categories: Category[];
}

/**
 * Search and filters above the list. On a phone only the search box is always visible; the
 * other filters fold out, so the list starts near the top of the screen.
 */
export function ExpenseFiltersBar({
  filters,
  searchText,
  onSearchTextChange,
  onChange,
  onClear,
  categories,
}: ExpenseFiltersBarProps) {
  const { t } = useTranslation();
  const localize = useLocalizedText();
  const isWide = useMediaQuery('(min-width: 768px)');
  const [expanded, setExpanded] = useState(false);
  // No expense is in the future, so the date pickers stop at today.
  const [today] = useState(() => todayDateOnly());
  const moreId = useId();

  const active = hasActiveFilters({ ...filters, search: searchText });
  // How many filters besides the search are set - shown on the phone's "Filters" button.
  const hiddenCount = [filters.from, filters.to, filters.categoryId, filters.type !== 'all' ? 'x' : ''].filter(Boolean).length;
  const showMore = isWide || expanded;

  return (
    <section className={styles.filters} role="search" aria-label={t('expenses.filters.label')}>
      <div className={styles.searchRow}>
        <TextField
          compact
          type="search"
          label={t('expenses.filters.search')}
          placeholder={t('expenses.filters.searchPlaceholder')}
          leadingIcon={<Search size={18} />}
          value={searchText}
          maxLength={100}
          onChange={(event) => onSearchTextChange(event.target.value)}
          className={styles.searchField}
        />
        {!isWide && (
          <button
            type="button"
            className={styles.moreButton}
            aria-expanded={expanded}
            aria-controls={moreId}
            onClick={() => setExpanded((value) => !value)}
          >
            <SlidersHorizontal size={18} aria-hidden="true" />
            {t('expenses.filters.label')}
            {hiddenCount > 0 && <span className={styles.moreCount}>{hiddenCount}</span>}
          </button>
        )}
      </div>

      {showMore && (
        <div id={moreId} className={styles.moreFilters}>
          <TextField
            compact
            type="date"
            label={t('expenses.filters.from')}
            value={filters.from}
            max={filters.to && filters.to < today ? filters.to : today}
            onChange={(event) => onChange({ from: event.target.value })}
          />
          <TextField
            compact
            type="date"
            label={t('expenses.filters.to')}
            value={filters.to}
            min={filters.from || undefined}
            max={today}
            onChange={(event) => onChange({ to: event.target.value })}
            errors={isDateRangeValid(filters) ? [] : [t('expenses.filters.dateRange')]}
          />
          <SelectField
            compact
            label={t('expenses.filters.category')}
            value={filters.categoryId}
            onChange={(event) => onChange({ categoryId: event.target.value })}
            options={[
              { value: '', label: t('expenses.filters.allCategories') },
              ...categories.map((category) => ({ value: category.id, label: localize(category.name) })),
            ]}
          />
          <SelectField
            compact
            label={t('expenses.filters.type')}
            value={filters.type}
            onChange={(event) => onChange({ type: event.target.value as TypeOption })}
            options={[
              { value: 'all', label: t('expenses.filters.allTypes') },
              { value: 'recurring', label: t('expenses.filters.recurringOnly') },
              { value: 'oneTime', label: t('expenses.filters.oneTimeOnly') },
            ]}
          />
          <SelectField
            compact
            label={t('expenses.filters.sort')}
            value={filters.sort}
            onChange={(event) => onChange({ sort: event.target.value as SortOption })}
            options={[
              { value: 'newest', label: t('expenses.filters.newest') },
              { value: 'oldest', label: t('expenses.filters.oldest') },
              { value: 'highest', label: t('expenses.filters.highest') },
              { value: 'lowest', label: t('expenses.filters.lowest') },
            ]}
          />
        </div>
      )}

      {active && (
        <button type="button" className={styles.clearButton} onClick={onClear}>
          <X size={16} aria-hidden="true" />
          {t('expenses.filters.clear')}
        </button>
      )}
    </section>
  );
}
