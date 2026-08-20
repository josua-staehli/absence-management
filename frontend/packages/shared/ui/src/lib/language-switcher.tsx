import { supportedLanguages, useTranslation } from '@absence-management/shared-i18n';
import { SegmentedControl } from '@mantine/core';

/**
 * Switches the language of the whole frontend. It sits in the header of AppLayout, so both
 * applications have it and no page has to think about it: changing the language re-renders every
 * component that called `useTranslation`.
 */
export function LanguageSwitcher() {
  const { i18n, t } = useTranslation();

  return (
    <SegmentedControl
      size="xs"
      aria-label={t('app.language')}
      value={i18n.resolvedLanguage ?? i18n.language}
      onChange={(language) => void i18n.changeLanguage(language)}
      data={supportedLanguages.map((language) => ({
        value: language,
        label: language.toUpperCase(),
      }))}
    />
  );
}
