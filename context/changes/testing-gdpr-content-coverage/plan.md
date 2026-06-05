# GDPR & Content Coverage Testing Implementation Plan

## Overview

Implement backend user deletion cascade delete verification and expand the global ingredient catalog with 20 common Polish cooking ingredients, writing tests for both to satisfy Phase 3 of the test rollout.

## Current State Analysis

- **GDPR Deletion**: `UserService.DeleteUser` removes the user entity, but because it doesn't load related recipes, the `InMemoryDatabase` provider's Change Tracker fails to cascade the deletion of private recipes in tests.
- **Ingredient Catalog**: The database currently only seeds 20 ingredients, which is too sparse for popular Polish recipes. Autocomplete search is handled entirely on the client, and we have no automated smoke tests verifying catalog coverage.

## Desired End State

- Deleting a user via `DeleteUser` cascades to delete all their private recipes, verified by unit tests running against the InMemory database.
- The global ingredient catalog is expanded to 40 items, seeding common Polish cooking ingredients, verified by an automated smoke test asserting size and content coverage.

### Key Discoveries:

- [UserService.cs:L121-143](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/UserService.cs#L121-L143) - `DeleteUser` retrieves the user entity without loading the `Recipes` relationship.
- [AppDbContext.cs:L38-43](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L38-L43) - Cascade delete behavior is configured correctly on the `Recipe.User` relationship.
- [AppDbContext.cs:L51-75](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Data/AppDbContext.cs#L51-L75) - Current ingredient seeds are limited to 20 hardcoded records.

## What We're NOT Doing

- Implementing new API controllers or frontend interface views.
- Restructuring the database cascade rules or swapping database providers in testing.
- Adding complex search algorithms to the backend (ingredient search remains client-side).

## Implementation Approach

1. **Eager-load recipes**: Modify `UserService.DeleteUser` to eager-load `Recipes` using `.Include(u => u.Recipes)` before removing the user. This ensures EF Core's Change Tracker deletes the child recipes in memory, which resolves the cascade delete verification issue in testing.
2. **Seed Polish Ingredients**: Expand the seeds in `AppDbContext.cs` with 20 Polish cooking ingredients (twaróg, kiełbasa, kapusta kiszona, ogórek kiszony, etc.), keeping `IsSpiceOrStaple` designations accurate.
3. **EF Core Migration**: Create a new database migration `AddPolishIngredientsSeed` to apply these seeds to the database schema.
4. **Write Tests**: Implement the cascade delete verification test in `UserServiceTests.cs` and create a smoke test for catalog coverage in `IngredientServiceTests.cs`.

## Critical Implementation Details

- **Change Tracker & Eager Loading** — The EF Core InMemory Database does not natively enforce SQL foreign key cascade constraints. EF Core simulates this client-side, but only for entities loaded into the context. Eager-loading `Recipes` is required to ensure child records are removed in test environments.

---

## Phase 1: Backend Service Updates & Migration

### Overview

Update the user deletion service query, add 20 Polish cooking ingredients to the seed data, and generate the EF database migration.

### Changes Required:

#### 1. Eager Loading in DeleteUser
**File**: `backend/Services/UserService.cs`
**Intent**: Eagerly load user recipes before removal so the Change Tracker deletes related records in test databases.
**Contract**: Rewrite the user lookup to use `Include(u => u.Recipes)` and `FirstOrDefault(u => u.Id == userId)`.

#### 2. Ingredient Catalog Expansion
**File**: `backend/Data/AppDbContext.cs`
**Intent**: Add 20 popular Polish ingredients to the seeded data with unique Guids.
**Contract**: Append the new items to the `ingredients` List in `OnModelCreating` (e.g. twaróg, kiełbasa, kapusta kiszona, ogórek kiszony, schab, śmietana, koperek, natka pietruszki, majeranek, liść laurowy, ziele angielskie, olej rzepakowy, kasza gryczana, bułka tarta, burak, pieczarki, boczek wędzony, chrzan, korzeń pietruszki, seler).

#### 3. Database Migration
**File**: `backend/Data/Migrations/` [NEW]
**Intent**: Generate a migration to insert the new seed ingredients into production database instances.
**Contract**: Generate migration using `dotnet ef migrations add AddPolishIngredientsSeed --project backend`.

### Success Criteria:

#### Automated Verification:
- Backend compiles cleanly: `dotnet build`

#### Manual Verification:
- None.

**Implementation Note**: After completing this phase, run the automated verification and check the generated migration code before moving to Phase 2.

---

## Phase 2: Automated Tests

### Overview

Implement tests to verify user deletion cascades and that the ingredient catalog covers Polish culinary staples.

### Changes Required:

#### 1. GDPR Cascade Delete Test
**File**: `backend.Tests/UserServiceTests.cs`
**Intent**: Assert that deleting a user removes both the user and their associated private recipes.
**Contract**: Add `DeleteUser_ShouldCascadeDeletePrivateRecipes` test. Seed a user and a private recipe, delete the user, and assert that the recipe is deleted from the DbContext.

#### 2. Ingredient Catalog Smoke Test
**File**: `backend.Tests/IngredientServiceTests.cs` [NEW]
**Intent**: Verify that the ingredient catalog size is at least 40 and includes Polish cooking staples without hardcoding the entire list.
**Contract**: Create `IngredientServiceTests` with test `GetIngredients_ShouldCoverPolishStaples`. Assert count is >= 40 and check that "twaróg", "kiełbasa", and "kapusta kiszona" exist.

### Success Criteria:

#### Automated Verification:
- All backend tests run and pass: `dotnet test`

#### Manual Verification:
- None.

**Implementation Note**: Verify that all 43 backend tests pass successfully.

---

## Phase 3: Cookbook Sync

### Overview

Update the stateful test rollout plan and document the testing patterns for cascade deletes and smoke testing.

### Changes Required:

#### 1. Test Plan Status Update
**File**: `context/foundation/test-plan.md`
**Intent**: Mark Phase 3 as complete and link to the archived folder.
**Contract**: Update Phase 3 status to complete and set change folder path.

#### 2. Cookbook Documentation
**File**: `context/foundation/test-plan.md`
**Intent**: Document the testing patterns for GDPR/cascade-deletes and catalog content smoke testing in section 6.
**Contract**: Replace TBDs in sections 6.4 and 6.5 with descriptions of eager-loading InMemory cascading and non-brittle content smoke testing.

### Success Criteria:

#### Automated Verification:
- Git status has no uncommitted changes in the change folder.

#### Manual Verification:
- None.

---

## Testing Strategy

### Unit Tests:
- `DeleteUser_ShouldCascadeDeletePrivateRecipes` (in `UserServiceTests.cs`)
- `GetIngredients_ShouldCoverPolishStaples` (in `IngredientServiceTests.cs`)

### Integration Tests:
- Covered by checking the database cascade effect locally during service invocation.

### Manual Testing Steps:
- None needed (covered by unit/integration tests).

## Performance Considerations

- Autocomplete searches are client-side, so extending the catalog to 40 items has negligible overhead on server-side queries.

## Migration Notes

- Ensure `dotnet ef database update` is executed on deployment to populate the new ingredient seeds.

## References

- Related research: `context/changes/testing-gdpr-content-coverage/research.md`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Backend Service Updates & Migration

#### Automated
- [ ] 1.1 Backend compiles cleanly: `dotnet build`

### Phase 2: Automated Tests

#### Automated
- [ ] 2.1 Backend tests run and pass: `dotnet test`

### Phase 3: Cookbook Sync

#### Automated
- [ ] 3.1 Test plan marked complete and cookbook sections updated
