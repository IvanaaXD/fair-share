import 'i18next';
import type { Translation } from './locales/srCyrl';

// Makes t('nav.overview') type-checked: a key that does not exist is a compile error,
// and the editor suggests the existing keys while typing.
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: {
      translation: Translation;
    };
  }
}
