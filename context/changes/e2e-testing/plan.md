# E2E Testing Setup and Core Flows Implementation Plan

## Overview

Configure Playwright E2E testing in the Angular frontend, establish E2E quality levers (seed test and quality rules), implement E2E tests for the Auth Guard redirect and Recipe Search flows, and integrate E2E tests into the GitHub Actions pipeline.

## Current State Analysis

- **Frontend Unit Tests**: Currently configured with Karma and Jasmine. No E2E framework exists.
- **Backend API**: ASP.NET Core C# API, compiles cleanly, has unit/integration tests (xUnit).
- **Database**: InMemory database is used for tests, SQLite/Azure SQL is used for local run/production.
- **CI/CD Pipeline**: GitHub Actions workflows are present for frontend and backend deployments, but they do not run any E2E tests.

## Desired End State

- Playwright is configured in `frontend/` with a non-interactive setup.
- E2E quality rules are documented in `AGENTS.md` and a working exemplar E2E test `seed.spec.ts` is in place covering Recipe Creation.
- E2E tests verify:
  1. **Recipe Creation Flow** (`seed.spec.ts`): Logs in, navigates to private recipes, creates a recipe, verifies it exists in the list, deletes it.
  2. **Auth Guard Redirect** (`auth-guard.spec.ts`): Attempts to navigate to `/dashboard` directly without authentication, verifying it redirects to `/login`.
  3. **Recipe Search Flow** (`recipe-search.spec.ts`): Logs in, enters search ingredients, submits, verifies matching recipe in results, verifies correct styling of owned vs missing ingredients.
- All E2E tests run on pull requests in the CI/CD pipeline.

### Key Discoveries:
- Backend runs on `http://localhost:5174` (profile "http").
- Frontend runs on `http://localhost:4200` via `ng serve`.

## What We're NOT Doing
- We are not writing visual regression tests (pixel diffs) or using vision models.
- We are not mocking the C# backend API; E2E tests run against the real local running backend and database.
- We are not rewriting any application logic or styling.

## Implementation Approach
We will install and configure Playwright inside `frontend/` using its non-interactive CLI. We will configure Playwright's `webServer` block to automatically start both the backend API and frontend dev server during test runs. We will use a setup project to handle authentication once and cache the `storageState` for subsequent tests to keep execution times fast.

---

## Phase 1: Playwright Setup & Levers

### Overview
Install Playwright non-interactively in the `frontend` subfolder, write the Playwright configuration to start backend and frontend dev servers, and establish the quality levers.

### Changes Required:

#### 1. Playwright Installation & Package Config
- **File**: [frontend/package.json](file:///c:/Users/reade/Documents/10xDev%20Project/frontend/package.json)
- **Intent**: Add Playwright test dependencies to the frontend subproject.
- **Contract**: Execute `npm init playwright@latest -- --quiet --lang=TypeScript --no-gha` inside `frontend/`.

#### 2. Playwright Configuration
- **File**: `frontend/playwright.config.ts`
- **Intent**: Configure Playwright to use `localhost:4200` as baseURL, and start both the backend and frontend dev servers automatically.
- **Contract**: Configure a setup project for authentication and two `webServer` objects in the configuration:
  ```typescript
  webServer: [
    {
      command: 'cd ../backend && dotnet run --launch-profile http',
      url: 'http://localhost:5174/swagger/index.html',
      reuseExistingServer: !process.env.CI,
    },
    {
      command: 'npm run start',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
    }
  ]
  ```

#### 3. Authentication Setup Spec
- **File**: `frontend/tests/auth.setup.ts`
- **Intent**: Perform login once via the API or UI to save the authenticated state.
- **Contract**: Log in with a test user, retrieve the token/cookie, and write it to `playwright/.auth/user.json`.

#### 4. Exemplar Seed Test (Recipe Creation)
- **File**: `frontend/tests/seed.spec.ts`
- **Intent**: Implement the exemplar test representing the Recipe Creation flow to serve as a guideline for other E2E tests.
- **Contract**: Log in, navigate to private recipes, fill out the form to create a unique recipe (using `Date.now()`), verify its presence in the UI, and delete it as cleanup.

#### 5. E2E Quality Rules
- **File**: [AGENTS.md](file:///c:/Users/reade/Documents/10xDev%20Project/AGENTS.md)
- **Intent**: Document E2E quality guidelines so future agent runs strictly follow them.
- **Contract**: Ensure the E2E quality rules block exists in the active course rules section.

### Success Criteria:

#### Automated Verification:
- Playwright installs and dev servers start successfully.
- The `seed.spec.ts` runs and passes successfully.

#### Manual Verification:
- Verify that a recipe is correctly created and cleaned up in the SQLite database during the run.

---

## Phase 2: Auth Guard Redirect E2E Test

### Overview
Implement an E2E test verifying that accessing protected routes without active session credentials redirects to `/login`.

### Changes Required:

#### 1. Auth Guard Redirect Spec
- **File**: `frontend/tests/auth-guard.spec.ts`
- **Intent**: Verify Auth Guard protection on private pages.
- **Contract**: Without referencing `storageState` (bypassing auth), navigate to `/dashboard`, `/my-recipes`, and `/settings` and assert that the URL changes to `/login`.

### Success Criteria:

#### Automated Verification:
- Run `npx playwright test auth-guard.spec.ts` and verify it passes.

#### Manual Verification:
- Confirm that navigating manually in the browser in Incognito mode redirects from `/dashboard` to `/login`.

---

## Phase 3: Recipe Search E2E Test

### Overview
Implement an E2E test verifying the core search functionality—searching by ingredients and verifying matching results and correct styling.

### Changes Required:

#### 1. Recipe Search Spec
- **File**: `frontend/tests/recipe-search.spec.ts`
- **Intent**: Verify matching results display correctly when a user searches by ingredients.
- **Contract**: Authenticate via cached state, type ingredients in the search input, submit, verify matching recipes are returned, and assert that owned ingredients are styled as available and missing ones as unavailable.

### Success Criteria:

#### Automated Verification:
- Run `npx playwright test recipe-search.spec.ts` and verify it passes.

#### Manual Verification:
- Confirm recipe match lists display correctly in the browser when performing a search.

---

## Phase 4: CI/CD Integration

### Overview
Configure the GitHub Actions workflow to execute the E2E test suite upon pull requests.

### Changes Required:

#### 1. Frontend CI/CD Workflow Update
- **File**: [.github/workflows/deploy-frontend.yml](file:///c:/Users/reade/Documents/10xDev%20Project/.github/workflows/deploy-frontend.yml)
- **Intent**: Add an E2E execution step to the frontend CI check.
- **Contract**: Before the deploy steps, install Playwright dependencies, and run `npx playwright test` inside `frontend/`.

### Success Criteria:

#### Automated Verification:
- Verify GitHub Actions configuration parsing succeeds.

#### Manual Verification:
- None.

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Playwright Setup & Levers

#### Automated
- [x] 1.1 Install Playwright and configure `playwright.config.ts`
- [x] 1.2 Implement `auth.setup.ts` and `seed.spec.ts` (Recipe Creation)
- [x] 1.3 Update `AGENTS.md` with E2E quality rules

#### Manual
- [x] 1.4 Verify recipe creation and database cleanup works during E2E run

### Phase 2: Auth Guard Redirect E2E Test

#### Automated
- [x] 2.1 Implement `auth-guard.spec.ts` and verify redirect to `/login` passes

#### Manual
- [x] 2.2 Manually verify incognito redirect behavior in browser

### Phase 3: Recipe Search E2E Test

#### Automated
- [x] 3.1 Implement `recipe-search.spec.ts` and verify match list and styling assertions pass

#### Manual
- [x] 3.2 Verify recipe search matches user-facing expectations in UI

### Phase 4: CI/CD Integration

#### Automated
- [x] 4.1 Update `.github/workflows/deploy-frontend.yml` with Playwright install and test execution steps
