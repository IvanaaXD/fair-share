/**
 * Live preview of how a group expense is split, calculated in whole cents so that the parts
 * always add up exactly. The backend (ExpenseSplitCalculator) calculates the real split again
 * when the expense is saved - this only shows the user what will happen and blocks a split
 * that the backend would reject (CreateGroupExpenseRequestValidator).
 */
import type { SplitType } from '../../api/types';
import { parseAmount } from '../expenses/expenseValidation';

export type SplitMode = SplitType;

/** The backend accepts a difference of up to 0.01 (one cent / 0.01 %). */
const PERCENT_TOLERANCE = 0.01;
const CENT_TOLERANCE = 1;

export type SplitStatus =
  | { kind: 'noAmount' }
  | { kind: 'tooFew' }
  | { kind: 'equal'; eachCents: number; remainder: boolean }
  | { kind: 'percentOk' }
  | { kind: 'percentInvalid' }
  | { kind: 'percentWrong'; sum: number }
  | { kind: 'exactOk' }
  | { kind: 'exactInvalid' }
  | { kind: 'exactMissing'; sumCents: number; diffCents: number }
  | { kind: 'exactOver'; sumCents: number; diffCents: number };

export interface SplitPreview {
  /** Cents per included member, in the order of `included`. */
  shares: Map<string, number>;
  status: SplitStatus;
  /** true when the split may be sent to the backend. */
  valid: boolean;
}

export function toCents(amount: number): number {
  return Math.round(amount * 100);
}

/** "33,5" / "33.5" -> 33.5; null when empty or not a number. */
export function parseNumber(text: string): number | null {
  return parseAmount(text);
}

/** Equal parts; the cents that do not divide evenly go to the first members, one each. */
export function equalCents(totalCents: number, count: number): number[] {
  if (count <= 0) return [];
  const base = Math.floor(totalCents / count);
  const remainder = totalCents - base * count;
  return Array.from({ length: count }, (_, index) => base + (index < remainder ? 1 : 0));
}

export function computeSplit(
  totalCents: number | null,
  mode: SplitMode,
  included: string[],
  percentText: Record<string, string>,
  exactText: Record<string, string>,
): SplitPreview {
  const shares = new Map<string, number>();

  if (included.length < 2) {
    return { shares, status: { kind: 'tooFew' }, valid: false };
  }
  if (totalCents === null || totalCents <= 0) {
    return { shares, status: { kind: 'noAmount' }, valid: false };
  }

  if (mode === 'Equal') {
    const parts = equalCents(totalCents, included.length);
    included.forEach((id, index) => shares.set(id, parts[index]));
    return {
      shares,
      status: { kind: 'equal', eachCents: parts[parts.length - 1], remainder: parts[0] !== parts[parts.length - 1] },
      valid: true,
    };
  }

  if (mode === 'Percentage') {
    const percents = included.map((id) => parseNumber(percentText[id] ?? ''));
    if (percents.some((value) => value === null || value <= 0 || value > 100)) {
      included.forEach((id, index) => shares.set(id, Math.round((totalCents * (percents[index] ?? 0)) / 100)));
      return { shares, status: { kind: 'percentInvalid' }, valid: false };
    }
    const values = percents as number[];
    const sum = Math.round(values.reduce((total, value) => total + value, 0) * 100) / 100;
    let assigned = 0;
    included.forEach((id, index) => {
      const cents = Math.round((totalCents * values[index]) / 100);
      shares.set(id, cents);
      assigned += cents;
    });
    if (Math.abs(sum - 100) > PERCENT_TOLERANCE) {
      return { shares, status: { kind: 'percentWrong', sum }, valid: false };
    }
    // Rounding leftovers (a cent or two) go to the last member, so the parts add up exactly.
    const last = included[included.length - 1];
    shares.set(last, (shares.get(last) ?? 0) + totalCents - assigned);
    return { shares, status: { kind: 'percentOk' }, valid: true };
  }

  // Exact amounts.
  const amounts = included.map((id) => parseNumber(exactText[id] ?? ''));
  let sumCents = 0;
  included.forEach((id, index) => {
    const cents = amounts[index] === null ? 0 : toCents(amounts[index] as number);
    shares.set(id, cents);
    sumCents += cents;
  });
  if (amounts.some((value) => value === null || value <= 0)) {
    return { shares, status: { kind: 'exactInvalid' }, valid: false };
  }
  const diff = sumCents - totalCents;
  if (Math.abs(diff) <= CENT_TOLERANCE) {
    return { shares, status: { kind: 'exactOk' }, valid: true };
  }
  return {
    shares,
    status: diff < 0 ? { kind: 'exactMissing', sumCents, diffCents: -diff } : { kind: 'exactOver', sumCents, diffCents: diff },
    valid: false,
  };
}

/** Text for an input, written the way the user types in their language ("12,50" / "12.50"). */
export function decimalText(value: number, language: string): string {
  const text = Number.isInteger(value) ? String(value) : value.toFixed(2).replace(/0$/, '');
  return language.startsWith('sr') ? text.replace('.', ',') : text;
}

/** Money for an input: always two decimals ("54,20" / "54.20"). */
export function moneyText(value: number, language: string): string {
  const text = value.toFixed(2);
  return language.startsWith('sr') ? text.replace('.', ',') : text;
}

/** Equal percentages that add up to exactly 100 (33,34 + 33,33 + 33,33). */
export function equalPercents(count: number): number[] {
  return equalCents(10000, count).map((hundredths) => hundredths / 100);
}
