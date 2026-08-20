import { defineConfig } from 'vite';
import { createAppConfig } from '../../vite.shared.mjs';

export default defineConfig(() =>
  createAppConfig({
    name: '@absence-management/admin',
    root: import.meta.dirname,
    defaultPort: 4201,
  }),
);
