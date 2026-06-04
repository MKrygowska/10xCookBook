# Phase 1 Test Rollout — Critical-Path Coverage Implementation Plan

## Overview

This plan establishes critical-path test coverage for rollout Phase 1 of the test plan, addressing:
1. **Risk #1 (Private Recipe Isolation):** Verifying the controller JWT claims parsing boundary (HTTP → JWT → service) where private recipes of User A are accessible, but private recipes of User B are strictly excluded from search results.
2. **Risk #2 (Match Rate Silent Miscalculation):** Verifying the weighted scoring algorithm, including Banker's rounding, weights (1.0 for primary, 0.1 for spice), and sorting tie-breakers under edge cases.

## Current State Analysis

- The `RecipesController.MatchRecipes` endpoint is protected by `[Authorize]` at class level and delegates JWT-based `userId` parsing via `TryGetUserId()` to `RecipeService.MatchRecipesAsync`.
- Currently, there are **no integration/HTTP-level tests** (no `WebApplicationFactory`). Unit tests in `RecipeServiceTests.cs` exist but they bypass the controller layer entirely.
- The `RecipeService.MatchRecipesAsync` uses Banker's rounding (`Math.Round` by default) and applies a 1.0 weight for primary ingredients and 0.1 for spices. It sorts the matched recipes by match rate, then missing primary count, then title.
- Existing tests in `RecipeServiceTests.cs` cover some match rate calculations and sorting tie-breakers, but they do not check critical edge cases (all-spice recipes, zero-match recipes, empty input, case sensitivity) nor do they assert sorting order with multi-level tie-breakers exhaustively.

## Desired End State

- The unified backend project has the `Microsoft.AspNetCore.Mvc.Testing` package installed.
- A custom integration test class `RecipeIntegrationTests` is introduced, boot-strapping a test server (`WebApplicationFactory<Program>`) that overrides the default SQL Server configuration to use an EF Core in-memory database (`InMemoryDatabase`).
- Integration tests assert that `/api/recipes/match` returns `401 Unauthorized` for requests with missing or invalid tokens, and returns isolated, correct recipe search results for requests with valid tokens (verifying User A sees their own private recipes but not User B's).
- Extended unit tests in `RecipeServiceTests.cs` verify match rate calculations (focusing on Banker's rounding, weights) and sorting tie-breakers with a robust, independent set of expected outcomes.

### Key Discoveries:

- **Unified Assembly:** The tests and the API reside in the same project ([10x-cookbook-backend.csproj](file:///c:/Users/reade/Documents/10xDev%20Project/backend/10x-cookbook-backend.csproj)), meaning the `Program` entry point is internal but visible to test code without `InternalsVisibleTo` or making it public.
- **JWT Key:** The default JWT secret for Development mode is `"SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong"`.

## What We're NOT Doing

- We are not refactoring the `RecipesController` or `RecipeService` implementation code. This change is strictly about writing tests to cover existing code.
- We are not configuring a real SQLite database or applying migrations for integration tests; we will use EF's `InMemoryDatabase`.
- We are not writing frontend tests or Mobile E2E tests.

## Implementation Approach

We will execute this rollout in three phases. Because the tests will be added alongside existing components, we will use `/10x-implement` for Phase 1 (infrastructure) and can utilize `/10x-tdd` for Phase 2 and 3 if we write the assertions first.

---

## Phase 1: Setup Test Infrastructure (xUnit Integration Tests)

### Overview
Add necessary test NuGet packages and bootstrap `WebApplicationFactory<Program>` to support HTTP-level testing of the ASP.NET Core API with in-memory database overrides.

### Changes Required:

#### 1. NuGet Dependencies
**File**: [10x-cookbook-backend.csproj](file:///c:/Users/reade/Documents/10xDev%20Project/backend/10x-cookbook-backend.csproj)  
**Intent**: Add integration testing infrastructure to the project.  
**Contract**: Add `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.8" />`.

#### 2. Base Integration Test Class
**File**: [RecipeIntegrationTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeIntegrationTests.cs) [NEW]  
**Intent**: Create `RecipeIntegrationTests` class that configures the local in-memory test server.  
**Contract**: 
*   Uses `WebApplicationFactory<Program>` to boot-strap the server.
*   Overrides `DbContextOptions<AppDbContext>` in `ConfigureWebHost` to use `UseInMemoryDatabase("RecipeIntegrationTestDb")`.
*   Includes a helper method to generate valid JWT tokens with custom `userId` and `email` claims (signed with the default development secret: `"SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong"`).
*   Includes a DB seeding helper method to seed standard test user accounts, ingredients, and recipes.

### Success Criteria:

#### Automated Verification:
- Backend project builds cleanly: `dotnet build`
- All current tests pass: `dotnet test`

#### Manual Verification:
- None required (setup phase).

---

## Phase 2: Controller JWT Wiring & Data Isolation Integration Tests (Risk #1)

### Overview
Write integration tests targeting `/api/recipes/match` to verify that JWT authentication is active and data isolation is enforced between users.

### Changes Required:

#### 1. Authentication and Isolation Tests
**File**: [RecipeIntegrationTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeIntegrationTests.cs)  
**Intent**: Add integration tests asserting JWT requirements and isolation logic.  
**Contract**:
*   `MatchRecipes_ShouldReturnUnauthorized_WhenTokenIsMissing`: Sends a POST to `/api/recipes/match` without bearer token, asserts `401 Unauthorized` status code.
*   `MatchRecipes_ShouldReturnUnauthorized_WhenTokenIsMalformed`: Sends request with an invalid/random bearer token, asserts `401 Unauthorized`.
*   `MatchRecipes_ShouldIsolatePrivateRecipes`:
    *   Creates User A and User B.
    *   Seeds User A's private recipe (e.g. "Private Pizza A", containing "ser") and User B's private recipe (e.g. "Private Pasta B", containing "ser").
    *   POST to `/api/recipes/match` with User A's JWT token and ingredient "ser".
    *   Asserts response status is `200 OK`.
    *   Asserts response list **contains** "Private Pizza A" but **excludes** "Private Pasta B".

### Success Criteria:

#### Automated Verification:
- Backend compiles and tests run successfully: `dotnet test`
- Both new integration tests pass.

#### Manual Verification:
- None required.

---

## Phase 3: Match Rate Scoring & Sorting Edge Cases Unit Tests (Risk #2)

### Overview
Add comprehensive unit tests to [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs) to cover rounding behavior, edge cases, and multi-level sorting tie-breakers.

### Changes Required:

#### 1. Match Rate Scoring Unit Tests
**File**: [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs)  
**Intent**: Expand the existing test class with unit tests for Banker's rounding boundaries, spice-only matching, empty inputs, and sorting tie-breakers.  
**Contract**:
*   `MatchRecipes_ShouldFollowBankersRounding`: Assert match rate at midpoint boundaries (e.g. `90.5` rounds to `90`, `91.5` rounds to `92`).
*   `MatchRecipes_ShouldHandleAllSpiceRecipe`: Seed a recipe with only spices and assert it computes correct match rate.
*   `MatchRecipes_ShouldExcludeZeroPercentMatches`: Seed a recipe with no overlapping ingredients and assert it is not returned (as rate is 0).
*   `MatchRecipes_ShouldSortByTieBreakersCorrectly`:
    *   Seed recipes with identical match rates but different missing primary counts, and recipes with identical match rates and missing counts but different titles.
    *   Assert the resulting list is ordered exactly by `MatchRate` DESC, then `MissingPrimaryCount` ASC, then `Title` ASC.

### Success Criteria:

#### Automated Verification:
- All unit tests pass successfully: `dotnet test`

#### Manual Verification:
- None required.

---

## Testing Strategy

### Unit Tests:
- Covered by unit tests added in Phase 3 targeting `RecipeService.MatchRecipesAsync`.

### Integration Tests:
- Covered by integration tests added in Phase 2 using `WebApplicationFactory<Program>` to test `/api/recipes/match`.

---

## Performance Considerations
- No production performance impact. The EF Core `InMemoryDatabase` is used only during tests and is fast.

## Migration Notes
- No database migration or data schema changes are required.

## References
- Research Findings: `context/changes/testing-critical-path-coverage/research.md`
- Controller wiring: [RecipesController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L22-L33)
- Service logic: [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L20-L124)

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Setup Test Infrastructure (xUnit Integration Tests)

#### Automated

- [x] 1.1 Backend project builds cleanly: `dotnet build` — 5aab20c
- [x] 1.2 All current tests pass: `dotnet test` — 5aab20c

### Phase 2: Controller JWT Wiring & Data Isolation Integration Tests (Risk #1)

#### Automated

- [x] 2.1 Backend compiles and tests run successfully: `dotnet test` — 346de1f

### Phase 3: Match Rate Scoring & Sorting Edge Cases Unit Tests (Risk #2)

#### Automated

- [x] 3.1 All unit tests pass successfully: `dotnet test` — 728413c
