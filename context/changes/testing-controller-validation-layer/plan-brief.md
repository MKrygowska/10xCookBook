# Controller & Validation Layer Testing — Plan Brief

> Full plan: `context/changes/testing-controller-validation-layer/plan.md`
> Research: `context/changes/testing-controller-validation-layer/research.md`

## What & Why

We need to implement validation checks on recipe request payloads to prevent DB exceptions from bubbling up to clients and exposing internal database details. We also need to guarantee that the frontend authentication guards protect private pages (`/dashboard`, `/my-recipes`, `/settings`) by adding comprehensive tests.

## Starting Point

Today, the API's DTO request models lack string length limits that match database constraints, resulting in unhandled database-level constraint exceptions when malformed requests are sent. The frontend's `auth.guard.spec.ts` verifies mock authentication status, but there are no tests asserting that the guard is actually registered on the routes.

## Desired End State

All entry points to the application validate request limits. Invalid payload structures receive HTTP 400 Bad Request responses with standard validation error details. Integration tests run automatically to assert validation rules, and the frontend router contains test assertions checking that every private page requires the auth guard.

## Key Decisions Made

| Decision                       | Choice            | Why (1 sentence)  | Source           |
| ------------------------------ | ----------------- | ----------------- | ---------------- |
| Validation format              | RFC 7807 Standard | Standardize on default ASP.NET Core `ValidationProblemDetails` and clean up unreachable validation code. | Research |
| Exception leaking              | Sanitize catches  | Wrap entity writes to catch database errors and return localized error messages instead of leaking `ex.Message`. | Plan |
| Guard tests scope              | Router Config Test| Adding a static configuration test ensures that future routing changes do not accidentally expose private pages. | Research |

## Scope

**In scope:**
- Adding `[MaxLength]` and `[MinLength]` attributes to recipe DTO properties.
- Removing dead validation checks from `AuthController.cs`.
- Sanitizing controller catches in `RecipesController.cs`.
- Integration tests in `backend.Tests` checking validation results.
- Unit and routing guard tests in `frontend/`.

**Out of scope:**
- Implementing actual token refresh logic or advanced JWT decoding libraries.
- Modifying UI components or styles.

## Architecture / Approach

Validation runs automatically in ASP.NET Core controllers via the global `[ApiController]` filter. Integration tests will spin up a local server using `WebApplicationFactory<Program>` to make real HTTP POST/PUT requests and verify return codes. Angular route tests will inspect the app's routing configuration statically.

## Phases at a Glance

| Phase     | What it delivers       | Key risk                  |
| --------- | ---------------------- | ------------------------- |
| 1. Backend Validation | Adds DTO validation annotations and cleans up dead/leaky controller code. | Incomplete validation coverage. |
| 2. Integration Tests  | Integration tests verifying validation limits return 400 errors. | Mismatch between tests and API. |
| 3. Frontend Guards    | Router configuration protection tests and guard tests. | Private routes accidentally exposed. |

**Prerequisites:** None
**Estimated effort:** ~1 session across 3 phases

## Open Risks & Assumptions

- **EF InMemory Provider Limitations**: EF Core's InMemory DB does not enforce database constraints (like MaxLength or unique indices) in the same way SQL Server does. Therefore, validating model state *before* it hits the DB (using DTO validations) is crucial for safety.

## Success Criteria (Summary)

- API requests with invalid properties consistently return HTTP 400 Bad Request.
- `dotnet test` executes and passes in `backend.Tests`.
- `npm run test` executes and passes in `frontend/`.
- Private pages redirect to login if accessed without a valid session.
