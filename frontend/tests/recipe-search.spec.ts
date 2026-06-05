import { test, expect } from '@playwright/test';

test('user can search recipes by ingredients and see match results and styling', async ({ page }) => {
  // 1. Navigate to the dashboard
  await page.goto('/dashboard');

  // 2. Locate the ingredient search input
  const searchInput = page.getByPlaceholder('Wpisz składnik (np. pomidor, sól)...');
  await searchInput.fill('pomidor');

  // 3. Wait for the autocomplete dropdown and select 'pomidor'
  const dropdownItem = page.locator('.dropdown-item').filter({ hasText: 'pomidor' });
  await dropdownItem.click();

  // 4. Verify that the tag chip was added to the UI
  await expect(page.locator('.tag-chip').filter({ hasText: 'pomidor' })).toBeVisible();

  // 5. Verify that the results section header appears showing matches
  const resultsHeader = page.locator('.section-title');
  await expect(resultsHeader).toContainText('Dopasowane przepisy');

  // 6. Verify that matching recipe cards are displayed
  const recipeCard = page.locator('article.recipe-card').first();
  await expect(recipeCard).toBeVisible();

  // 7. Verify the matched ingredient pill (styled green) is present on the card
  const matchedPill = recipeCard.locator('.pill-matched').filter({ hasText: 'pomidor' });
  await expect(matchedPill).toBeVisible();

  // 8. Test instructions toggle (accordion)
  const toggleBtn = recipeCard.getByRole('button', { name: 'Pokaż instrukcję przygotowania' });
  await expect(toggleBtn).toBeVisible();
  await toggleBtn.click();

  // Verify that the instruction details became visible
  await expect(recipeCard.locator('.instructions-text')).toBeVisible();

  // Hide the instructions
  await recipeCard.getByRole('button', { name: 'Ukryj instrukcję przygotowania' }).click();
  await expect(recipeCard.locator('.instructions-text')).not.toBeVisible();
});
