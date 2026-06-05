# Controller & Validation Layer Testing Implementation Plan

## Overview
Implement request model validation, DB exception cleaning, integration tests for model validations via `WebApplicationFactory`, and unit tests/route protection configurations for Angular auth routing guards (Phase 2 test rollout).

## Current State Analysis
- Model validation attributes are missing on key DTO fields (e.g. `Title` has no MaxLength, `Quantity` has no MaxLength), causing DB-level constraint failures.
- Controllers catch DB-level errors and leak raw exception details.
- `AuthController.cs` has dead validation code because automatic filter returns RFC 7807 problem details first.
- Frontend auth guard does not verify JWT expiration and there are no tests ensuring the guard is configured on the router.

## Desired End State
- All recipe DTO models validate string length boundaries at the API layer, returning HTTP 400 with a clear error payload.
- Unreachable validation code in `AuthController.cs` is cleaned up.
- Backend API does not expose raw database implementation errors (like SQL or EF Core constraint logs) to clients.
- WebApplicationFactory integration tests verify model validation limits.
- Frontend routing guard is tested for both authenticated and unauthenticated states.
- Frontend routing configuration test asserts that `/dashboard`, `/my-recipes`, and `/settings` require authentication.

## What We're NOT Doing
- Implementing authentication hooks, token refresh lifecycle, or complex database transaction rollbacks (re-seeding DB between integration tests is sufficient).
- Styling changes, visual updates, or testing non-auth routing states.

## Implementation Approach
1. Update DTO properties in `RecipeDtos.cs` with length and format validation annotations.
2. Clean up duplicate dead-code checks in `AuthController.cs`.
3. Wrap/sanitize catches in `RecipesController.cs` to return user-friendly strings.
4. Create a new xUnit integration test suite `ValidationIntegrationTests.cs` using `WebApplicationFactory<Program>` in `backend.Tests`.
5. Enhance `auth.guard.spec.ts` with route protection check assertions and expiration simulation scenarios.

---

## Phase 1: Backend Validation & Error Cleanup
### Overview
Add missing DTO validation annotations, remove dead code validation blocks, and clean up database exception leakage in controllers.

### Changes Required:

#### 1. DTO Annotations
**File**: `backend/DTOs/RecipeDtos.cs`
**Intent**: Enforce MaxLength limits corresponding to database columns to prevent database-level crashes.
**Contract**:
- `CreateRecipeRequest.Title` & `UpdateRecipeRequest.Title`: Add `[MaxLength(200, ErrorMessage = "Tytuł nie może przekraczać 200 znaków.")]`.
- `RecipeIngredientRequest.Quantity`: Add `[MaxLength(100, ErrorMessage = "Ilość nie może przekraczać 100 znaków.")]` and `[MinLength(1, ErrorMessage = "Ilość nie może być pusta.")]` or verify that empty strings are blocked.

#### 2. AuthController Cleanup
**File**: `backend/Controllers/AuthController.cs`
**Intent**: Remove dead-code manual checks of `ModelState.IsValid` as the `[ApiController]` attribute automatic action filter handles validation failures first.
**Contract**: Remove `if (!ModelState.IsValid)` checking from `Register` and `Login`.

#### 3. Exception Sanitization
**File**: `backend/Controllers/RecipesController.cs`
**Intent**: Catch database exceptions and map them to clean error objects rather than returning raw `ex.Message`.
**Contract**: Update catch blocks in `CreateRecipe` and `UpdateRecipe` to return standard messages.

### Success Criteria:
#### Automated Verification:
- Backend compiles cleanly: `dotnet build`

---

## Phase 2: Integration Tests for Validation
### Overview
Write automated integration tests executing API requests against a local test server to verify validation behavior.

### Changes Required:

#### 1. Integration Tests
**File**: `backend.Tests/ValidationIntegrationTests.cs` [NEW]
**Intent**: Test endpoint validations with malformed payloads using `WebApplicationFactory<Program>`.
**Contract**:
- Create `ValidationIntegrationTests` inheriting from standard testing infrastructure.
- Assert that sending too long a title to `POST /api/recipes` returns HTTP 400.
- Assert that sending too long a quantity or empty quantity to `POST /api/recipes` returns HTTP 400.
- Assert that sending invalid email formats to `POST /api/auth/register` returns HTTP 400.

### Success Criteria:
#### Automated Verification:
- Run backend tests: `dotnet test` (inside `backend.Tests/` folder)

---

## Phase 3: Frontend Route Protection & Guard Tests
### Overview
Add route configuration tests to verify `/dashboard`, `/my-recipes`, and `/settings` are locked behind `authGuard`. Add client-side token expiration tests.

### Changes Required:

#### 1. Router Guards Tests
**File**: `frontend/src/app/guards/auth.guard.spec.ts`
**Intent**: Assert that routing guards are configured for private pages and redirect on missing or expired tokens.
**Contract**:
- Import `routes` from `app.routes`.
- Add route protection tests verifying that `canActivate` includes `authGuard` for paths `dashboard`, `my-recipes`, and `settings`.
- Add test checking that unauthenticated states redirect to `/login`.

### Success Criteria:
#### Automated Verification:
- Run frontend tests: `npm run test` (inside `frontend/` folder)

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Backend Validation & Error Cleanup

#### Automated
- [x] 1.1 Backend compiles cleanly: `dotnet build` — 0a1a0d3

### Phase 2: Integration Tests for Validation

#### Automated
- [x] 2.1 Backend tests run and pass: `dotnet test`

### Phase 3: Frontend Route Protection & Guard Tests

#### Automated
- [ ] 3.1 Frontend tests run and pass: `npm run test`
