---
date: 2026-06-05T11:08:00+02:00
researcher: Antigravity
git_commit: 643d35f92791b96ef588c875d3bfd3747046cdb2
branch: feat/testing-controller-validation
repository: MKrygowska/10xCookBook
topic: "Controller & validation layer tests"
tags: [research, codebase, validation, routing-guards, controller-testing]
status: complete
last_updated: 2026-06-05
last_updated_by: Antigravity
---

# Research: Controller & validation layer tests

**Date**: 2026-06-05T11:08:00+02:00
**Researcher**: Antigravity
**Git Commit**: 643d35f92791b96ef588c875d3bfd3747046cdb2
**Branch**: feat/testing-controller-validation
**Repository**: MKrygowska/10xCookBook

## Research Question

Conduct research for Phase 2 of the test rollout (Controller & validation layer) covering Risk #3 (API input validation bypass) and Risk #4 (Angular routing authentication guard configuration).

## Summary

This research investigates:
1. **Risk #3 (Controller & Validation Layer)**: Map request DTO validation annotations in the backend C# API. Check if `ModelState.IsValid` is checked in controllers, how ASP.NET Core automatic validation acts via `[ApiController]`, how database-level exceptions are handled, and how to write HTTP integration tests using `Microsoft.AspNetCore.Mvc.Testing` and `WebApplicationFactory`.
2. **Risk #4 (Angular Auth Guard)**: Detail how the frontend functional routing `authGuard` verifies authentication status, how `auth.guard.spec.ts` mocks this behavior, and how frontend authentication state is tracked in `AuthService`. We identify critical validation gaps (such as lack of expiration checks on JWT tokens) and recommend the cheapest high-signal test strategies.

Key Gaps Identified:
- **Missing validation constraints** on recipe request DTOs (`Title` MaxLength 200, `Quantity` MaxLength 100) causing database exceptions (`DbUpdateException`) to bubble up.
- **Leaked DB exceptions** in controllers returning raw exception details (e.g. `BadRequest(new { error = ex.Message })`) instead of standard structured validation responses.
- **Unreachable manual validation code** in `AuthController` due to ASP.NET Core's global automatic validation filter intercepting request errors.
- **No client-side JWT expiration validation** in frontend `AuthService`.
- **Untested routing configuration** in frontend where a route could be exposed by accidentally removing the `canActivate: [authGuard]` array attribute.

---

## Detailed Findings

### 1. Backend DTO Validation Mapping (`backend/DTOs/`)

The following validation attributes are present in the backend request DTOs:
- **`AuthDtos.cs`**:
  - `RegisterRequest`: `Email` is annotated with `[Required]` and `[EmailAddress]`; `Password` is annotated with `[Required]` and `[MinLength(6)]`.
  - `LoginRequest`: `Email` is annotated with `[Required]`; `Password` is annotated with `[Required]`.
- **`RecipeDtos.cs`**:
  - `MatchRecipesRequest`: `Ingredients` is annotated with `[Required]`.
  - `RecipeIngredientRequest`: `Quantity` is annotated with `[Required]`; `IngredientId` has no validation attributes.
  - `CreateRecipeRequest` and `UpdateRecipeRequest`: `Title` and `Instructions` are annotated with `[Required]`. However, `Title` does not specify `[MaxLength(200)]` which is constrained at the DB layer in `Recipe.cs`. `RecipeIngredientRequest` does not specify `[MaxLength(100)]` for `Quantity` which is constrained at the DB layer in `RecipeIngredient.cs`.

### 2. ASP.NET Core Controller Validation & Dead Code
- `BaseApiController` utilizes the `[ApiController]` attribute (line 7). Consequently, the global MVC filter automatically parses model state violations and returns a `ValidationProblemDetails` object (complying with RFC 7807) with HTTP Status 400.
- `AuthController.cs` checks `if (!ModelState.IsValid)` manually in `Register` and `Login`. Because the automatic model state validator intercepts requests before the action execution, this manual check is unreachable dead code.

### 3. Frontend Auth Guard and AuthService
- **`authGuard`** ([auth.guard.ts:L5-18](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/guards/auth.guard.ts#L5-L18)) injects `AuthService` and `Router` and allows access if `authService.isAuthenticated()` returns true; otherwise it navigates to `/login` and returns false.
- **`auth.guard.spec.ts`** ([auth.guard.spec.ts:L1-37](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/guards/auth.guard.spec.ts#L1-L37)) mocks `isAuthenticated` to return `true` or `false` using Jasmine spies and validates routing response.
- **`AuthService`** ([auth.service.ts:L43-45](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/services/auth.service.ts#L43-L45)) evaluates authentication purely by checking if `auth_token` exists in `localStorage`. There is no expiration decoding or verification of the JWT structure.

---

## Code References

### Backend
- [AuthDtos.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/DTOs/AuthDtos.cs) - Definition of register and login request models.
- [RecipeDtos.cs](file:///c:/Users/reade/Documents/10xDev%20Project/backend/DTOs/RecipeDtos.cs) - Definition of recipe create/update and matching request models.
- [AuthController.cs:L21-25](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/AuthController.cs#L21-L25) - Manual `ModelState.IsValid` dead code.
- [RecipesController.cs:L71-74](file:///c:/Users/reade/Documents/10xDev%20Project/backend/Controllers/RecipesController.cs#L71-L74) - Try-catch exposing raw DB exceptions to the API user.

### Frontend
- [auth.guard.ts](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/guards/auth.guard.ts) - Authentication guard implementation.
- [auth.guard.spec.ts](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/guards/auth.guard.spec.ts) - Unit tests for the authentication guard.
- [auth.service.ts](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/src/app/services/auth.service.ts) - Frontend Auth service storing token in localStorage.

---

## Architecture Insights

1. **Automatic HTTP 400 Bad Request Filters**:
   - The `[ApiController]` automatic filter returning standard `ValidationProblemDetails` is the canonical ASP.NET Core pattern. The duplicate manual `ModelState.IsValid` checks in `AuthController` should be removed to clean up code.
2. **Integration Testing using WebApplicationFactory**:
   - We can verify model validations by extending integration tests inside `backend.Tests`. Setting up tests with `WebApplicationFactory<Program>` allows us to send requests with invalid payload structures and assert that the server returns HTTP 400.
3. **Frontend Route Protection Testing**:
   - The unit tests for `authGuard` prove the guard's logic but do not verify if the guard is actually configured on the routing list. We should add a static configuration verification test to `auth.guard.spec.ts` asserting that `/dashboard`, `/my-recipes`, and `/settings` are protected.

---

## Historical Context (from prior changes)

- **`context/archive/2026-06-04-testing-critical-path-coverage/`**:
  - Established the `backend.Tests` project structure to isolate tests from production code.
  - Demonstrated how to mock the database provider with InMemory DB and mock JWT tokens using a development fallback secret.

---

## Related Research

- No other active research artifacts.

---

## Open Questions

1. **How should we handle database-level exception leaks?**
   - Do we want to catch exception details inside the service layer or keep returning standard bad request payloads from the controller without exposing DB implementation strings?
2. **Do we need JWT expiration checking in the frontend auth guard?**
   - If the backend returns `401 Unauthorized` for expired JWTs, do we handle this gracefully via an interceptor?

