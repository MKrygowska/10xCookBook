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

## 10xDevs AI Toolkit - Module 2, Lesson 3

Review AI-generated code before merge with the **implementation review chain**:

```
/10x-implement -> /10x-impl-review -> triage -> (/10x-lesson | fix | skip | disagree)
```

`/10x-impl-review` is the lesson focus. Review is a quality gate, not an instruction to fix every finding.

### Task Router - Where to start

| Skill | Use it when |
| --- | --- |
| **Code review (lesson focus)** | |
| `/10x-impl-review <change-id>` | You have implemented code and want a structured review before merge. The skill checks plan adherence, scope discipline, safety and quality, architecture, pattern consistency, and success criteria, then presents findings for triage. |
| **Recurring lesson outcome** | |
| `/10x-lesson` | A finding reveals a recurring project rule or agent failure pattern. Record it in `context/foundation/lessons.md` instead of treating it as a one-off note. |

### Triage discipline

- Severity says how bad the finding is. Impact says how much the decision matters now.
- Valid outcomes: fix now, fix differently, skip, accept as risk, record as recurring rule (`/10x-lesson`), disagree.
- Fix critical findings. Do not burn hours on low-impact observations just because the agent found them.
- Conscious skipping of low-impact findings is a valid review outcome, not negligence.
- If you disagree with a finding, record why. Wrong agent reasoning is also signal.

### Review boundaries

- This lesson reviews implemented code. It does not create the plan, execute new phases, or teach CI review.
- Testing strategy and quality gates are introduced in Module 3.
- Do not use `/10x-contract` as a triage outcome in this lesson.

### Paths used by this lesson

- `context/changes/<change-id>/plan.md` - expected implementation contract
- `context/changes/<change-id>/reviews/` - review output
- `context/foundation/lessons.md` - recurring lessons

Skills must not write to `context/archive/`. Archived changes are immutable; if a resolved target path starts with `context/archive/`, abort with: "This change is archived. Open a new change with `/10x-new` instead."

<!-- END @przeprogramowani/10x-cli -->
