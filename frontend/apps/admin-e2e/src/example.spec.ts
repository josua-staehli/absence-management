import { expect, test } from '@playwright/test';

// A smoke test until the applications render something: the shell has to be served and the
// React root has to be mounted.
test('serves the application shell', async ({ page }) => {
  await page.goto('/');

  await expect(page.locator('#root')).toBeAttached();
});
