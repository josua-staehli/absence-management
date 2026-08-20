import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import type { UserConfig } from 'vitest/config';

interface AppConfigOptions {
  /** The Nx project name, used for the Vitest project name and the Vite cache directory. */
  name: string;
  /** `import.meta.dirname` of the app's own vite.config.mts. */
  root: string;
  /** Port used when the app is started outside Aspire (`nx dev <app>`). */
  defaultPort: number;
}

/**
 * Two values are injected by the Aspire AppHost when the system is started with `aspire run`:
 *   PORT     - the port Aspire allocated for this resource
 *   API_URL  - the address of the "api" resource
 * The fallbacks let each app also run standalone against the API's launch profile.
 *
 * This lives in one file so the two apps cannot end up with different proxy rules - the kind of
 * difference that only shows up as "works in web, 404 in admin".
 */
export function createAppConfig({ name, root, defaultPort }: AppConfigOptions): UserConfig {
  const workspaceRoot = resolve(root, '../..');
  const port = Number(process.env.PORT) || defaultPort;
  const apiUrl = process.env.API_URL ?? 'http://localhost:5180';
  const shortName = name.split('/').pop();

  return {
    root,
    cacheDir: `${workspaceRoot}/node_modules/.vite/apps/${shortName}`,
    // The Vite root is the app folder, so PostCSS would look for its config there. Mantine's
    // preset is workspace-wide, hence the explicit path to postcss.config.cjs at the root.
    css: { postcss: workspaceRoot },
    server: {
      port,
      host: 'localhost',
      proxy: {
        // The apps always call /api on their own origin, which avoids any CORS setup.
        '/api': { target: apiUrl, changeOrigin: true },
      },
    },
    preview: { port, host: 'localhost' },
    plugins: [react()],
    build: { outDir: './dist', emptyOutDir: true },
    test: {
      name,
      watch: false,
      globals: true,
      environment: 'jsdom',
      // Stubs the browser APIs jsdom is missing and Mantine expects.
      setupFiles: [`${workspaceRoot}/vitest.setup.ts`],
      include: ['{src,tests}/**/*.{test,spec}.{ts,mts,tsx}'],
      coverage: { reportsDirectory: './test-output/vitest/coverage', provider: 'v8' as const },
    },
  };
}
