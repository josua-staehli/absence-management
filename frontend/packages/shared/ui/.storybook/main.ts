import { resolve } from 'node:path';
import type { StorybookConfig } from '@storybook/react-vite';

const workspaceRoot = resolve(import.meta.dirname, '../../../..');

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.tsx'],
  framework: { name: '@storybook/react-vite', options: {} },
  // Same stance as `analytics: false` in nx.json.
  core: { disableTelemetry: true },
  // Storybook's Vite root is this library, so PostCSS would be looked up here. Mantine's preset
  // is workspace-wide - the same explicit path the two applications use in vite.shared.mts.
  viteFinal: (viteConfig) => ({
    ...viteConfig,
    css: { ...viteConfig.css, postcss: workspaceRoot },
  }),
};

export default config;
