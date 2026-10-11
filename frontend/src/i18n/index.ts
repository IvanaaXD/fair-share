import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { en } from './locales/en';
import { srCyrl } from './locales/srCyrl';
import { transliterateResources } from './transliterate';

export const LANGUAGES = [
  { code: 'sr-Cyrl', short: 'Ћир', label: 'Српски, ћирилица' },
  { code: 'sr-Latn', short: 'Lat', label: 'Srpski, latinica' },
  { code: 'en', short: 'EN', label: 'English' },
] as const;

export type LanguageCode = (typeof LANGUAGES)[number]['code'];

const STORAGE_KEY = 'fairshare.language';
const DEFAULT_LANGUAGE: LanguageCode = 'sr-Cyrl';

function isLanguageCode(value: string | null): value is LanguageCode {
  return LANGUAGES.some((language) => language.code === value);
}

/** The language saved on this device, otherwise Serbian Cyrillic. */
function getInitialLanguage(): LanguageCode {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    return isLanguageCode(saved) ? saved : DEFAULT_LANGUAGE;
  } catch {
    // Storage can be blocked (private mode, strict browser settings) - just use the default.
    return DEFAULT_LANGUAGE;
  }
}

function applyLanguage(language: string) {
  // Screen readers and the browser's hyphenation/spell-check use the lang attribute.
  document.documentElement.lang = language;
  try {
    localStorage.setItem(STORAGE_KEY, language);
  } catch {
    // Not saved - the choice still applies until the page is closed.
  }
}

void i18n.use(initReactI18next).init({
  resources: {
    'sr-Cyrl': { translation: srCyrl },
    'sr-Latn': { translation: transliterateResources(srCyrl) },
    en: { translation: en },
  },
  lng: getInitialLanguage(),
  fallbackLng: DEFAULT_LANGUAGE,
  // The resources are bundled with the app, so there is nothing to wait for.
  initAsync: false,
  interpolation: {
    // React already escapes values, so i18next must not escape them a second time.
    escapeValue: false,
  },
});

applyLanguage(i18n.language);
i18n.on('languageChanged', applyLanguage);

export default i18n;
