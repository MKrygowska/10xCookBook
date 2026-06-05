# Repository Guidelines

This repository contains 10xCookBook, an ingredient-based recipe search and CRUD application consisting of an Angular 17 SPA frontend and an ASP.NET Core C# API backend. Find product scope details in `@10xCookBook - MVP.txt`.

### Hard Rules / Agent-Specific Instructions
- Isolate all changes strictly within `frontend/` or `backend/` subfolders. Never write code or configurations directly to the repository root.
- Do not write multi-line code or configuration blocks in rule files. Reference canonical paths instead.
- Always execute a local compilation check (`dotnet build` or `npm run build`) before declaring an implementation complete.

### Project Structure & Module Organization
- `frontend/` — Angular SPA client. Sourced in `frontend/src/`. Configuration is detailed in `@frontend/package.json`.
- `backend/` — ASP.NET Core C# Web API. Namespace is `10x_cookbook_backend`. Project configuration lives at `@backend/10x-cookbook-backend.csproj` and `@backend/Program.cs`.

### Build, Test, and Development Commands
Execute the following scripts from their respective subfolders:
- Frontend dev server: `npm run start` (inside `frontend/`)
- Frontend production build: `npm run build` (inside `frontend/`)
- Frontend unit tests: `npm run test` (inside `frontend/`)
- Backend Web API dev server: `dotnet run` (inside `backend/`)
- Backend build and compile check: `dotnet build` (inside `backend/`)

### Coding Style & Naming Conventions
- Angular: Use camelCase for methods/variables, kebab-case for filenames (`*.component.ts`), and enforce strict typing.
- C#/.NET: Use PascalCase for filenames, classes, namespaces, and methods. Nullable checks and implicit usings are enabled globally.

### Testing Guidelines
- Frontend: Jasmine/Karma unit tests live adjacent to components using the `*.spec.ts` pattern.
- Backend: Endpoint integrations should be verified using the local API file at `@backend/10x-cookbook-backend.http`.

### Commit & Repository Management
- **Branching Strategy:** Never commit or work directly on `main` for active tasks. Always create a dedicated branch from `main` named `<type>/<id>-<slug>` (e.g., `feat/F-03-deployment-ci-cd` or `feat/S-01-public-recipe-matching`) before starting `10x-plan` or `10x-implement`.
- **Merge & Archive Flow:** Only merge the task branch back to `main` after completing all plan phases and verifying both automated and manual tests. Run `/10x-archive` to archive the change folder immediately after the merge.

<!-- BEGIN @przeprogramowani/10x-cli -->

## 10xDevs AI Toolkit - Module 3, Lesson 4 (E2E Tests)

**For E2E tests, use the `/10x-e2e` skill.** It is the single source of truth
for the workflow — risk → seed test + rules → generate → review against the five
anti-patterns → re-prompt → verify. The skill's `references/` carry the full
rules, anti-patterns, seed pattern, and prompt-template.

A few hard rules that hold even before you invoke the skill:

- **Locators:** `getByRole` / `getByLabel` / `getByText` first; `getByTestId`
  only when accessibility attributes are ambiguous. Never CSS selectors, XPath,
  or DOM structure.
- **Never `page.waitForTimeout()`.** Wait for state: `toBeVisible()`,
  `waitForURL()`, `waitForResponse()`.
- **Test independence + cleanup.** Each test runs standalone — its own setup,
  action, assertion, and cleanup; unique ids (timestamp suffix) so parallel runs
  and re-runs don't collide.

Two boundaries to keep straight:

- **DOM (snapshot) is the default.** Vision (`--caps=vision`) is a supplement for
  visual-only risks (layout, z-index, animation); for pixel regression prefer
  deterministic tools (`toMatchSnapshot`, Argos, Lost Pixel). VLM model
  selection/cost is a debugging topic (Lesson 5), not testing.
- **Healer helps on selectors, harms on logic.** A changed selector → healer
  re-finds it (route through PR review). A changed business behavior → healer
  masks the bug; that failing-test-to-fix case is Lesson 5.

<!-- END @przeprogramowani/10x-cli -->
