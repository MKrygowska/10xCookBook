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

## 10xDevs AI Toolkit - Module 2, Lesson 5

Scale the single-change cycle into parallel work with **worktrees, goal-directed delegation, and multi-session orchestration**:

```
worktree per change -> /goal or your AI coding assistant -p -> PR -> review -> merge
```

The lesson focus is safe throughput: isolated contexts, choosing the right execution mode, and capping parallelism at review capacity.

### Task Router - Where to start

| Skill | Use it when |
| --- | --- |
| **Code isolation** | |
| `git worktree add` | You need a separate working directory for a parallel change. One change per worktree, one fresh agent context per worktree. |
| **Complex changes** | |
| `/10x-implement <change-id> phase <n>` | The change has multiple phases, needs manual gates, or benefits from interactive decision-making during execution. |
| **Simple changes** | |
| `/goal` | You have a clear, bounded task and want goal-directed delegation. The agent works autonomously toward the stated goal with a stop condition. |
| `your AI coding assistant -p` | You want headless execution for a well-defined task. The Ralph Wiggum loop (run, check, retry) is the universal autonomous pattern. |
| **Multi-session orchestration** | |
| Superset / Conductor / Antigravity / VS Code Agent View | You are running multiple agent sessions in parallel and need visibility, coordination, or session management across them. |

### Parallel work rules

- One change per worktree or isolated workspace. One fresh agent context per change.
- Choose interactive `/10x-implement` for complex changes, `/goal` or `your AI coding assistant -p` for simple ones.
- Parallelism is capped by review capacity. More agents without review means more unreviewed code, not higher throughput.
- The quality pain from faster shipping is intentional — it bridges into Module 3 testing gates.

### Lesson boundaries

- Do not reteach interactive `/10x-implement` or `/10x-impl-review`; those are Lessons 2 and 3.
- Do not introduce testing strategy here. The quality pain is the motivation for Module 3.
- Worktrees are a mechanism for isolation, not the topic of a full git tutorial.

### Paths used by this lesson

- `context/changes/<change-id>/` - active change folder
- `context/changes/<change-id>/plan.md` - implementation input for any execution mode

Skills must not write to `context/archive/`. Archived changes are immutable; if a resolved target path starts with `context/archive/`, abort with: "This change is archived. Open a new change with `/10x-new` instead."

<!-- END @przeprogramowani/10x-cli -->
