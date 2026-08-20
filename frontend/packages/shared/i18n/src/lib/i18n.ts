import i18next, { init, use } from 'i18next';
import { initReactI18next } from 'react-i18next';

import { de } from './de';
import { en } from './en';

/**
 * The one i18next instance of the frontend, initialised when this module is first imported -
 * which AppLayout does before it renders anything.
 *
 * The two resources are ordinary TypeScript objects rather than JSON files loaded at run time:
 * they are small, they are bundled with the app, and being TypeScript is what makes the module
 * augmentation below possible.
 */

export const supportedLanguages = ['en', 'de'] as const;

export type SupportedLanguage = (typeof supportedLanguages)[number];

/** English is the default of the frontend, and the reference `de.ts` is checked against. */
export const defaultLanguage: SupportedLanguage = 'en';

const resources = {
  en: { translation: en },
  de: { translation: de },
};

/**
 * The type safety of every `t('…')` call in the frontend: i18next infers the allowed keys from
 * these two declarations, so a typo or a key that only exists in one language is a compile error
 * instead of the key itself showing up in the UI.
 */
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof en };
  }
}

// The named exports configure the default instance - the same one `useTranslation` falls back to
// when a component renders outside the provider, e.g. the title in an app's app.tsx. Not
// awaited: the resources are bundled, so the instance is ready as soon as this returns.
use(initReactI18next);
void init({
  resources,
  lng: defaultLanguage,
  fallbackLng: defaultLanguage,
  // React escapes what it renders, so i18next must not escape a second time.
  interpolation: { escapeValue: false },
});

export { i18next };
