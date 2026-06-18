import { expect, test } from '@playwright/test';

test.describe('TL5 Auth Risk', () => {
  test('E2E-02: unauthenticated user is redirected before creating article', async ({ page }) => {
    await page.goto('/editor');

    await expect(page).toHaveURL(/\/login/);

    await expect(
      page.getByRole('heading', { name: /sign in/i })
    ).toBeVisible();
  });

  test('E2E-03: unauthenticated user is redirected before opening settings', async ({ page }) => {
    await page.goto('/settings');

    await expect(page).toHaveURL(/\/login/);

    await expect(
      page.getByRole('heading', { name: /sign in/i })
    ).toBeVisible();
  });
});
