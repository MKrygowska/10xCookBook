import { test, expect } from '@playwright/test';

test('created private recipe persists after page reload', async ({ page }) => {
  const recipeTitle = `E2E Private Recipe ${Date.now()}`;
  const instructions = 'Mix ingredients, bake for 30 minutes at 180C, serve hot.';

  // 1. Navigate to the private recipes page
  await page.goto('/my-recipes');

  // 2. Open the recipe creation modal
  await page.getByRole('button', { name: 'Dodaj nowy przepis' }).click();

  // 3. Fill in the recipe details
  await page.getByLabel('Tytuł przepisu').fill(recipeTitle);
  await page.getByLabel('Instrukcja przygotowania').fill(instructions);

  // 4. Add an ingredient via autocomplete
  const ingredientInput = page.getByPlaceholder('Wyszukaj i dodaj składnik (np. jajka, masło)...');
  await ingredientInput.fill('mąka'); // Using 'mąka' (flour) as a typical Polish ingredient
  
  // Wait for and click the dropdown option
  const dropdownItem = page.locator('.dropdown-item').filter({ hasText: 'mąka' });
  await dropdownItem.click();

  // 5. Fill quantity for the added ingredient
  const qtyInput = page.getByPlaceholder('Ilość (np. 3 szt., 100g)');
  await qtyInput.fill('500g');

  // 6. Submit the form
  await page.getByRole('button', { name: 'Zapisz przepis' }).click();

  // 7. Verify the recipe card is visible on the page
  const recipeCard = page.locator('article.recipe-card').filter({ hasText: recipeTitle });
  await expect(recipeCard).toBeVisible();
  await expect(recipeCard.locator('.instructions-text')).toHaveText(instructions);

  // 8. Reload page and verify persistence
  await page.reload();
  await expect(page.locator('article.recipe-card').filter({ hasText: recipeTitle })).toBeVisible();

  // 9. Cleanup - Delete the recipe
  // Locate the delete button on our specific recipe card using its title/tooltip 'Usuń'
  const deleteBtn = page.locator('article.recipe-card').filter({ hasText: recipeTitle }).locator('.btn-delete');
  await deleteBtn.click();

  // Click confirm button in the modal
  await page.getByRole('button', { name: 'Usuń przepis' }).click();

  // 10. Verify it is removed
  await expect(page.locator('article.recipe-card').filter({ hasText: recipeTitle })).not.toBeVisible();
});
