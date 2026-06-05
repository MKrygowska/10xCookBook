---
date: 2026-06-04T16:21:00+02:00
researcher: Antigravity
git_commit: 9a8e2d4c6a1ae0f1f73b802834775c3ac4d81592
branch: test/testing-critical-path-coverage
repository: MKrygowska/10xCookBook
topic: "Phase 1 test rollout — critical-path coverage for data isolation and match rate"
tags: [research, codebase, recipes, ingredients, matching, testing]
status: complete
last_updated: 2026-06-04
last_updated_by: Antigravity
---

# Research: Phase 1 Test Rollout — Critical-Path Coverage

**Date**: 2026-06-04T16:21:00+02:00
**Researcher**: Antigravity
**Git Commit**: 9a8e2d4c6a1ae0f1f73b802834775c3ac4d81592
**Branch**: `test/testing-critical-path-coverage`
**Repository**: `MKrygowska/10xCookBook`

## Research Question

Conduct research for Phase 1 of the test plan rollout:
1. Prove JWT data isolation in the recipes controller (`/match` endpoint).
2. Ground the ingredient match rate scoring formula and sorting, identifying gaps, edge cases, and the cheapest testing layer.

---

## Summary

1. **Risk #1 (Private Recipe Isolation):**
   * The [RecipesController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L22-L33) correctly extracts the `userId` as a nullable GUID (`Guid?`) using `TryGetUserId()`.
   * The database-level filtering in [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L50) correctly isolates private recipes (`(r.IsPublic || (userId != null && r.UserId == userId))`).
   * **Crucial Gap:** Existing tests in [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs) are purely service-level. There are **no HTTP/Controller-level integration tests**. If a controller refactoring breaks claim parsing or JWT token extraction (e.g. passing a hardcoded `null`), the entire system would fail silently.
   * **Cheapest Test:** An ASP.NET Core integration test using `WebApplicationFactory<Program>` that simulates HTTP requests with a JWT and asserts database data isolation.

2. **Risk #2 (Scoring Formula & Sorting):**
   * Weights are assigned as: **1.0** for primary ingredients and **0.1** for spices/staples.
   * The formula used is: `matchRate = (int)Math.Round((matchedWeight / totalWeight) * 100)` with .NET's default **Banker's rounding** (midpoint rounding to nearest even number).
   * **Sorting rules:** Results are ordered by `MatchRate` descending, then `MissingPrimaryCount` ascending, then `Title` ascending.
   * Existing tests are mirrored calculations and don't verify correctness against an independent business oracle.
   * **Cheapest Test:** hermetic unit tests verifying edge cases (empty input, all-spice recipes, 0% match, case-sensitivity) directly on `RecipeService`.

---

## Detailed Findings

### Risk #1: Private Recipe Isolation & JWT Controller Wiring

*   **Extraction details in [RecipesController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L30-L31):**
    ```csharp
    var userId = TryGetUserId();
    var matchedRecipes = await _recipeService.MatchRecipesAsync(request.Ingredients, userId);
    ```
    `RecipesController` calls `TryGetUserId()` which returns a nullable `Guid?`.
*   **Helper details in [BaseApiController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/BaseApiController.cs#L21-L29):**
    ```csharp
    protected Guid? TryGetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
    ```
    If the JWT is missing the `NameIdentifier` claim or if it is malformed, `TryGetUserId()` returns `null` silently rather than throwing.
*   **Class Auth Guard:**
    [RecipesController.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L12-L13) has `[Authorize]` at class level, and there is **no** `[AllowAnonymous]` override on the POST `/api/Recipes/match` endpoint (line 22). This means ASP.NET Core authentication middleware will reject unauthenticated HTTP requests before they reach the endpoint.
*   **Service-level Filtering in [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L48-L53):**
    ```csharp
    var recipes = await _dbContext.Recipes
        .AsNoTracking()
        .Where(r => (r.IsPublic || (userId != null && r.UserId == userId)) && r.RecipeIngredients.Any(ri => filterIds.Contains(ri.IngredientId)))
        .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
        .ToListAsync();
    ```
    This LINQ filter is correct:
    *   If `userId == null` (anonymous): Only public recipes (`r.IsPublic`) pass.
    *   If `userId == someGuid` (authenticated): Public recipes OR the user's private recipes (`r.UserId == userId`) pass.
    *   Other users' private recipes (`!r.IsPublic && r.UserId != userId`) are always excluded.
*   **Existing Test Coverage in [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs):**
    *   `MatchRecipes_ShouldIncludePrivateRecipesOfCurrentUser` (line 157)
    *   `MatchRecipes_ShouldExcludePrivateRecipesOfOtherUsers` (line 195)
    *   *Observation:* These test `RecipeService` directly, passing in mock database contexts. They do not cover controller routing or JWT claim decoding.

### Risk #2: Recipe Matching Scoring & Rounding Formula

*   **Scoring rules in [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L69-L96):**
    *   Primary ingredient: weight = **1.0**
    *   Spice or staple (`IsSpiceOrStaple == true`): weight = **0.1**
    *   `totalWeight` is calculated as the sum of weights of all ingredients in the recipe.
    *   `matchedWeight` is calculated as the sum of weights of all ingredients matched by the user.
    *   `matchRate` is calculated as:
        ```csharp
        double matchRateDouble = totalWeight > 0 ? (matchedWeight / totalWeight) * 100 : 0;
        int matchRate = (int)Math.Round(matchRateDouble);
        ```
*   **Rounding Behavior:**
    `Math.Round(double)` uses Banker's rounding (round to nearest even number at midpoints).
    *   `90.5` rounds to `90`
    *   `91.5` rounds to `92`
    *   `90.9` rounds to `91`
    *   `9.09` rounds to `9`
*   **Filtering & Sorting Rules in [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L99-L123):**
    *   Only recipes with `matchRate > 0` are included.
    *   Results are sorted using:
        1. `MatchRate` descending
        2. `MissingPrimaryCount` ascending (fewer missing primary ingredients is better)
        3. `Title` ascending (alphabetical tie-breaker)
    *   A maximum of 20 recipes is returned (`.Take(20)`).
*   **Existing tests in [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs):**
    *   `MatchRecipes_ShouldComputeWeightedMatchRateWithSpicesCorrectly` (line 63) asserts exact match rate values:
        *   Tomato soup (1 primary tomato [1.0] + 1 spice olive oil [0.1]). Total weight = 1.1.
        *   User has tomato: matched weight = 1.0. `1.0 / 1.1 * 100 = 90.9%` -> rounds to `91`. Test asserts `Equal(91, tomatoSoup.MatchRate)` (line 81).
        *   User has olive oil: matched weight = 0.1. `0.1 / 1.1 * 100 = 9.09%` -> rounds to `9`. Test asserts `Equal(9, tomatoSoup2.MatchRate)` (line 97).
    *   `MatchRecipes_ShouldSortByMatchRateAndLeastMissingPrimaryIngredients` (line 101) asserts that Zupa cebulowa (52% match, 1 missing primary) is sorted before Makaron z sosem (35% match, 2 missing primary).
*   **Oracle Problem:**
    The tests use hardcoded values matching the formula exactly, without verifying whether Banker's rounding or the specific weights conform to independent product expectations.
*   **Edge Cases:**
    *   *Empty inputs:* If user passes an empty ingredient list, `MatchRecipesAsync` returns empty list on line 22-25.
    *   *0% match:* Excluded on line 99 (`matchRate > 0`).
    *   *All-spice recipes:* A recipe with only spices (e.g. salt and pepper) will have low total weight (e.g. 0.2). If a user matches all, match rate is 100%. If none matches, it is excluded.
    *   *Whitespace/Case sensitivity:* User input is trimmed and lowercased on lines 28-31. Matches are case-insensitive.

---

## Code References

*   [RecipesController.cs:L22-33](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L22-L33) — `/match` endpoint logic and extraction of user identity.
*   [BaseApiController.cs:L21-29](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/BaseApiController.cs#L21-L29) — `TryGetUserId()` claims extraction helper.
*   [RecipeService.cs:L48-53](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L48-L53) — Private recipe isolation WHERE query.
*   [RecipeService.cs:L65-97](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs#L65-L97) — Scoring weights, Banker's rounding, and percentage logic.
*   [RecipeServiceTests.cs:L63-99](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs#L63-L99) — Existing match rate unit test.
*   [RecipeServiceTests.cs:L157-230](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs#L157-L230) — Existing private recipe data isolation tests.

---

## Architecture Insights

*   **Controller-Service Separation:** The backend cleanly separates API/HTTP parsing concerns from database querying and match rate math.
*   **JWT Handled by Middleware:** Authentication relies on standard ASP.NET Core JWT bearer authentication configured in `Program.cs`. Claims principal mapping is standard.
*   **In-Memory DB for Testing:** xUnit tests use `Microsoft.EntityFrameworkCore.InMemory` which matches EF query syntax but does not enforce real SQL constraints or transaction rollbacks.

---

## Historical Context (from prior changes)

*   [2026-05-26-public-recipe-matching/plan.md](file:///c:/Users/reade/Documents/10xDev%20Project/context/archive/2026-05-26-public-recipe-matching/plan.md) — S-01 implementation plan notes: initial DB-level pre-filtering optimization and case-insensitive deduplication logic.
*   [2026-05-28-unified-matching/plan.md](file:///c:/Users/reade/Documents/10xDev%20Project/context/archive/2026-05-28-unified-matching/plan.md) — Unified matching plan: unified public and private recipe search logic.
*   [2026-05-29-backend-controllers-refactor/plan.md](file:///c:/Users/reade/Documents/10xDev%20Project/context/archive/2026-05-29-backend-controllers-refactor/plan.md) — Endpoint refactoring from Minimal APIs to classic controllers, which introduced `BaseApiController.cs` and `RecipesController.cs`.

---

## Open Questions

1. **Midpoint Rounding Intent:**
   Should the rounding algorithm be Banker's rounding (`Math.Round`), or should it always round halves up to match standard human expectations (e.g. `90.5` -> `91` instead of `90`)? Currently, standard C# Banker's rounding is used.
2. **Missing Controller Integration Testing:**
   Is there any existing setup for `WebApplicationFactory` in the test suite to write HTTP-level integration tests easily? (No, there isn't; it must be bootstrapped).
