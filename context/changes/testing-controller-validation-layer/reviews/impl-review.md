<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Controller & Validation Layer Testing

- **Plan**: `context/changes/testing-controller-validation-layer/plan.md`
- **Scope**: Full plan review
- **Date**: 2026-06-05
- **Verdict**: NEEDS ATTENTION
- **Findings**: 2 critical, 7 warnings, 3 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | FAIL |
| Architecture | PASS |
| Pattern Consistency | WARNING |
| Success Criteria | PASS |

## Findings

### F1 — EF Core Tracking Exception during Recipe Update

- **Severity**: ❌ CRITICAL
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Safety & Quality
- **Location**: `backend/Services/RecipeService.cs:248`
- **Detail**: Inside `UpdateRecipeAsync()`, calling `recipe.RecipeIngredients.Clear()` marks the tracked relations as `Deleted`. When the code subsequently loops through `request.Ingredients` and adds new `RecipeIngredient` objects, some of those objects may have the same compound key (`RecipeId`, `IngredientId`) as the ones marked for deletion. EF Core's change tracker throws an `InvalidOperationException` because it cannot track two instances with the same key.
- **Fix**: Replace the `.Clear()` + loop with a proper collection merge:
  - Remove ingredients not present in the update request.
  - Update the quantity for ingredients already present.
  - Add new ingredients.
  - Strength: Resolves the runtime crash when users edit recipes containing existing ingredients.
  - Tradeoff: Adds slightly more complex merge logic.
  - Confidence: HIGH — standard EF Core collection update pattern.
  - Blind spot: None.
- **Decision**: PENDING

### F2 — `isAuthenticated` returns false for valid tokens

- **Severity**: ❌ CRITICAL
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `frontend/src/app/services/auth.service.ts:43`
- **Detail**: If a valid token exists in `localStorage`, but `isAuthenticatedSubject.value` is `false` (e.g., set after service construction), `isAuthenticated()` returns `false` instead of updating and returning `true`.
- **Fix**: Update the behavior subject and return `true` if a valid unexpired token is found in localStorage.
  - Strength: Syncs the authentication state reactively and removes the need for manual service instantiation in unit tests.
  - Tradeoff: Adds a side effect inside a getter-like method.
  - Confidence: HIGH — simple fix.
  - Blind spot: None.
- **Decision**: PENDING

### F3 — Naive JWT `atob` decode throws on unpadded strings

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `frontend/src/app/services/auth.service.ts:61`
- **Detail**: Using `atob` directly on Base64URL payload strings fails if the payload length is not a multiple of 4 (as Base64URL strips padding `=`). If it fails, it throws an error and incorrectly marks valid tokens as expired.
- **Fix**: Implement a padding helper that replaces Base64URL characters (`-`, `_`) and adds `=` padding before calling `atob`.
- **Decision**: PENDING

### F4 — Scalability Risk in `MatchRecipesAsync`

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Safety & Quality
- **Location**: `backend/Services/RecipeService.cs:48`
- **Detail**: Searching matches on common ingredients (like salt/oil) eager-loads almost the entire database into memory.
- **Fix**: Add a limit (e.g. `Take(100)`) at the database query level when retrieving potential matches.
- **Decision**: PENDING

### F5 — Missing Null Checks in Controller Actions

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `backend/Controllers/AuthController.cs:19`
- **Detail**: `Register` and `Login` do not guard against a null `request` object, which can cause a 500 error if null payload bypasses validation filters.
- **Fix**: Add `if (request == null) return BadRequest(...);` checks.
- **Decision**: PENDING

### F6 — Silent Exception Swallowing (No Logger)

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `backend/Controllers/RecipesController.cs:71`
- **Detail**: Generic `catch (Exception)` blocks in controller endpoints swallow internal exceptions without logging them, making production support difficult.
- **Fix**: Inject `ILogger<RecipesController>` and log exceptions before returning the bad request.
- **Decision**: PENDING

### F7 — AuthService unit tests placed inside Guard spec

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: `frontend/src/app/guards/auth.guard.spec.ts:50`
- **Detail**: Unit tests for `AuthService` token expiration are placed in the route guard test file instead of a service spec file.
- **Fix**: Move the AuthService tests to `frontend/src/app/services/auth.service.spec.ts`.
- **Decision**: PENDING

### F8 — Thread-unsafe environment variable mutation in tests

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `backend.Tests/ValidationIntegrationTests.cs:30`
- **Detail**: Process-wide environment variables mutated in constructors can cause race conditions in parallel tests.
- **Fix**: Remove the environment variable mutation and rely on `builder.UseEnvironment("Development")`.
- **Decision**: PENDING

### F9 — Stale local storage after account deletion

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `frontend/src/app/services/auth.service.ts:39`
- **Detail**: `deleteAccount()` does not clear localStorage or authentication status on the client.
- **Fix**: Pipe `deleteAccount()` to execute `logout()` on success.
- **Decision**: PENDING
