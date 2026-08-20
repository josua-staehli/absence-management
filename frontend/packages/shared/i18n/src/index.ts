export * from './lib/de';
export * from './lib/en';
export * from './lib/i18n';

// The hook every component uses. Re-exported here so that a library depends on this one and not
// on react-i18next directly - and so the module augmentation in lib/i18n.ts is always loaded.
export { I18nextProvider, Trans, useTranslation } from 'react-i18next';
