import type { ValidationKey } from '../../auth/validation';

/** Only digits count; dashes and spaces are allowed while typing (as on a payment slip). */
export function validateBankAccount(value: string): ValidationKey | null {
  if (!value.trim()) return null;
  if (!/^[\d\s-]+$/.test(value)) return 'validation.bankChars';
  const digits = value.replace(/\D/g, '').length;
  return digits === 16 || digits === 18 ? null : 'validation.bankLength';
}

/** "161000001234567890" -> "161-0000012345678-90" (the usual 3-13-2 layout); others as they are. */
export function formatBankAccount(digits: string | null): string {
  if (!digits) return '';
  return /^\d{18}$/.test(digits) ? `${digits.slice(0, 3)}-${digits.slice(3, 16)}-${digits.slice(16)}` : digits;
}
