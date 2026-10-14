import { createElement } from 'react';
import { ArrowDownRight, ArrowUpRight, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { CategorySpending } from '../../api/types';
import { useCategories } from '../../hooks/useCategories';
import { toDateOnly, useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { categoryIcon } from '../expenses/categoryIcons';
import styles from './Analytics.module.css';

interface CategoryBreakdownProps {
  categories: CategorySpending[];
  currency: string;
  /** The period, so a row can open the expenses of that category in it. */
  from: string;
  to: string;
}

/**
 * Categories ranked by amount. Bars are drawn relative to the largest category, so small
 * differences stay visible; the share of the total is written next to it. One colour for all -
 * the name says which category it is, the length says how much.
 */
export function CategoryBreakdown({ categories, currency, from, to }: CategoryBreakdownProps) {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const { categories: allCategories } = useCategories();
  const iconKeys = new Map(allCategories.map((category) => [category.id, category.icon]));
  const largest = Math.max(0, ...categories.map((category) => category.total));

  return (
    <ul className={styles.categoryList}>
      {categories.map((category) => {
        const name = localize(category.categoryName);
        const query = new URLSearchParams({ category: category.categoryId, from: toDateOnly(from), to: toDateOnly(to) });
        return (
          <li key={category.categoryId}>
            <Link to={`/expenses?${query.toString()}`} className={styles.categoryRow}>
              <span className={styles.categoryIcon} aria-hidden="true">
                {createElement(categoryIcon(category.categoryName, iconKeys.get(category.categoryId)), { size: 18 })}
              </span>
              <span className={styles.categoryMain}>
                <span className={styles.categoryTop}>
                  <span className={styles.categoryName}>{name}</span>
                  <span className={`${styles.categoryAmount} money`}>{format.money(category.total, currency)}</span>
                </span>
                <span className={styles.categoryTrack} aria-hidden="true">
                  <span
                    className={styles.categoryFill}
                    style={{ width: `${largest > 0 ? Math.max(1.5, (category.total / largest) * 100) : 0}%` }}
                  />
                </span>
                <span className={styles.categoryMeta}>
                  <span>
                    {t('analytics.share', { percent: format.percent(category.percentage) })} ·{' '}
                    {t('analytics.expenseCount', { count: category.count })}
                  </span>
                  <Change current={category.total} previous={category.previousPeriodTotal} />
                </span>
              </span>
              <ChevronRight size={18} aria-hidden="true" className={styles.categoryChevron} />
            </Link>
          </li>
        );
      })}
    </ul>
  );
}

/** Change against the previous period. More spending is shown in amber, less in petrol - with an arrow and a word, not colour alone. */
export function Change({ current, previous }: { current: number; previous: number }) {
  const { t } = useTranslation();
  const format = useFormatters();

  if (!(previous > 0)) {
    return current > 0 ? <span className={styles.changeNew}>{t('analytics.changeNew')}</span> : null;
  }
  const percent = ((current - previous) / previous) * 100;
  if (Math.abs(percent) < 0.5) {
    return <span className={styles.changeSame}>{t('analytics.changeSame')}</span>;
  }
  const up = percent > 0;
  const text = format.percent(Math.abs(percent));
  return (
    <span className={up ? styles.changeUp : styles.changeDown}>
      {up ? <ArrowUpRight size={15} aria-hidden="true" /> : <ArrowDownRight size={15} aria-hidden="true" />}
      {up ? t('analytics.changeMore', { percent: text }) : t('analytics.changeLess', { percent: text })}
    </span>
  );
}
