import '@mantine/core/styles.css';

import { MantineProvider } from '@mantine/core';
import type { Preview } from '@storybook/react-vite';

/**
 * Stories render a single component, not an app, so AppLayout is not in the tree - but Mantine
 * components still need their provider and their stylesheet.
 */
const preview: Preview = {
  decorators: [
    (Story) => (
      <MantineProvider>
        <Story />
      </MantineProvider>
    ),
  ],
};

export default preview;
