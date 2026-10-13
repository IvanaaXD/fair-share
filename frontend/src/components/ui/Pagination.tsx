import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import styles from './Pagination.module.css';

interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

/** Page numbers to show: always the first and last, the current one and its neighbours. */
function pageWindow(page: number, totalPages: number): (number | 'gap')[] {
  const pages = new Set([1, totalPages, page - 1, page, page + 1]);
  const sorted = [...pages].filter((value) => value >= 1 && value <= totalPages).sort((a, b) => a - b);

  const result: (number | 'gap')[] = [];
  for (const value of sorted) {
    const previous = result[result.length - 1];
    if (typeof previous === 'number' && value - previous > 1) {
      // A single missing page is shown as a number; a gap only replaces two or more pages.
      result.push(value - previous === 2 ? previous + 1 : 'gap');
    }
    result.push(value);
  }
  return result;
}

export function Pagination({ page, totalPages, onChange }: PaginationProps) {
  const { t } = useTranslation();

  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav className={styles.pagination} aria-label={t('pagination.label')}>
      <button
        type="button"
        className={styles.button}
        aria-label={t('pagination.previous')}
        disabled={page <= 1}
        onClick={() => onChange(page - 1)}
      >
        <ChevronLeft size={18} aria-hidden="true" />
      </button>
      {pageWindow(page, totalPages).map((value, index) =>
        value === 'gap' ? (
          <span key={`gap-${index}`} className={styles.gap} aria-hidden="true">
            …
          </span>
        ) : (
          <button
            key={value}
            type="button"
            className={`${styles.button} ${value === page ? styles.current : ''}`}
            aria-current={value === page ? 'page' : undefined}
            aria-label={t('pagination.page', { page: value })}
            onClick={() => onChange(value)}
          >
            {value}
          </button>
        ),
      )}
      <button
        type="button"
        className={styles.button}
        aria-label={t('pagination.next')}
        disabled={page >= totalPages}
        onClick={() => onChange(page + 1)}
      >
        <ChevronRight size={18} aria-hidden="true" />
      </button>
    </nav>
  );
}
