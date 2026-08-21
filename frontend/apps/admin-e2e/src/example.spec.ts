import { expect, test } from '@playwright/test';

test('shows absence decisions and employees', async ({ page }) => {
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

  await page.route('**/api/employees', async (route) => {
    await route.fulfill({
      json: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          firstName: 'Anna',
          lastName: 'Meier',
          email: 'anna.meier@example.com',
        },
      ],
    });
  });

  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Absence Management – Administration', exact: true }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: 'New request' })).toHaveCount(0);

  const request = page.getByRole('row').filter({ hasText: 'Autumn holiday' });
  await expect(request.getByRole('button', { name: 'Approve' })).toBeVisible();
  await expect(request.getByRole('button', { name: 'Reject' })).toBeVisible();
  await expect(request.getByRole('button', { name: 'Edit' })).toHaveCount(0);

  const employee = page.getByRole('row').filter({ hasText: 'anna.meier@example.com' });
  await expect(employee).toContainText('Anna');
  await expect(employee).toContainText('Meier');
});
