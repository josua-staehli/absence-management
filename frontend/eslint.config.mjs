import nx from '@nx/eslint-plugin';
import tsParser from '@typescript-eslint/parser';

export default [
  // Registers the @nx plugin. It brings no rules of its own, which is the point:
  // code style belongs to oxlint, this file is only about the dependency graph.
  ...nx.configs['flat/base'],
  {
    ignores: [
      '**/dist',
      '**/out-tsc',
      '**/test-output',
      '**/storybook-static',
      // Nx keeps a copy of every cached task output here, generated client included.
      '**/.nx/**',
      // The trailing /** is what makes ESLint skip the whole folder: without it the generated
      // files are linted and their eslint-disable comments name rules this configuration does
      // not load, which is an error of its own.
      'packages/shared/api-client/src/generated/**',
      '**/vite.config.*.timestamp*',
    ],
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.mjs'],
    // flat/base sets no parser, and the default one cannot read TypeScript or JSX.
    languageOptions: { parser: tsParser, ecmaVersion: 2024, sourceType: 'module' },
    rules: {
      /*
       * The frontend counterpart of the backend's project references: checked by
       * `pnpm boundaries`, so an accidental import across a layer or area boundary fails
       * the build instead of quietly creating a tangle.
       *
       *   scope:*  - which feature area a library belongs to (absences, employees, shared)
       *   type:*   - which layer it is (app, feature, data-access, ui, util, e2e)
       */
      '@nx/enforce-module-boundaries': [
        'error',
        {
          // Every library here is source based - its package.json entry points at src/index.ts
          // and the applications bundle it with Vite, so no library has a build target. With this
          // flag on, the first import from an application would fail as a 'buildable library
          // importing a non-buildable one'.
          enforceBuildableLibDependency: false,
          // Two files are imported by path rather than by package name: the ESLint
          // configuration itself, and the Vite setup the two applications share.
          allow: ['^.*/eslint\\.config\\.[cm]?[jt]s$', '^.*/vite\\.shared\\.mjs$'],
          depConstraints: [
            // --- feature areas ------------------------------------------------
            {
              sourceTag: 'scope:app',
              onlyDependOnLibsWithTags: ['scope:absences', 'scope:employees', 'scope:shared'],
            },
            {
              sourceTag: 'scope:absences',
              onlyDependOnLibsWithTags: ['scope:absences', 'scope:employees', 'scope:shared'],
            },
            {
              sourceTag: 'scope:employees',
              onlyDependOnLibsWithTags: ['scope:employees', 'scope:shared'],
            },
            { sourceTag: 'scope:shared', onlyDependOnLibsWithTags: ['scope:shared'] },
            // --- layers -------------------------------------------------------
            {
              sourceTag: 'type:app',
              onlyDependOnLibsWithTags: ['type:feature', 'type:ui', 'type:util'],
            },
            {
              sourceTag: 'type:feature',
              onlyDependOnLibsWithTags: ['type:data-access', 'type:ui', 'type:util'],
            },
            { sourceTag: 'type:data-access', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:ui', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:util', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:e2e', onlyDependOnLibsWithTags: ['type:util'] },
          ],
        },
      ],
    },
  },
];
