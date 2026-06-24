# CI/CD Code Review Integration Implementation Plan

## Overview

Introduce an automated pull request code review pipeline in GitHub Actions. The pipeline executes the `review-gemini.ts` script on pull requests targeting `master`, publishes a formatted review comment with scores on the PR, sets corresponding status labels (`ai-cr:passed` or `ai-cr:failed`), and supports on-demand runs via the `ai-cr:review` label.

## Current State Analysis

- A local typescript script `scripts/review-gemini.ts` is configured to run the Gemini 3.1 Flash-Lite model on a git diff piped from stdin.
- The script uses the schema in `scripts/prompts.ts` which has 5 older criteria.
- There are no verification workflows configured in `.github/workflows/` (only deployment workflows exist).
- Secrets management and labels are not automated in the CI/CD.

## Desired End State

- A GHA workflow runs on pull request changes to `master` and when the label `ai-cr:review` is added.
- The GHA workflow invokes a composite action `.github/actions/code-review` that executes `scripts/review-gemini.ts`.
- The script `scripts/prompts.ts` has 6 updated criteria: `implementationCorrectness`, `readabilityAndCleanCode`, `complexity`, `performanceAndResourceEfficiency`, `securitySafety`, and `idiomaticity`.
- The PR is labeled green (`ai-cr:passed`) or red (`ai-cr:failed`) based on the review verdict.
- A premium, styled markdown comment containing a table of scores and the summary is published to the PR.
- The `ai-cr:review` trigger label is automatically removed.
- Re-runs are skipped if changes only affect markdown/documentation files.

### Key Discoveries:

- `node_modules/@google/genai` is the installed SDK (requires `npm ci` in runner).
- `gh` (GitHub CLI) and `jq` are pre-installed in GitHub Action runners and provide a clean way to fetch PR diffs (`gh pr diff`), manage labels (`gh pr edit`), and post comments (`gh pr comment`) without third-party actions.
- GHA workflow requires write permissions for `pull-requests`, `issues`, and `statuses`.

## What We're NOT Doing

- Blocking PR merges at the workflow level (exit code 1) on failure. We will set exit code 0 and rely on the red label `ai-cr:failed` to indicate failure, keeping PR merge decisions human-driven.
- Automated code reviews on pushed commits directly to `master`.

## Implementation Approach

1. Update `scripts/prompts.ts` to support the 6 chosen criteria (swapping `testRiskCoverage` for `readabilityAndCleanCode` and `performanceAndResourceEfficiency`).
2. Update `scripts/review-gemini.ts` to read PR metadata (Title and Description) from environment variables `PR_TITLE` and `PR_DESCRIPTION` (truncating description to 2000 characters).
3. Create the composite action `.github/actions/code-review/action.yml` to package node setup, dependency installation, diff fetching, script execution, parsing via `jq`, label editing, and comment publishing.
4. Create the workflow `.github/workflows/code-review.yml` with triggers, path exclusions, permission flags, and on-demand trigger filtering.

---

## Phase 1: Script Updates & Local Testing

### Overview
Update the prompts schema, model parameters, and review execution code to accept PR title/description metadata and evaluate the 6 code review criteria.

### Changes Required:

#### 1. Prompts Schema
**File**: `scripts/prompts.ts`  
**Intent**: Update `SYSTEM_PROMPT`, `REVIEW_JSON_SCHEMA` and `Review` interface to support the 6 selected criteria, swapping `testRiskCoverage` out and adding `readabilityAndCleanCode` and `performanceAndResourceEfficiency`.  
**Contract**: Change exported interface `Review` and schema properties.

#### 2. Review Script
**File**: `scripts/review-gemini.ts`  
**Intent**: Read `PR_TITLE` and `PR_DESCRIPTION` from environment variables, truncate `PR_DESCRIPTION` to 2000 characters if too long, and format them into the model's user query alongside the piped git diff. Maintain backward compatibility (fall back to "brak" if env variables are missing).  
**Contract**: Read `process.env.PR_TITLE` and `process.env.PR_DESCRIPTION` and validate all 6 required fields from the updated schema.

### Success Criteria:

#### Automated Verification:
- Code builds cleanly: `npm run build` (or verify via tsx execution)

#### Manual Verification:
- Dry-run script locally using `echo "test" | npx tsx scripts/review-gemini.ts` and verify it outputs the 6 updated fields and defaults title/description to "brak".
- Set environment variables in terminal, run `echo "test" | npx tsx scripts/review-gemini.ts` and verify they are successfully included.

---

## Phase 2: Composite Action Setup

### Overview
Create a modular composite GitHub Action that installs dependencies, fetches the PR diff, executes the code review script, parses results, and manages PR comments/labels.

### Changes Required:

#### 1. Composite Action Definition
**File**: `[NEW] .github/actions/code-review/action.yml`  
**Intent**: Define a composite action accepting `pr-number`, `pr-title`, `pr-description`, and `gemini-api-key`. It will install dependencies, retrieve diff using `gh pr diff`, execute `review-gemini.ts` saving JSON output, parse metrics using `jq`, add/remove labels, and post a markdown review table comment.  
**Contract**: Input parameters and GHA composite action structure.
```yaml
inputs:
  pr-number:
    required: true
  pr-title:
    required: true
  pr-description:
    required: false
  gemini-api-key:
    required: true
```

### Success Criteria:

#### Automated Verification:
- File `.github/actions/code-review/action.yml` exists and conforms to YAML syntax.

#### Manual Verification:
- Verify that `jq` and `gh` cli usage in the action works correctly.

---

## Phase 3: Main Workflow Integration

### Overview
Create the main workflow file in `.github/workflows/` that triggers on PR events and labeled actions, applying path exclusions and permissions.

### Changes Required:

#### 1. Workflow Definition
**File**: `[NEW] .github/workflows/code-review.yml`  
**Intent**: Define the main workflow triggering on `pull_request` (`opened`, `synchronize`, `reopened`, `labeled`) targetting `master`. Ignore changes under `*.md`, `.gitignore`, and `.github/` folder. Add job `if` statement to filter label events only for `ai-cr:review`. Specify required permissions block.  
**Contract**: GHA workflow syntax, triggers, permissions, and steps calling `.github/actions/code-review`.

### Success Criteria:

#### Automated Verification:
- File `.github/workflows/code-review.yml` exists and has correct syntax.

#### Manual Verification:
- Commit and push changes, open a test pull request, and verify the review pipeline is triggered.
- Verify comment is posted on the PR with the score table.
- Verify correct label (`ai-cr:passed` or `ai-cr:failed`) is added, and the `ai-cr:review` label is removed when on-demand run is finished.

---

## Testing Strategy

### Manual Testing Steps:
1. Open a test Pull Request targeting the `master` branch.
2. Verify that the GitHub Action starts automatically.
3. Observe that it comments on the PR with scores and a summary, and adds the `ai-cr:passed` label (since the changes are clean).
4. Introduce a deliberate code smell/vulnerability in the PR, push it, and check that the label changes to `ai-cr:failed`.
5. Add the label `ai-cr:review` to the PR, verify the workflow is triggered again, and that the label is removed automatically upon starting.

## References

- Requirements document: `context/changes/ci-cd-code-review/requirements.md`
- Code review script: `scripts/review-gemini.ts`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Script Updates & Local Testing

#### Automated
- [x] 1.1 Code builds cleanly: compile check passes

#### Manual
- [x] 1.2 Verify local dry run outputs the 6 updated schema fields
- [x] 1.3 Verify environment variables PR_TITLE and PR_DESCRIPTION are accepted

### Phase 2: Composite Action Setup

#### Automated
- [ ] 2.1 Composite action file exists and conforms to YAML syntax

#### Manual
- [ ] 2.2 Verify action CLI calls for diffing, labeling, and commenting

### Phase 3: Main Workflow Integration

#### Automated
- [ ] 3.1 Main workflow file exists and GHA syntax is correct

#### Manual
- [ ] 3.2 Verify automatic trigger on opening or updating PR
- [ ] 3.3 Verify comment is successfully posted on PR
- [ ] 3.4 Verify labels are added/removed and ai-cr:review label is cleaned up
