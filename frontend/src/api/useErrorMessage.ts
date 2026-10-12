import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocalizedText } from '../i18n/useLocalizedText';
import { ApiError } from './ApiError';

/**
 * Turns any error from an API call into one sentence for the user.
 *
 * Messages that the app knows by status code (no connection, too many attempts, or the ones a
 * page passes in `byStatus`) are translated in the app. Otherwise the backend's own message is
 * shown - the backend writes it in Serbian Cyrillic, so in Latin and English it is
 * transliterated. Real English texts for those need error codes from the backend (a later step).
 */
export function useErrorMessage() {
  const { t } = useTranslation();
  const localize = useLocalizedText();

  return useCallback(
    (error: unknown, byStatus: Partial<Record<number, string>> = {}): string => {
      if (!(error instanceof ApiError)) {
        return t('errors.generic');
      }
      if (error.isNetworkError) {
        return t('errors.network');
      }
      if (error.status === 429) {
        return t('errors.tooManyRequests');
      }
      const known = byStatus[error.status];
      if (known) {
        return known;
      }
      if (error.detail) {
        return localize(error.detail);
      }
      return t('errors.generic');
    },
    [t, localize],
  );
}
