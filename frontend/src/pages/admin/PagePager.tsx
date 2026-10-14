import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import styles from './Admin.module.css';

interface PagePagerProps {
  page: number;
  /** The backend does not return a total, so there is a next page while this page is full. */
  hasNext: boolean;
  onChange: (page: number) => void;
}

/** "Previous / Page 2 / Next" for lists without a known number of pages. */
export function PagePager({ page, hasNext, onChange }: PagePagerProps) {
  const { t } = useTranslation();
  if (page === 1 && !hasNext) return null;

  return (
    <nav className={styles.pager} aria-label={t('admin.pagination')}>
      <button type="button" className={styles.pagerButton} onClick={() => onChange(page - 1)} disabled={page <= 1}>
        <ChevronLeft size={18} aria-hidden="true" />
        {t('admin.previousPage')}
      </button>
      <span className={styles.pagerPage} aria-current="page">
        {t('admin.page', { page })}
      </span>
      <button type="button" className={styles.pagerButton} onClick={() => onChange(page + 1)} disabled={!hasNext}>
        {t('admin.nextPage')}
        <ChevronRight size={18} aria-hidden="true" />
      </button>
    </nav>
  );
}
