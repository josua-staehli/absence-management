import { defineConfig } from '@hey-api/openapi-ts';

/**
 * Generates the typed API client from the OpenAPI document.
 *
 * The input file is written by `dotnet build` of AbsenceManagement.Api (see
 * OpenApiDocumentsDirectory in its csproj). Run with `pnpm gen:api`. The output is checked in,
 * so a fresh clone builds without codegen.
 */
export default defineConfig({
  input: './openapi/AbsenceManagement.Api.json',
  output: {
    path: './packages/shared/api-client/src/generated',
    // Everything here is bundled by Vite, so extensionless relative imports are the least
    // surprising. The folder is excluded from oxlint and oxfmt - it is regenerated, not
    // maintained.
    importFileExtension: '',
  },
  plugins: [
    {
      name: '@hey-api/client-fetch',
      // The frontend always calls /api on its own origin; the Vite dev server proxies it.
      baseUrl: '',
      // Reject instead of returning an error object, which is what TanStack Query wants.
      throwOnError: true,
    },
    {
      // Enums become const objects, so `AbsenceType.VACATION` exists at run time and the list of
      // types in the UI is derived from the contract instead of retyped.
      name: '@hey-api/typescript',
      enums: 'javascript',
    },
    '@hey-api/sdk',
  ],
});
