import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ page }) => {
  const email = 'e2e_user@example.com';
  const password = 'password123';

  // 1. Attempt to register the test user
  await page.goto('/register');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Hasło').fill(password);
  await page.getByRole('button', { name: 'Zarejestruj się' }).click();

  try {
    // If registration is successful, we should end up on the dashboard
    await expect(page).toHaveURL(/.*dashboard/);
  } catch (error) {
    // If registration failed (e.g. email already registered), go to login page
    await page.goto('/login');
    await page.getByLabel('E-mail').fill(email);
    await page.getByLabel('Hasło').fill(password);
    await page.getByRole('button', { name: 'Zaloguj się' }).click();
    await expect(page).toHaveURL(/.*dashboard/);
  }

  // Save the authenticated storage state (including localStorage token/email)
  await page.context().storageState({ path: authFile });
});
