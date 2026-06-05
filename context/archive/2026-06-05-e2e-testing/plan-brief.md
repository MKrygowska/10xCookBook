# E2E Testing Setup — Plan Brief

> Full plan: `context/changes/e2e-testing/plan.md`

## What & Why
We want to add a robust browser-level E2E testing framework to protect key user journeys in 10xCookBook. Unit tests cover logic and controllers, but they cannot prove that the Angular client integrates properly with the ASP.NET Core API, that authentication flows survive routing guards, or that DOM-level interaction and styling behave correctly under user search conditions.

## Starting Point
Currently, there is no E2E framework in the workspace. The client project (Angular 17 in `frontend/`) has unit tests configured via Karma and Jasmine, and the backend has integration tests. Local development servers run on `http://localhost:4200` (frontend) and `http://localhost:5174` (backend).

## Desired End State
Playwright is installed in the `frontend` subfolder. We have three E2E specs: Recipe Creation (exemplar seed test), Auth Guard redirect verification, and Recipe Search verification. These tests run automatically during pull request checks on GitHub Actions before deployment.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Playwright Location | Inside the frontend/ subfolder | Keeps client-centric UI testing colocated with the Angular code. | Plan |
| Dev Server Lifecycle | Playwright's webServer config | Automates starting both API and frontend servers during runs. | Plan |
| Auth Strategy | storageState setup project | Caches authenticated state to keep E2E runs fast. | Plan |
| Database Choice | SQLite/Development DB | Reuses the existing dev DB to avoid complex orchestration delta. | Plan |

## Scope
**In scope:**
- Playwright installation and non-interactive setup in `frontend/`.
- Exemplar seed E2E test for Recipe Creation.
- Auth Guard Redirect test (Risk #4).
- Recipe Search results and styling test (Risk #1).
- GitHub Actions workflow integration.

**Out of scope:**
- Visual regression (pixel-perfect) screenshot matching.
- Testing external third-party integrations (all tests run locally).

## Architecture / Approach
Playwright runs headless browsers driven by user-facing accessibility tree locators. A global setup project logs in once and caches cookie states. The configuration spawns the ASP.NET API and Angular servers concurrently using multiple `webServer` entries.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Setup & Levers | Playwright configured with auth state cache and `seed.spec.ts` (Creation). | Flaky setup and brittle locators. |
| 2. Auth Guard Spec | `auth-guard.spec.ts` redirecting unauthenticated visits. | Security bypass on dashboard. |
| 3. Recipe Search Spec | `recipe-search.spec.ts` asserting match results and styling. | Wrong ingredients shown. |
| 4. CI/CD Pipeline | Playwright execution in GitHub Actions check. | Regression code deployed. |

**Prerequisites:** Node.js and .NET SDK installed.
**Estimated effort:** ~1-2 sessions.

## Open Risks & Assumptions
- We assume SQLite data is seedable and resets consistently.
- Playwright browser downloads are supported in the user's terminal.
