import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocalizedText } from '../i18n/useLocalizedText';
import { ApiError } from './ApiError';

/** General English sentences for errors without a code (the backend's text is Serbian). */
const STATUS_MESSAGES: Partial<Record<number, 'errors.badRequest' | 'errors.forbidden' | 'errors.notFound' | 'errors.conflict'>> = {
  400: 'errors.badRequest',
  403: 'errors.forbidden',
  404: 'errors.notFound',
  409: 'errors.conflict',
};

/**
 * Turns any error from an API call into one sentence for the user.
 *
 * Messages that the app knows by status code (no connection, too many attempts, or the ones a
 * page passes in `byStatus`) are translated in the app. Errors with a code (ErrorCodes on the
 * backend) are translated from "apiErrors". Without a code the backend's own Serbian message is
 * shown (transliterated in Latin); in English a general sentence for the status code is shown.
 */
export function useErrorMessage() {
  const { t, i18n } = useTranslation();
  const localize = useLocalizedText();

  return useCallback(
    (error: unknown, byStatus: Partial<Record<number, string>> = {}): string => {
      if (!(error instanceof ApiError)) {
        return t('errors.generic');
      }
      // 502-504 come from the proxy (Vite or Nginx) when the backend is not running.
      if (error.isNetworkError || (error.status >= 502 && error.status <= 504)) {
        return t('errors.network');
      }
      if (error.status === 429) {
        return t('errors.tooManyRequests');
      }
      const known = byStatus[error.status];
      if (known) {
        return known;
      }
      // Errors with a code are translated in the app, in every language.
      const codeKey = `apiErrors.${error.code}`;
      if (error.code && i18n.exists(codeKey)) {
        return t(codeKey as 'apiErrors.serverError');
      }
      // A message without a code is written in Serbian by the backend: shown in both scripts,
      // but in English a general sentence by status code reads better than transliterated Serbian.
      if (i18n.language === 'en') {
        return t(STATUS_MESSAGES[error.status] ?? 'errors.generic');
      }
      if (error.detail) {
        return localize(error.detail);
      }
      return t('errors.generic');
    },
    [t, i18n, localize],
  );
}
