<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Public Recipe Matching — Post-Review Fixes

- **Plan**: context/changes/public-recipe-matching12345/plan.md
- **Scope**: All Phases (1 to 3)
- **Date**: 2026-05-26
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical | 3 warnings | 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS ✅ |
| Scope Discipline | PASS ✅ |
| Safety & Quality | WARNING ⚠️ |
| Architecture | WARNING ⚠️ |
| Pattern Consistency | PASS ✅ |
| Success Criteria | PASS ✅ |

## Findings

### F1 — In-Memory Eager Loading of Filtered Set Scales Poorly

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Safety & Quality
- **Location**: backend/Services/RecipeService.cs:37-42
- **Detail**: The DB pre-filtering query fetches all public recipes containing ANY matched ingredient IDs into memory before calculating matchRate and performing top-20 pagination. If a user queries a common ingredient (e.g. salt), the application eager-loads almost the entire database graph into C# memory, creating massive GC pressure and potential query timeouts.
- **Fix A ⭐ Recommended**: Cap the DB fetch count or only load recipes matching at least one primary (non-spice) ingredient before performing weighted sorting.
  - Strength: Grounded in existing model data where spices have very low weights (0.1) and don't meaningfully drive matching.
  - Tradeoff: Slight increase in query logic complexity.
  - Confidence: HIGH — standard optimization for recipe search.
  - Blind spot: None significant.
- **Decision**: Fixed via Fix A

### F2 — Ignored User Private Recipes in Match Service

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Architecture
- **Location**: backend/Services/RecipeService.cs:39
- **Detail**: The matching algorithm only queries public recipes (`r.IsPublic`). However, the MVP specification requires private recipes to be searchable/viewable only by their owner. Users cannot match recipes they manually created as private, violating functional scope.
- **Fix**: Accept an optional `Guid? userId` in `MatchRecipesAsync` and match on `r.IsPublic || r.UserId == userId`.
  - Strength: Fully completes the MVP scope for personal private recipes.
  - Tradeoff: Increases parameters in backend service method.
  - Confidence: HIGH — extremely easy SQL adjustment.
  - Blind spot: Requires updating call sites to pass the active user ID.
- **Decision**: SKIPPED

### F3 — Unhandled RxJS Subscriptions in DashboardComponent

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: frontend/src/app/components/dashboard/dashboard.component.ts
- **Detail**: The component subscribes to `recipeService.getIngredients()` and `recipeService.matchRecipes()` without cleanup. If the component is destroyed (e.g. logout or navigation) while a request is pending, it can cause memory leaks or unexpected UI updates on a destroyed component instance.
- **Fix**: Implement `OnDestroy` and use a `takeUntil(this.destroy$)` RxJS operator pattern to auto-unsubscribe.
  - Strength: Eliminates all potential memory leaks on navigation.
  - Tradeoff: Minimal boilerplate addition.
  - Confidence: HIGH — standard Angular performance best practice.
  - Blind spot: None significant.
- **Decision**: Fixed via Fix

### F4 — Case-Insensitive Full Table Scan on Ingredients Name

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: backend/Services/RecipeService.cs:30
- **Detail**: Querying `i.Name.ToLower()` forces RDBMS to apply `LOWER()` function to each row, bypassing any database index on the Name column and causing a full table scan. Since default database collations are already case-insensitive, `.ToLower()` is redundant.
- **Fix**: Remove `.ToLower()` and rely on native database case-insensitive collation for `normalizedUserIngredients.Contains(i.Name)`.
- **Decision**: SKIPPED

### F5 — Fragile setTimeout in Input Blur Handler

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: frontend/src/app/components/dashboard/dashboard.component.ts:104-109
- **Detail**: The component uses a hardcoded 200ms `setTimeout` in `onInputBlur` to defer closing the autocomplete dropdown. If the component is destroyed during this window, the callback will execute on a destroyed instance.
- **Fix**: Capture the timeout handle and clear it inside the `ngOnDestroy` lifecycle hook.
- **Decision**: SKIPPED
