---
date: 2026-06-05T10:15:00Z
researcher: Malgorzata Krygowska
git_commit: 195363c84407def65b32c9840e94ef4b883e5919
branch: main
repository: 10xCookBook
topic: "GDPR and Content Coverage Tests (Phase 3)"
tags: [research, codebase, testing, gdpr, ingredients]
status: complete
last_updated: 2026-06-05
last_updated_by: Malgorzata Krygowska
---

# Research: GDPR and Content Coverage Tests

**Date**: 2026-06-05T10:15:00Z
**Researcher**: Malgorzata Krygowska
**Git Commit**: 195363c84407def65b32c9840e94ef4b883e5919
**Branch**: main
**Repository**: 10xCookBook

## Research Question

Conduct research for Phase 3 of the stateful test rollout (GDPR & Content coverage):
- **Risk #5**: Prove that after `DELETE /api/users/me`, the deleted user's private recipes return zero rows. Challenge the assumption that "cascade delete works in EF just because it is configured"; avoid asserting only that DeleteUser returns true.
- **Risk #6**: Prove that the ingredient catalog covers common Polish cooking ingredients beyond the 20 seeded. Challenge the assumption that 20 seeds is enough for MVP.

---

## Summary

1. **Risk #5 (GDPR Cascade Delete)**:
   - EF Core is configured with `.OnDelete(DeleteBehavior.Cascade)` on the `Recipe.UserId` relationship in [AppDbContext.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L38-L43), and the production database (SQL Server) natively cascades user deletions.
   - However, in xUnit unit tests using `Microsoft.EntityFrameworkCore.InMemory`, cascade delete only triggers for loaded/tracked entities. Because [UserService.DeleteUser](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/UserService.cs#L121-L143) uses `Find(userId)` without loading related recipes, the user is deleted but the private recipes **survive** in the InMemory database.
   - **Fix**: Update the user lookup in `UserService.DeleteUser` to eager-load the private recipes: `.Include(u => u.Recipes)`. This makes the EF Change Tracker aware of the dependent recipes and deletes them, which ensures correct cascade delete behavior in both InMemory tests and production databases.
   - **Test**: Add a test `DeleteUser_ShouldCascadeDeletePrivateRecipes` in [UserServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend.Tests/UserServiceTests.cs).

2. **Risk #6 (Ingredient Catalog)**:
   - The current catalog only contains 20 seeded ingredients in [AppDbContext.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L51-L75). There is no server-side dynamic search; the frontend pre-fetches the entire catalog via `GET /api/ingredients` and performs autocomplete client-side.
   - **Fix**: Expand the seed list in [AppDbContext.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L51-L75) with 20 additional popular Polish cooking ingredients (e.g., *twaróg*, *sauerkraut*, *sausage*, *majeranek*, etc.) to make the catalog robust.
   - **Test**: Add a content smoke/catalog coverage test in `backend.Tests` (such as `IngredientServiceTests` or `IngredientIntegrationTests`) to query the catalog and verify that it contains at least 40 items and covers key Polish staples.

---

## Detailed Findings

### GDPR User Deletion & Cascade Delete

- **Controller Action**: `UserController.DeleteMe` is defined in [UserController.cs:L18-40](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/UserController.cs#L18-L40). It extracts the user's ID from claims and calls `_userService.DeleteUser(userId, out var errorMessage)`.
- **Service Action**: `UserService.DeleteUser` in [UserService.cs:L121-143](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/UserService.cs#L121-L143) retrieves the user entity via `_dbContext.Users.Find(userId)` and deletes it via `_dbContext.Users.Remove(user)`.
- **Entity Definitions**: 
  - [User.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Models/User.cs) contains `public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();`.
  - [Recipe.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Models/Recipe.cs) has a nullable `public Guid? UserId { get; set; }` and navigation `public User? User { get; set; }`.
- **EF Core Configuration**: [AppDbContext.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L38-L43) configures cascade delete:
  ```csharp
  modelBuilder.Entity<Recipe>()
      .HasOne(r => r.User)
      .WithMany(u => u.Recipes)
      .HasForeignKey(r => r.UserId)
      .OnDelete(DeleteBehavior.Cascade);
  ```
- **The In-Memory Defect**: In-memory database provider (`Microsoft.EntityFrameworkCore.InMemory`) does not enforce referential integrity or cascade deletes at the database level. Instead, it relies on EF Core's Change Tracker, which only cascades deletes for loaded/tracked related entities in memory. In `DeleteUser`, `_dbContext.Users.Find(userId)` only loads the `User` record; it does *not* load any related recipes. Thus, the Change Tracker is unaware of the dependent recipes, causing them to survive deletion during unit and integration tests.

### Ingredient Catalog Coverage

- **Seeding Logic**: Seed data is hardcoded in [AppDbContext.cs:L51-75](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L51-L75). It contains exactly 20 ingredients.
- **API Endpoint**: `GET /api/ingredients` is implemented in [IngredientsController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/IngredientsController.cs) and fetches all items from the database sorted alphabetically using [IngredientService.GetIngredientsAsync](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/IngredientService.cs#L16-L23).
- **Frontend Autocomplete**: The frontend pre-fetches the entire list upon dashboard or recipe modal creation and filters it using client-side TypeScript array matching (e.g. `availableIngredients.filter(i => i.name.toLowerCase().includes(query))`).
- **Gaps**: Standard Polish recipes (like pierogi, barszcz, bigos) cannot be fully described using only the 20 seeded ingredients because essential items such as *twaróg*, *kapusta kiszona*, *kiełbasa*, *burak*, and Polish soup root greens (*włoszczyzna*) are missing.

---

## Code References

- [UserController.cs:L18-40](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/UserController.cs#L18-L40) - `DeleteMe` endpoint action.
- [UserService.cs:L121-143](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/UserService.cs#L121-L143) - `DeleteUser` service method.
- [AppDbContext.cs:L38-43](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L38-L43) - Fluent API cascade delete configuration.
- [AppDbContext.cs:L51-75](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L51-L75) - Ingredient seed list.
- [UserServiceTests.cs:L170-186](file:///c:/Users/reade/Documents/10xDev%20Project/backend.Tests/UserServiceTests.cs#L170-L186) - Existing `DeleteUser_ShouldRemoveUserFromDatabase` test.

---

## Architecture Insights

- **Change Tracker vs. DB Constraint**: The project relies heavily on database-level constraints for cascade deletes in production. However, to keep tests green and meaningful using InMemory database, C# service operations must eagerly load dependent entity collections (e.g. using `.Include()`) before deleting root records.
- **Client-Side Autocomplete**: Since searching ingredients is performed entirely on the client, database performance is not impacted by ingredient catalog size. Expanding the seed catalog on the server has zero performance cost on the query path.

---

## Historical Context

- `context/archive/2026-05-28-user-data-retention/plan.md` - Set up the user data retention features and the background retention service.
- `context/archive/2026-05-28-private-recipe-crud/plan.md` - Set up private recipe creation and linked the recipes to specific user IDs.

---

## Related Research

- None.

---

## Open Questions

- None.
