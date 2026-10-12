/**
 * Client-side checks that mirror the backend's FluentValidation rules (PasswordRules,
 * RegisterRequestValidator), so most mistakes are shown before the form is even sent.
 * The backend still checks everything - these only save a round trip.
 *
 * Each function returns a translation key, or null when the value is fine.
 */
import type { ParseKeys } from 'i18next';

export type ValidationKey = ParseKeys<'translation'>;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const PASSWORD_MIN = 8;
export const PASSWORD_MAX = 72;
export const NAME_MAX = 100;

export function required(value: string): ValidationKey | null {
  return value.trim() ? null : 'validation.required';
}

export function email(value: string): ValidationKey | null {
  if (!value.trim()) {
    return 'validation.required';
  }
  return EMAIL_PATTERN.test(value.trim()) ? null : 'validation.email';
}

export function newPassword(value: string): ValidationKey | null {
  if (!value) {
    return 'validation.required';
  }
  if (value.length < PASSWORD_MIN || value.length > PASSWORD_MAX) {
    return 'validation.passwordLength';
  }
  if (!/\p{L}/u.test(value)) {
    return 'validation.passwordLetter';
  }
  if (!/\d/.test(value)) {
    return 'validation.passwordDigit';
  }
  return null;
}

export function matches(value: string, original: string): ValidationKey | null {
  if (!value) {
    return 'validation.required';
  }
  return value === original ? null : 'validation.passwordMatch';
}

export function name(value: string): ValidationKey | null {
  if (!value.trim()) {
    return 'validation.required';
  }
  return value.trim().length > NAME_MAX ? 'validation.nameTooLong' : null;
}

/** Removes the fields without an error, so `Object.keys(errors).length` tells if the form is valid. */
export function onlyErrors<T extends string>(
  errors: Record<T, ValidationKey | null>,
): Partial<Record<T, ValidationKey>> {
  const result: Partial<Record<T, ValidationKey>> = {};
  for (const [field, key] of Object.entries(errors) as [T, ValidationKey | null][]) {
    if (key) {
      result[field] = key;
    }
  }
  return result;
}
