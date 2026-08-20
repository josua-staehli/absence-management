import '@mantine/core/styles.css';
import '@mantine/dates/styles.css';

import { i18next, I18nextProvider } from '@absence-management/shared-i18n';
import {
  AppShell,
  Container,
  createTheme,
  Group,
  MantineProvider,
  type MantineColor,
  Stack,
  Title,
} from '@mantine/core';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { type ReactNode, useMemo } from 'react';

import { LanguageSwitcher } from './language-switcher';

/**
 * This exists because there are two apps. Duplicating the provider setup per app is how the two
 * start to drift - different retry policies, different themes, one of them missing the Mantine
 * stylesheet. An app's app.tsx is down to "which layout, which pages".
 *
 * The two stylesheet imports belong here for the same reason: Mantine ships plain CSS, and it
 * has to be imported exactly once, before any component that uses it. The I18nextProvider is
 * here because importing the instance is what initialises it - once, before the first t() call.
 */

const queryClient = new QueryClient({
  defaultOptions: {
    // Business errors are shown to the user instead of being retried.
    queries: { retry: false, refetchOnWindowFocus: false },
    mutations: { retry: false },
  },
});

export interface AppLayoutProps {
  /** Shown in the header. Also the fastest way to see which app you are looking at. */
  title: string;
  /** Primary colour, deliberately different per app for the same reason. */
  accentColor: MantineColor;
  /** The pages of the app, stacked in the order they are written. */
  children: ReactNode;
}

export function AppLayout({ title, accentColor, children }: AppLayoutProps) {
  const theme = useMemo(() => createTheme({ primaryColor: accentColor }), [accentColor]);

  return (
    <I18nextProvider i18n={i18next}>
      <QueryClientProvider client={queryClient}>
        <MantineProvider theme={theme}>
          <AppShell header={{ height: 56 }} padding="md">
            <AppShell.Header>
              <Group h="100%" px="md" justify="space-between">
                <Title order={4}>{title}</Title>
                <LanguageSwitcher />
              </Group>
            </AppShell.Header>

            <AppShell.Main>
              <Container size="lg">
                <Stack gap="xl">{children}</Stack>
              </Container>
            </AppShell.Main>
          </AppShell>
        </MantineProvider>
      </QueryClientProvider>
    </I18nextProvider>
  );
}
