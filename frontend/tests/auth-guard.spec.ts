import { test, expect } from '@playwright/test';

// Override global authenticated state to run this test completely unauthenticated
test.use({ storageState: { cookies: [], origins: [] } });

test('unauthenticated user is redirected from private routes to login page', async ({ page }) => {
  // 1. Attempt to access the dashboard directly
  await page.goto('/dashboard');
  await expect(page).toHaveURL(/.*login/);

  // 2. Attempt to access my recipes directly
  await page.goto('/my-recipes');
  await expect(page).toHaveURL(/.*login/);

  // 3. Attempt to access settings directly
  await page.goto('/settings');
  await expect(page).toHaveURL(/.*login/);
});
