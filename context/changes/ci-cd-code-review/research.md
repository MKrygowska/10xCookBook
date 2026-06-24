---
date: 2026-06-24T17:26:00+02:00
researcher: Antigravity
git_commit: 0576111db5a816f4f693c7c1ee2883fb97cb8433
branch: main
repository: MKrygowska/10xCookBook
topic: "CI/CD integration for PR automatic code reviews using Gemini"
tags: [research, codebase, github-actions, ci-cd, code-review, gemini]
status: complete
last_updated: 2026-06-24
last_updated_by: Antigravity
---

# Research: CI/CD integration for PR automatic code reviews using Gemini

**Date**: 2026-06-24T17:26:00+02:00
**Researcher**: Antigravity
**Git Commit**: 0576111db5a816f4f693c7c1ee2883fb97cb8433
**Branch**: main
**Repository**: MKrygowska/10xCookBook

## Research Question

How to integrate the `review-gemini.ts` script into a GitHub Actions workflow that runs on pull requests, retrieves the PR details and diff, posts a review comment, and manages PR labels (`ai-cr:failed`, `ai-cr:passed`, `ai-cr:review`).

## Summary

We can implement a highly robust and secure code review pipeline by combined usage of GitHub Actions and the GitHub CLI (`gh`). 
Instead of checking out a deep git history to calculate a diff manually (which is error-prone in CI environments), we can use the pre-installed GitHub CLI (`gh pr diff`) inside the runner to get the exact changes of the PR.
Comments and label changes can be performed using native `gh pr comment` and `gh pr edit` commands without requiring external heavy actions or custom node scripts, keeping the pipeline extremely fast and secure.

## Detailed Findings

### 1. Workflow Triggers & On-Demand Run
To support both automatic runs (for every new or updated PR to `master`) and manual/on-demand retries (triggered by adding the label `ai-cr:review`), the workflow trigger can be configured with a combination of `pull_request` types:
```yaml
on:
  pull_request:
    branches:
      - master
    types:
      - opened
      - synchronize
      - reopened
      - labeled
```
To prevent runs when unrelated labels are added, we can check the event name and label name in the job condition:
```yaml
if: |
  github.event_action != 'labeled' || 
  github.event.label.name == 'ai-cr:review'
```

### 2. PR Diff and Details Retrieval
By setting `GH_TOKEN: ${{ github.token }}` in the step environment, we can fetch all details using the GitHub CLI:
- **Diff**: `gh pr diff ${{ github.event.pull_request.number }} > pr.diff`
- **Title**: `${{ github.event.pull_request.title }}`
- **Description**: `${{ github.event.pull_request.body }}`

### 3. Writing Comments and Labels
Using GitHub CLI:
- **Ensuring Labels Exist**:
  ```bash
  gh label create "ai-cr:failed" --color "ff0000" --description "AI Code Review Failed" || true
  gh label create "ai-cr:passed" --color "00ff00" --description "AI Code Review Passed" || true
  ```
- **Adding / Removing Labels**:
  - On pass:
    ```bash
    gh pr edit ${{ github.event.pull_request.number }} --add-label "ai-cr:passed" --remove-label "ai-cr:failed,ai-cr:review" || true
    ```
  - On fail:
    ```bash
    gh pr edit ${{ github.event.pull_request.number }} --add-label "ai-cr:failed" --remove-label "ai-cr:passed,ai-cr:review" || true
    ```
- **Posting PR Comment**:
  ```bash
  gh pr comment ${{ github.event.pull_request.number }} --body-file review-comment.md
  ```

### 4. Code Review Script Updates
The script [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) should be slightly expanded to read `PR_TITLE` and `PR_DESCRIPTION` from environment variables, format them into the model's user prompt alongside the diff, and truncate the description to ~2000 characters to keep context window usage optimal.

### 5. Composite Action Structuring
Creating a composite action under `.github/actions/code-review/action.yml` encapsulates:
- Node setup (`actions/setup-node`).
- Package dependency cache and installation (`npm ci`).
- Running the `review-gemini.ts` script.
- Extracting the structured JSON output (specifically the `verdict` and `summary`) to decide which labels to add/remove and write the markdown comment.

## Code References

- [scripts/review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) - The core code review script utilizing the Google GenAI SDK.
- [.github/workflows/deploy-backend.yml](file:///c:/Users/reade/Documents/10xDev%20Project/.github/workflows/deploy-backend.yml) - Reference for existing GHA pipeline structure.

## Architecture Insights

- **Zero-Dependency CLI Integration**: Relying on the pre-installed GitHub CLI (`gh`) for API calls (comments, labels, diffs) avoids introducing dependency maintenance overhead (like custom JavaScript actions or Octokit setups).
- **Loose Script Coupling**: The script `review-gemini.ts` will continue to read the diff from `stdin`, meaning it remains fully runnable locally (e.g. `git diff | npx tsx scripts/review-gemini.ts`) without modifications.

## Historical Context (from prior changes)

- Prior CI/CD deployments in `context/archive/2026-05-29-deployment-ci-cd/` configured standard AWS/Azure deployment steps.
- Security policies specify that any API secrets (like `GEMINI_API_KEY`) must be stored in GitHub Repository Secrets and never exposed in the source code.

## Related Research

- None

## Open Questions

- None
