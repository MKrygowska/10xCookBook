# CI/CD Code Review Integration — Plan Brief

> Full plan: `context/changes/ci-cd-code-review/plan.md`
> Research: `context/changes/ci-cd-code-review/research.md`

## What & Why

Introduce an automated, pull-request-driven AI code reviewer in GitHub Actions. The goal is to provide developers with fast, constructive feedback on their pull requests directly in the GitHub interface, ensuring quality gates (correctness, readability, complexity, performance, security, and idiomaticity) are evaluated before changes are merged.

## Starting Point

We have a functional TypeScript script `scripts/review-gemini.ts` that runs a Gemini code review on a git diff passed via stdin. However, this is currently a local tool, using 5 older criteria, and is not integrated into any CI/CD automation or GitHub PR environment.

## Desired End State

Every pull request targeting `master` (excluding PRs containing only documentation/config changes) is reviewed automatically. The review results are posted as a styled comment on the PR (including a scorecard table) and the PR is labeled `ai-cr:passed` or `ai-cr:failed`. Developers can also trigger a manual re-run by adding the label `ai-cr:review`.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| :--- | :--- | :--- | :--- |
| **PR Description context** | Truncated to 2000 chars | Provides business context without risk of bloating model input with large logs. | Plan |
| **GHA strictness on fail** | Exit code 0 (Success) | Relies on the `ai-cr:failed` label to block/flag, leaving merge control human-driven. | Plan |
| **On-demand label cleanup** | Remove trigger label immediately | Prepares the trigger label to be added again later for subsequent reviews. | Plan |
| **Trigger paths exclusion** | Ignore *.md, .gitignore, .github/ | Avoids wasting API calls and CI minutes on non-code changes. | Plan |
| **Diff retrieval method** | Use `gh pr diff` | Obtains the exact PR diff from GitHub directly, avoiding shallow clone history issues. | Research |
| **PR Comment formatting** | Styled scorecard table + summary | Maximizes readability and visual clarity of review metrics. | Plan |

## Scope

**In scope:**
- Updating prompts schema to support 6 chosen criteria.
- Updating `review-gemini.ts` to consume PR title and description.
- Creating composite action under `.github/actions/code-review/`.
- Creating workflow file `.github/workflows/code-review.yml`.
- Automating PR comment posting and label management using GitHub CLI.

**Out of scope:**
- Enforcing branch protection blocking on failure (exit code 1).
- Reviewing direct push commits to `master`.

## Architecture / Approach

The pipeline will use a modular design to keep workflows clean. The main workflow `.github/workflows/code-review.yml` intercepts pull requests and triggers. It delegates the execution to a composite action `.github/actions/code-review/action.yml` which does the heavy lifting: checks out code, installs dependencies, pulls the diff, calls the Node script, parses the JSON output using `jq`, and invokes `gh` to apply labels and post comments.

```
PR Event / Label Added
         │
         ▼
.github/workflows/code-review.yml (Triggers, Path Filters, Permissions)
         │
         ▼
.github/actions/code-review/action.yml (Composite: env cache, dependencies)
         │
         ├───► Fetch diff using `gh pr diff`
         ├───► Run `npx tsx scripts/review-gemini.ts`
         ├───► Parse outputs with `jq`
         └───► Apply labels & post comment via `gh`
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
| :--- | :--- | :--- |
| **1. Script Updates & Local Testing** | Updated criteria and PR metadata support in `review-gemini.ts`. | Ensuring backward compatibility for local dry runs. |
| **2. Composite Action Setup** | Shell scripts/yml that installs, diffs, parses outputs, comments, and labels. | Correctly parsing JSON fields in bash environment without fragility. |
| **3. Main Workflow Integration** | Complete workflow integration with trigger constraints and permissions. | Handling secrets and on-demand trigger events properly. |

**Prerequisites:** GitHub repository access to create Actions and repository secrets (`GEMINI_API_KEY`).  
**Estimated effort:** ~1-2 sessions across 3 phases.

## Open Risks & Assumptions

- **Secret Access on PRs from Forks**: Repository secrets (like `GEMINI_API_KEY`) are by default not available to workflows triggered by pull requests from forks due to security restrictions. We assume this pipeline is for internal/collaborative branches within the same repository.
- **GitHub Token Permissions**: The default `GITHUB_TOKEN` must have write permissions for PRs, issues, and statuses enabled.

## Success Criteria (Summary)

- Pull requests automatically receive a code review comment showing a detailed scorecard of the 6 criteria.
- PRs are automatically labeled with `ai-cr:passed` or `ai-cr:failed` reflecting the AI verdict.
- Adding the `ai-cr:review` label triggers an immediate, fresh review and automatically cleans itself up.
