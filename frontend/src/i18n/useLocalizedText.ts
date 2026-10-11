import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { toLatin } from './transliterate';

/**
 * For texts that are not part of the translations: names of groups, descriptions of
 * expenses, messages from the API. Cyrillic text is shown in Latin in every language except
 * Serbian Cyrillic: in Latin mode to match the chosen script, and in English because a
 * foreign user cannot read Cyrillic. The text itself is never translated.
 */
export function useLocalizedText() {
  const { i18n } = useTranslation();
  const shouldTransliterate = i18n.language !== 'sr-Cyrl';

  return useCallback((text: string) => (shouldTransliterate ? toLatin(text) : text), [shouldTransliterate]);
}