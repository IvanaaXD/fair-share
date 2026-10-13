import type { ExpenseQuery } from '../../api/types';

/**
 * The filters of the expenses page live in the address (?q=stan&from=2026-10-01&page=2), so a
 * filtered list survives a refresh, can be bookmarked, and the Back button undoes a change.
 */
export type SortOption = 'newest' | 'oldest' | 'highest' | 'lowest';
export type TypeOption = 'all' | 'recurring' | 'oneTime';

export interface ExpenseFilters {
  search: string;
  /** "YYYY-MM-DD" or "" */
  from: string;
  to: string;
  categoryId: string;
  type: TypeOption;
  sort: SortOption;
  page: number;
}

export const PAGE_SIZE = 20;

export const DEFAULT_FILTERS: ExpenseFilters = {
  search: '',
  from: '',
  to: '',
  categoryId: '',
  type: 'all',
  sort: 'newest',
  page: 1,
};

const SORTS: SortOption[] = ['newest', 'oldest', 'highest', 'lowest'];
const TYPES: TypeOption[] = ['all', 'recurring', 'oneTime'];
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function pick<T extends string>(value: string | null, allowed: T[], fallback: T): T {
  return allowed.includes(value as T) ? (value as T) : fallback;
}

/** Anything unexpected in the address (typed by hand, an old link) falls back to the default. */
export function readFilters(params: URLSearchParams): ExpenseFilters {
  const date = (key: string) => {
    const value = params.get(key) ?? '';
    return DATE_PATTERN.test(value) ? value : '';
  };
  const page = Number.parseInt(params.get('page') ?? '', 10);
  const categoryId = params.get('category') ?? '';

  return {
    search: (params.get('q') ?? '').slice(0, 100),
    from: date('from'),
    to: date('to'),
    categoryId: GUID_PATTERN.test(categoryId) ? categoryId : '',
    type: pick(params.get('type'), TYPES, 'all'),
    sort: pick(params.get('sort'), SORTS, 'newest'),
    page: Number.isFinite(page) && page > 1 ? page : 1,
  };
}

/** Only values that differ from the defaults go into the address, to keep it short. */
export function writeFilters(filters: ExpenseFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.search.trim()) params.set('q', filters.search.trim());
  if (filters.from) params.set('from', filters.from);
  if (filters.to) params.set('to', filters.to);
  if (filters.categoryId) params.set('category', filters.categoryId);
  if (filters.type !== 'all') params.set('type', filters.type);
  if (filters.sort !== 'newest') params.set('sort', filters.sort);
  if (filters.page > 1) params.set('page', String(filters.page));
  return params;
}

export function hasActiveFilters(filters: ExpenseFilters): boolean {
  return Boolean(filters.search.trim() || filters.from || filters.to || filters.categoryId || filters.type !== 'all');
}

/** "From" after "To" would be rejected by the backend, so such a range is not sent at all. */
export function isDateRangeValid(filters: ExpenseFilters): boolean {
  return !filters.from || !filters.to || filters.from <= filters.to;
}

export function toExpenseQuery(filters: ExpenseFilters): ExpenseQuery {
  const validRange = isDateRangeValid(filters);
  return {
    page: filters.page,
    pageSize: PAGE_SIZE,
    search: filters.search.trim() || undefined,
    from: validRange ? filters.from || undefined : undefined,
    to: validRange ? filters.to || undefined : undefined,
    categoryId: filters.categoryId || undefined,
    isRecurring: filters.type === 'all' ? undefined : filters.type === 'recurring',
    sortBy: filters.sort === 'highest' || filters.sort === 'lowest' ? 'Amount' : 'Date',
    sortDescending: filters.sort === 'newest' || filters.sort === 'highest',
  };
}
