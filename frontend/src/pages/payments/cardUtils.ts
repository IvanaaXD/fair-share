/**
 * Card checks done in the browser before anything is sent - the same rules as the backend
 * (CardNumber.IsValid, AddPaymentCardRequestValidator). They only save a round trip.
 */
import type { ValidationKey } from '../../auth/validation';

export function digitsOnly(value: string): string {
  return value.replace(/\D/g, '');
}

/** Luhn check digit (ISO/IEC 7812) - catches almost every mistyped card number. */
export function isLuhnValid(value: string): boolean {
  const digits = digitsOnly(value);
  if (digits.length < 12 || digits.length > 19) return false;
  let sum = 0;
  let double = false;
  for (let index = digits.length - 1; index >= 0; index--) {
    let digit = Number(digits[index]);
    if (double) {
      digit *= 2;
      if (digit > 9) digit -= 9;
    }
    sum += digit;
    double = !double;
  }
  return sum % 10 === 0;
}

/** "4242424242424242" -> "4242 4242 4242 4242" while typing. */
export function formatCardNumber(value: string): string {
  return digitsOnly(value).slice(0, 19).replace(/(\d{4})(?=\d)/g, '$1 ');
}

/** "0828" / "08/28" -> "08/28" while typing. */
export function formatExpiry(value: string): string {
  const digits = digitsOnly(value).slice(0, 4);
  return digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
}

export function parseExpiry(value: string): { month: number; year: number } | null {
  const match = /^(\d{2})\/(\d{2})$/.exec(value.trim());
  if (!match) return null;
  const month = Number(match[1]);
  if (month < 1 || month > 12) return null;
  return { month, year: 2000 + Number(match[2]) };
}

export function validateCardNumber(value: string): ValidationKey | null {
  if (!digitsOnly(value)) return 'validation.required';
  return isLuhnValid(value) ? null : 'card.invalidNumber';
}

export function validateExpiry(value: string, now = new Date()): ValidationKey | null {
  if (!value.trim()) return 'validation.required';
  const parsed = parseExpiry(value);
  if (!parsed) return 'card.invalidExpiry';
  // A card is valid until the end of its expiry month.
  const expired = parsed.year < now.getFullYear() || (parsed.year === now.getFullYear() && parsed.month < now.getMonth() + 1);
  return expired ? 'card.expired' : null;
}

export function validateCvv(value: string): ValidationKey | null {
  return /^\d{3,4}$/.test(value) ? null : 'card.invalidCvv';
}

/** A new key for every payment attempt (see paymentsApi.payWithCard). */
export function newIdempotencyKey(): string {
  return typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}
