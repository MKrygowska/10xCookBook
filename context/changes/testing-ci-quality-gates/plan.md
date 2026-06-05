# Quality Gates Wiring in CI/CD Implementation Plan

Lock formatting, typechecking, build verification, and unit tests in the CI/CD pipeline.

## Proposed Changes

### CI/CD Workflows

#### [MODIFY] [deploy-backend.yml](file:///c:/Users/reade/Documents/10xDev%20Project/.github/workflows/deploy-backend.yml)
- Update triggers to run on pushes and pull requests to `main` for `backend/**` and `backend.Tests/**`.
- Add `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build`, and `dotnet test`.
- Add conditional checks so publishing and deployment only run on push to `main`.

#### [MODIFY] [deploy-frontend.yml](file:///c:/Users/reade/Documents/10xDev%20Project/.github/workflows/deploy-frontend.yml)
- Add setup steps for Node.js (v20.x) with npm package caching.
- Add `npm ci`, typecheck (`npx tsc --noEmit -p tsconfig.app.json`), and unit tests (`npm run test -- --watch=false --browsers=ChromeHeadless`).
- Run checks prior to deployment.

---

## Verification Plan

### Automated Tests
- Verification of local format validation, C# tests, and frontend typecheck/tests before commit.

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Backend Quality Gates

#### Automated
- [x] 1.1 Backend workflow updated with format, build, and test gates — c3d9691

### Phase 2: Frontend Quality Gates

#### Automated
- [x] 2.1 Frontend workflow updated with restore, typecheck, and test gates — c3d9691

### Phase 3: Rollout Sync & Verification

#### Automated
- [x] 3.1 Test plan marked complete in test-plan.md — b3bb67f
