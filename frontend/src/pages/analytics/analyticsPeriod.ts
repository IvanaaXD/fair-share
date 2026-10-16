import type { AnalyticsPeriod } from '../../api/types';
import { todayDateOnly } from '../../i18n/format';

/** The ready-made periods, in the order of the buttons; "Custom" comes last. */
export const PRESETS: Exclude<AnalyticsPeriod, 'Custom'>[] = ['LastWeek', 'LastMonth', 'Last3Months', 'Last6Months', 'LastYear'];

export const DEFAULT_PERIOD: AnalyticsPeriod = 'LastMonth';

const ALL: AnalyticsPeriod[] = [...PRESETS, 'Custom'];
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export interface PeriodSelection {
  period: AnalyticsPeriod;
  /** "YYYY-MM-DD", only for Custom */
  from: string;
  to: string;
}

/**
 * The period lives in the address (?period=Last3Months, ?period=Custom&from=...&to=...), so a
 * refresh or a shared link shows the same numbers. Anything invalid falls back to the default.
 */
export function readPeriod(params: URLSearchParams): PeriodSelection {
  const period = params.get('period') as AnalyticsPeriod | null;
  if (period === 'Custom') {
    const from = params.get('from') ?? '';
    const to = params.get('to') ?? '';
    if (DATE_PATTERN.test(from) && DATE_PATTERN.test(to) && validateCustomRange(from, to) === null) {
      return { period, from, to };
    }
    return { period: DEFAULT_PERIOD, from: '', to: '' };
  }
  return { period: period && ALL.includes(period) ? period : DEFAULT_PERIOD, from: '', to: '' };
}

export function periodToParams(selection: PeriodSelection): Record<string, string> {
  if (selection.period === 'Custom') return { period: 'Custom', from: selection.from, to: selection.to };
  return selection.period === DEFAULT_PERIOD ? {} : { period: selection.period };
}

export type RangeError = 'required' | 'order' | 'future' | 'tooLong';

/**
 * The same rules as the backend (AnalyticsPeriodResolver): both dates, in order, at most a year
 * apart. The end cannot be in the future either - there are no expenses there yet.
 */
export function validateCustomRange(from: string, to: string, today = todayDateOnly()): RangeError | null {
  if (!from || !to) return 'required';
  if (from > to) return 'order';
  if (to > today) return 'future';
  const limit = new Date(`${from}T00:00:00Z`);
  limit.setUTCFullYear(limit.getUTCFullYear() + 1);
  if (new Date(`${to}T00:00:00Z`) > limit) return 'tooLong';
  return null;
}

/** Number of days in a period, both ends included. */
export function daysBetween(from: string, to: string): number {
  const start = Date.parse(`${from.slice(0, 10)}T00:00:00Z`);
  const end = Date.parse(`${to.slice(0, 10)}T00:00:00Z`);
  return Math.round((end - start) / 86_400_000) + 1;
}
