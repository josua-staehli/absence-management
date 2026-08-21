import { expect, test } from '@playwright/test';

test('shows absence requests with employee actions', async ({ page }) => {
  await page.route('**/api/absence-requests', async (route) => {
    await route.fulfill({
      json: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          employeeId: '22222222-2222-2222-2222-222222222222',
          employeeName: 'Anna Meier',
          type: 'Vacation',
          startDate: '2026-09-07',
          endDate: '2026-09-11',
          status: 'Open',
          comment: 'Autumn holiday',
          createdAt: '2026-08-21T08:00:00Z',
          updatedAt: null,
        },
      ],
    });
  });

  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Absence Management', exact: true }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: 'New request' })).toBeVisible();

  const request = page.getByRole('row').filter({ hasText: 'Anna Meier' });
  await expect(request).toContainText('Vacation');
  await expect(request).toContainText('Open');
  await expect(request.getByRole('button', { name: 'Edit' })).toBeVisible();
  await expect(request.getByRole('button', { name: 'Approve' })).toHaveCount(0);
  await expect(request.getByRole('button', { name: 'Reject' })).toHaveCount(0);
});
