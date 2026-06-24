# Testing with Promptfoo Implementation Plan

## Overview

We will introduce LLM prompt/agent evaluation by setting up `promptfoo` and configuring it to test our code reviewer agent across three different models from Google Gemini (`gemini-3.1-flash-lite`, `gemini-1.5-pro`, and `gemini-1.5-flash`) directly, utilizing the existing `GEMINI_API_KEY` without requiring any other credentials or external gateways.

To make this possible, we will refactor the code reviewer script to separate the core agent logic (which will be clean and importable) from the CLI runner. We will then configure promptfoo with a custom TypeScript provider, a realistic React 16 to 19+ migration diff containing three severe architectural and lifecycle flaws, and assertions (both LLM-as-a-judge and static assertions) to verify the reviewer's performance.

## Current State Analysis

- **Self-Executing Script**: Currently, [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) has immediate side-effects at the bottom of the file (reading from `process.stdin` and calling the review). This prevents importing its constants (`SYSTEM_PROMPT`, `REVIEW_JSON_SCHEMA`) or functions (`review`) into any test harness.
- **Single Model Lock**: The script is hardcoded to call `gemini-3.1-flash-lite` via the `@google/genai` SDK. There is no mechanism to pass in a different model.
- **No Evaluation Suite**: We currently have no automated way to evaluate prompt quality, model regressions, or structured output format guarantees across different LLMs.

## Desired End State

- **Importable Agent**: The core agent logic resides in [review-agent.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-agent.ts) with zero side-effects and is fully importable.
- **Model Parameterization**: The `review()` function supports a `model` parameter, allowing us to pass the target Gemini model name dynamically.
- **Thin CLI Runner**: The GHA workflow still calls [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts), which acts as a thin wrapper importing and executing the agent.
- **Promptfoo Integration**:
  - `promptfoo` is installed and runnable via `npx promptfoo eval`.
  - A custom provider [promptfoo-provider.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/promptfoo-provider.ts) routes promptfoo test cases to our agent.
  - Evaluation covers three Google Gemini models: `gemini-3.1-flash-lite`, `gemini-1.5-pro`, and `gemini-1.5-flash` using `GEMINI_API_KEY` (or `GOOGLE_API_KEY`).
  - A test suite evaluates the agent against a React migration diff fixture with specific assertions.

### Key Discoveries:
- [review-gemini.ts:145-153](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L145-L153) contains the blocking runner code that we must extract.
- Promptfoo's custom provider API allows a `file://` provider to implement `ApiProvider` and receive test variables via its `context.vars`.

## What We're NOT Doing
- We are not deploying the promptfoo web viewer online (it will run locally).
- We are not changing the production behavior or prompt text of the code reviewer (only restructuring the files).
- We are not using OpenRouter or Anthropic Claude API keys (the user chose to bypass them for simplicity and cost).

## Implementation Approach

1.  **Refactor**: Split `review-gemini.ts` into `review-agent.ts` (exporting the `review` function and schema) and `review-gemini.ts` (the runner). We will extend the `review()` signature to support an optional `model` override passed to `@google/genai` (defaulting to `gemini-3.1-flash-lite`).
2.  **Dependencies**: Install `promptfoo` as a `devDependency` in `package.json`.
3.  **Harness**: Implement the promptfoo custom provider in TypeScript, loading variables from the test case.
4.  **Fixtures**: Write the React 16 to 19 migration diff containing `componentWillMount`, direct DOM manipulation `document.getElementById`, and fetch call in render.
5.  **Config**: Create `promptfooconfig.yaml` with the test cases, assertions (LLM-as-a-judge using `gemini-3.1-flash-lite`, and JSON validation), and the three Gemini model configurations.

---

## Phase 1: Refactor Code Reviewer for Importability

### Overview
Extract the core agent logic and constants into a clean module, leaving the CLI wrapper intact.

### Changes Required:

#### 1. Create review-agent.ts [NEW]
**File**: `scripts/review-agent.ts`

**Intent**: Provide an importable, side-effect-free module for the code review agent.

**Contract**:
- Export `SYSTEM_PROMPT`, `REVIEW_JSON_SCHEMA`, `Review` interface.
- Export `review(diff: string, options?: { model?: string })` returning `Promise<Review>`.
- Use the `@google/genai` SDK and call the model specified by `options.model` (defaulting to `gemini-3.1-flash-lite`) using the environment key `GEMINI_API_KEY` (or `GOOGLE_API_KEY`).

#### 2. Refactor review-gemini.ts [MODIFY]
**File**: `scripts/review-gemini.ts`

**Intent**: Simplify the file to act only as a CLI entrypoint that reads stdin and calls `review()`.

**Contract**:
- Import `review` from `./review-agent.js`.
- Keep the `readDiff` function and the execution loop.
- Remove all prompts, schema definitions, and SDK imports.

### Success Criteria:

#### Automated Verification:
- Code compiles cleanly: `npx tsc --noEmit`
- Run local dry run on a test diff: `echo "test" | npx tsx scripts/review-gemini.ts` runs successfully and outputs expected JSON structure.

#### Manual Verification:
- Verify that metadata logs (`[INFO] Odczytane metadane PR...`) are printed to stderr, and JSON verdict goes to stdout.

---

## Phase 2: Setup Promptfoo and Custom Provider

### Overview
Install promptfoo, configure environment variables, and create the custom provider to connect promptfoo with our agent.

### Changes Required:

#### 1. Add promptfoo devDependency [MODIFY]
**File**: `package.json`

**Intent**: Add promptfoo to project devDependencies.

**Contract**:
- Run `npm install -D promptfoo` or edit `package.json` and run `npm install`.

#### 2. Create promptfoo-provider.ts [NEW]
**File**: `scripts/promptfoo-provider.ts`

**Intent**: Custom provider for promptfoo that routes test cases to our agent.

**Contract**:
```typescript
import { ApiProvider, ProviderResponse } from 'promptfoo';
import { review } from './review-agent.js';

export default class CodeReviewerProvider implements ApiProvider {
  private model: string;

  constructor(options: { id: string; config?: { model?: string } }) {
    this.model = options.config?.model || 'gemini-3.1-flash-lite';
  }

  id() {
    return `code-reviewer:${this.model}`;
  }

  async callApi(prompt: string, options?: any, context?: any): Promise<ProviderResponse> {
    const diff = context?.vars?.diff || '';
    try {
      const result = await review(diff, { model: this.model });
      return {
        output: JSON.stringify(result)
      };
    } catch (error: any) {
      return {
        error: error.message
      };
    }
  }
}
```

### Success Criteria:

#### Automated Verification:
- Code compiles cleanly: `npx tsc --noEmit`

#### Manual Verification:
- N/A (will be tested in Phase 3)

---

## Phase 3: Create React Diff Fixture & Configure Suite

### Overview
Write the React migration buggy diff, configure the `promptfooconfig.yaml` with the three models and assertions, and run the evaluation.

### Changes Required:

#### 1. Create React migration diff fixture [NEW]
**File**: `scripts/tests/fixtures/react-migration.diff`

**Intent**: Provide a realistic, flawed React 16 to 19+ migration diff.

**Contract**:
- Provide a unified diff modifying a component.
- The diff must contain:
  1.  Use of `componentWillMount` (deprecated lifecycle).
  2.  Direct DOM mutation (e.g. `document.getElementById('my-el').style.color = 'red'`).
  3.  API fetch call in the render body (causing infinite/uncontrolled rendering loops).

#### 2. Create promptfooconfig.yaml [NEW]
**File**: `promptfooconfig.yaml`

**Intent**: Define the promptfoo evaluation configuration.

**Contract**:
- `prompts`: Reference our `SYSTEM_PROMPT` in `scripts/review-agent.ts` or a plain text description.
- `providers`:
  - `file://scripts/promptfoo-provider.ts` configured with `model: gemini-3.1-flash-lite`.
  - `file://scripts/promptfoo-provider.ts` configured with `model: gemini-1.5-pro`.
  - `file://scripts/promptfoo-provider.ts` configured with `model: gemini-1.5-flash`.
- `tests`:
  - Refer to the React migration diff file fixture `file://scripts/tests/fixtures/react-migration.diff` as the `diff` variable.
  - Assertions:
    - LLM-as-a-judge assertion (using `gemini-3.1-flash-lite` as the judge model) confirming that the review correctly identifies:
      1.  `componentWillMount` usage.
      2.  Direct DOM update (`document.getElementById`).
      3.  Fetch call in the render path.
    - Static Javascript assertion:
      `JSON.parse(output).verdict === 'fail'` and checking that the returned object contains all required JSON fields.

### Success Criteria:

#### Automated Verification:
- Promptfoo evaluation passes: `npx promptfoo eval` runs without syntax errors and creates the results database.

#### Manual Verification:
- Run `npx promptfoo view` and verify that the comparison table displays the results for all three models side-by-side.

---

## Testing Strategy

### Unit Tests:
- Dry run script compilation checks.

### Integration/Eval Tests:
- `npx promptfoo eval` verifying:
  - Model compliance with the JSON output format.
  - Detection rate of the three critical React flaws.
  - Correctness of the `fail` verdict.

### Manual Testing Steps:
1.  Set `GEMINI_API_KEY` (or `GOOGLE_API_KEY`).
2.  Run `npx promptfoo eval` to perform the matrix tests.
3.  Inspect the local dashboard to check performance and judge remarks.

## Performance Considerations
- Rate limits may apply on free-tier keys. Promptfoo handles retries and token counts automatically.

## References
- Code Review Script: `scripts/review-gemini.ts`
- Upstream Research: `context/changes/testing-with-promptfoo/research.md`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Refactor Code Reviewer for Importability

#### Automated
- [x] 1.1 Extract core agent logic to review-agent.ts and compile check passes
- [x] 1.2 Refactor review-gemini.ts to use imported review function and verify dry run

#### Manual
- [x] 1.3 Verify CLI metadata logs (stderr) and JSON verdict (stdout) are preserved

### Phase 2: Setup Promptfoo and Custom Provider

#### Automated
- [ ] 2.1 Install promptfoo devDependency
- [ ] 2.2 Create custom TypeScript promptfoo provider script

### Phase 3: Create React Diff Fixture & Configure Suite

#### Automated
- [ ] 3.1 Create React migration flawed diff file fixture
- [ ] 3.2 Create promptfooconfig.yaml configuration file
- [ ] 3.3 Execute promptfoo eval run successfully
