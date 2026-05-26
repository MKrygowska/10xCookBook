# Public Recipe Matching — Post-Review Fixes Implementation Plan

## Overview

This plan addresses three findings from the retrospective plan review of the S-01 public recipe matching feature. All three findings were triaged as FIXED during the plan review. The fixes target: backend query performance, missing frontend unit tests, and a case-sensitivity bug in ingredient deduplication.

## Current State Analysis

The public recipe matching feature is fully implemented and merged to `main` (commit `d21a763`). A retrospective plan review identified three issues:

1. **F1 — In-Memory Recipe Matching Scales Poorly** (`RecipeService.cs:30-35`): All public recipes are loaded into memory with eager-loaded ingredient graphs before any filtering. The matching algorithm runs entirely in C# memory.
2. **F2 — Missing Dashboard Component Tests** (`frontend/src/app/components/dashboard/`): No `dashboard.component.spec.ts` exists, violating the repo's `*.spec.ts` testing convention.
3. **F3 — Case-Sensitive Tag Deduplication** (`dashboard.component.ts:79`): `addIngredient()` uses `.includes()` (case-sensitive) for deduplication, while `updateAvailableIngredients()` uses `.toLowerCase()` comparison (case-insensitive). A user typing "Pomidor" when "pomidor" is already selected bypasses the guard.

### Key Discoveries:

- Backend uses EF Core with SQLite (`AppDbContext`), scoped DI, xUnit + InMemory for tests — [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs)
- `Ingredient.Id` is `Guid`, not `int` — [Ingredient.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Models/Ingredient.cs#L8)
- `RecipeIngredient` links `Recipe` (Guid) to `Ingredient` (Guid) with a `Quantity` string — [RecipeIngredient model](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Models/RecipeIngredient.cs)
- Backend tests use `CreateInMemoryDbContext()` + `SeedTestData()` helpers with Polish ingredient names — [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs#L11-L58)
- Frontend has only one existing spec file (`auth.guard.spec.ts`) using `TestBed`, `HttpClientTestingModule`, and `jasmine.createSpy` — testing pattern is established but sparse
- Dashboard component imports `AuthService`, `RecipeService`, `Router` — standalone component with `CommonModule`, `FormsModule`
- Dashboard styles live in global `styles.scss` due to Angular component stylesheet budget limits
- Lesson: prefer native JS/TS functions over lodash; keep HTML in separate `.html` files — [lessons.md](file:///c:/Users/reade/Documents/10xDev%20Project/context/foundation/lessons.md)

## Desired End State

After this plan is complete:
- Recipe matching queries will pre-filter at the database level using ingredient IDs before loading recipe graphs, reducing memory and network overhead
- The dashboard component will have comprehensive Jasmine/Karma unit tests covering autocomplete, tag management, and recipe matching
- Ingredient deduplication in `addIngredient()` will be case-insensitive, consistent with the rest of the component

## What We're NOT Doing

- Not changing the weighted scoring algorithm itself (spice weight 0.1, primary weight 1.0)
- Not introducing query-level ranking or full-text search — ranking still happens in memory after the filtered set is loaded
- Not adding end-to-end (Cypress/Playwright) tests — only Jasmine/Karma unit tests
- Not refactoring `RecipeService` to use an interface — keeping concrete DI registration
- Not changing the `.Take(20)` result cap
- Not touching `RecipeEndpoints.cs` or `recipe.service.ts` (frontend)

## Implementation Approach

Three independent phases, one per finding. Each phase is self-contained and can be verified independently. Order: backend performance fix first (highest impact), then frontend tests, then the bug fix.

---

## Phase 1: Database-Level Ingredient Containment Filter

### Overview

Add a pre-filtering step to `MatchRecipesAsync` that queries the database for only those public recipes that contain at least one of the user's selected ingredients. This moves the initial filtering from C# memory to SQL, dramatically reducing the amount of data loaded.

### Changes Required:

#### 1. RecipeService — Add DB-level pre-filter

**File**: `backend/Services/RecipeService.cs`

**Intent**: Replace the current query that loads all public recipes (`_dbContext.Recipes.Where(r => r.IsPublic).Include(...).ToListAsync()`) with a two-step approach: first resolve ingredient IDs from names at the DB level, then filter recipes to only those linked to at least one of those ingredient IDs before eager-loading and scoring.

**Contract**: `MatchRecipesAsync(List<string> userIngredientNames)` signature and return type remain unchanged. The method now issues two queries: one to resolve `ingredientIds` from `normalizedUserIngredients`, and one to filter `Recipes` via `RecipeIngredients.Any(ri => ingredientIds.Contains(ri.IngredientId))` before the existing scoring loop.

#### 2. RecipeServiceTests — Verify filter correctness

**File**: `backend/Tests/RecipeServiceTests.cs`

**Intent**: Existing tests already validate weighted scoring, sorting, and normalization. They should continue to pass without modification since the method signature and behavior are unchanged — the optimization is internal. Run existing tests to confirm no regressions.

**Contract**: No changes to test code. All three existing `[Fact]` tests must pass.

### Success Criteria:

#### Automated Verification:

- Backend builds cleanly: `dotnet build` (from `backend/`)
- Existing unit tests pass: `dotnet test` (from `backend/`)

#### Manual Verification:

- Recipe matching returns the same results as before the change (spot-check via the app or HTTP file)

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Dashboard Component Unit Tests

### Overview

Create `dashboard.component.spec.ts` with Jasmine/Karma tests covering the component's core behaviors: initialization, ingredient autocomplete filtering, tag selection/removal, recipe search triggering, and case-sensitive edge cases.

### Changes Required:

#### 1. Dashboard spec file

**File**: `frontend/src/app/components/dashboard/dashboard.component.spec.ts` [NEW]

**Intent**: Create a comprehensive unit test file for `DashboardComponent` following the patterns established in `auth.guard.spec.ts`. Tests should cover: component creation, ingredient loading on init, search query filtering, ingredient selection (adds tag + triggers recipe search), ingredient removal, Enter key behavior, and logout navigation.

**Contract**: Uses `TestBed.configureTestingModule` with `HttpClientTestingModule`. Mocks `AuthService` (via `jasmine.createSpy` or `spyOn`) and `RecipeService` (returning `of(...)` observables). Mocks `Router` with `jasmine.createSpy('navigate')`. Test suite structure:
- `describe('DashboardComponent', () => { ... })`
- `beforeEach` with `TestBed` setup and `fixture.detectChanges()`
- Individual `it(...)` blocks per behavior

### Success Criteria:

#### Automated Verification:

- Frontend builds cleanly: `npm run build` (from `frontend/`)
- New unit tests pass: `npm run test -- --watch=false` (from `frontend/`)

#### Manual Verification:

- Test output shows all dashboard specs passing

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Case-Insensitive Tag Deduplication Fix

### Overview

Fix the case-sensitivity inconsistency in `addIngredient()` so that duplicate detection matches the case-insensitive logic already used in `updateAvailableIngredients()`.

### Changes Required:

#### 1. Dashboard component — Fix addIngredient deduplication

**File**: `frontend/src/app/components/dashboard/dashboard.component.ts`

**Intent**: Replace the case-sensitive `.includes(normalized)` check in `addIngredient()` with a case-insensitive `.some(sel => sel.toLowerCase() === normalized.toLowerCase())` check, matching the pattern already used in `updateAvailableIngredients()` at line 55.

**Contract**: `addIngredient(name: string)` method, line 79. The guard condition changes from `!this.selectedIngredients.includes(normalized)` to `!this.selectedIngredients.some(sel => sel.toLowerCase() === normalized.toLowerCase())`.

#### 2. Dashboard spec — Add case-insensitive deduplication test

**File**: `frontend/src/app/components/dashboard/dashboard.component.spec.ts`

**Intent**: Add a test case that verifies adding "Pomidor" when "pomidor" is already selected does NOT create a duplicate tag.

**Contract**: New `it('should not add duplicate ingredients with different casing', ...)` test.

### Success Criteria:

#### Automated Verification:

- Frontend builds cleanly: `npm run build` (from `frontend/`)
- All unit tests pass (including new dedup test): `npm run test -- --watch=false` (from `frontend/`)

#### Manual Verification:

- Manually test in the app: add "pomidor", then try typing "Pomidor" + Enter — should be rejected as duplicate

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- **Backend** (existing): weighted match rate, sorting, case/space normalization — all must pass after Phase 1 optimization
- **Frontend** (new in Phase 2): component lifecycle, autocomplete, tag CRUD, recipe search, case-insensitive dedup

### Integration Tests:

- Backend integration tests exist in `backend/Tests/` using EF InMemory provider — run via `dotnet test`

### Manual Testing Steps:

1. Open the app, log in, navigate to dashboard
2. Search for an ingredient (e.g., "pomidor"), verify autocomplete dropdown appears
3. Select the ingredient, verify tag appears and recipes load
4. Try adding the same ingredient with different casing — should be rejected
5. Remove the tag, verify recipes clear
6. Verify recipe match percentages look correct

## References

- Plan review findings: [plan-review.md](file:///c:/Users/reade/Documents/10xDev%20Project/context/changes/public-recipe-matching12345/reviews/plan-review.md)
- RecipeService: [RecipeService.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Services/RecipeService.cs)
- Dashboard component: [dashboard.component.ts](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/components/dashboard/dashboard.component.ts)
- Existing backend tests: [RecipeServiceTests.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Tests/RecipeServiceTests.cs)
- Existing frontend test pattern: [auth.guard.spec.ts](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/guards/auth.guard.spec.ts)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Database-Level Ingredient Containment Filter

#### Automated

- [x] 1.1 Backend builds cleanly: `dotnet build`
- [x] 1.2 Existing unit tests pass: `dotnet test`

#### Manual

- [x] 1.3 Recipe matching returns same results as before

### Phase 2: Dashboard Component Unit Tests

#### Automated

- [ ] 2.1 Frontend builds cleanly: `npm run build`
- [ ] 2.2 New unit tests pass: `npm run test -- --watch=false`

#### Manual

- [ ] 2.3 Test output shows all dashboard specs passing

### Phase 3: Case-Insensitive Tag Deduplication Fix

#### Automated

- [ ] 3.1 Frontend builds cleanly: `npm run build`
- [ ] 3.2 All unit tests pass including dedup test: `npm run test -- --watch=false`

#### Manual

- [ ] 3.3 Duplicate ingredient with different casing is rejected in the app
