# @absence-management/shared-i18n

The translations of the whole frontend and the configured i18next instance.

## Adding a text

1. Add the key to `src/lib/en.ts` — English is the reference language and defines the key type.
2. Add the same key to `src/lib/de.ts`; the `satisfies` there fails the typecheck if one is missing.
3. Use it with `const { t } = useTranslation()` and `t('section.key')`.
