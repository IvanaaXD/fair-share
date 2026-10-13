/**
 * Client-side checks for the expense form. They mirror the backend rules
 * (ValidationRules.ValidMoneyAmount / ValidExpenseDate, CreateExpenseRequestValidator and
 * ImageFileRules), so mistakes are shown before anything is sent. The backend checks again.
 */
import type { ValidationKey } from '../../auth/validation';
import { todayDateOnly } from '../../i18n/format';

export const MAX_AMOUNT = 1_000_000_000;
export const MAX_DESCRIPTION = 500;
export const MIN_DATE = '2000-01-01';
export const RECEIPT_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
export const RECEIPT_MAX_BYTES = 5 * 1024 * 1024;

/** "1.234,50", "1234.5", "12" -> "1234.50", "1234.5", "12"; null when it is not a number at all. */
function normalizeAmount(text: string): string | null {
  let value = text.trim().replace(/\s/g, '');
  if (!value) {
    return null;
  }
  // With both separators the last one is the decimal separator ("1.234,50" or "1,234.50").
  const lastComma = value.lastIndexOf(',');
  const lastDot = value.lastIndexOf('.');
  if (lastComma >= 0 && lastDot >= 0) {
    const decimal = lastComma > lastDot ? ',' : '.';
    const thousands = decimal === ',' ? '.' : ',';
    value = value.split(thousands).join('').replace(decimal, '.');
  } else {
    value = value.replace(',', '.');
  }
  return /^\d+(\.\d+)?$/.test(value) ? value : null;
}

export function parseAmount(text: string): number | null {
  const normalized = normalizeAmount(text);
  return normalized === null ? null : Number(normalized);
}

export function validateAmount(text: string): ValidationKey | null {
  if (!text.trim()) {
    return 'validation.amountRequired';
  }
  const normalized = normalizeAmount(text);
  if (normalized === null) {
    return 'validation.amountFormat';
  }
  const amount = Number(normalized);
  if (amount <= 0) {
    return 'validation.amountPositive';
  }
  if (amount > MAX_AMOUNT) {
    return 'validation.amountTooLarge';
  }
  // Checked on the text: 0.29 * 100 is 28.999... in floating point.
  if (/\.\d{3,}$/.test(normalized)) {
    return 'validation.amountDecimals';
  }
  return null;
}

export function validateDate(value: string, today = todayDateOnly()): ValidationKey | null {
  if (!value) {
    return 'validation.required';
  }
  if (value < MIN_DATE) {
    return 'validation.dateTooOld';
  }
  if (value > today) {
    return 'validation.dateFuture';
  }
  return null;
}

export function validateDescription(value: string): ValidationKey | null {
  return value.trim().length > MAX_DESCRIPTION ? 'validation.descriptionTooLong' : null;
}

export function validateReceipt(file: File): ValidationKey | null {
  if (!RECEIPT_TYPES.includes(file.type)) {
    return 'validation.imageType';
  }
  return file.size > RECEIPT_MAX_BYTES ? 'validation.imageSize' : null;
}
