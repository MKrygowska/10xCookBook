---
date: 2026-06-24T18:35:00+02:00
researcher: Antigravity
git_commit: ad9d5463c789e8558e3b35ae28a7ca5565b231d9
branch: feat/testing-with-promptfoo
repository: MKrygowska/10xCookBook
topic: "Analyze the current state of @packages/code-reviewer in the context of potential eval introduction - reusability of prompts, importability of agent, etc."
tags: [research, codebase, code-reviewer, promptfoo, evaluation]
status: complete
last_updated: 2026-06-24
last_updated_by: Antigravity
---

# Research: Code Reviewer Eval Introduction with Promptfoo

**Date**: 2026-06-24T18:35:00+02:00
**Researcher**: Antigravity
**Git Commit**: ad9d5463c789e8558e3b35ae28a7ca5565b231d9
**Branch**: feat/testing-with-promptfoo
**Repository**: MKrygowska/10xCookBook

## Research Question

Analyze the current state of '@packages/code-reviewer' (our code-review agent scripts) in the context of potential eval introduction - reusability of prompts, importability of agent, etc. Verify if the tech stack is aligned with promptfoo, or analyze other OSS evaluation tools.

## Summary

1. **Tech Stack Alignment**: Our tooling stack (Node.js, TypeScript, TSX, ES Modules, and Gemini API via `@google/genai`) is **perfectly aligned** with **Promptfoo**. Promptfoo is JS/TS-native, lightweight, highly configurable, and supports TypeScript custom providers and Gemini models natively.
2. **Current State Analysis**: The current codebase script [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) contains the complete agent logic (system prompt, JSON schema, and API call) but is **not importable** because it has side-effects (it reads from `process.stdin` and runs the main loop immediately upon import).
3. **Refactoring Recommendation**: We should split `review-gemini.ts` into a clean, reusable agent module [review-agent.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-agent.ts) (which exports the `SYSTEM_PROMPT`, `REVIEW_JSON_SCHEMA`, and `review` function without side effects) and keep `review-gemini.ts` as a thin CLI/GHA runner wrapper.
4. **Promptfoo Integration Strategy**: We can configure promptfoo using a `promptfooconfig.yaml` file, importing our custom reviewer agent using a file provider (`file://scripts/promptfoo-provider.ts`). This lets us pass `diff` samples to the agent and run assertions (e.g., matching the parsed JSON `verdict`, schema validation, or model-graded evaluations).

## Detailed Findings

### Tech Stack & Tooling Alignment

*   **Promptfoo** is Node.js-based and supports TypeScript natively via `tsx` out-of-the-box.
*   We can add `promptfoo` as a `devDependency` in our root [package.json](file:///c:/Users/reade/Documents/10xDev%20Project/package.json).
*   **Authentication**: Promptfoo's built-in `google` provider uses `GOOGLE_API_KEY`, which we can easily map from the existing `GEMINI_API_KEY` used in GHA workflows.

### Current Code Reviewer State (scripts/review-gemini.ts)

*   **Prompt Reusability**: The `SYSTEM_PROMPT` is defined on [review-gemini.ts:3-7](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L3-L7) as an exported constant string, and the user prompt is dynamically built inside `review()` on [review-gemini.ts:96-104](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L96-L104). They are reusable but bound to the file.
*   **Importability Barrier**: At the bottom of the file on [review-gemini.ts:145-153](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L145-L153), the script executes:
    ```typescript
    const diff = await readDiff();
    try {
      const result = await review(diff);
      console.log(JSON.stringify(result, null, 2));
    } ...
    ```
    This top-level await immediately executes code and blocks, making it impossible to import the `review` function or prompt constants in a test runner or custom provider without triggering the CLI input read side-effects.

### Proposed Architectural Separation of Concerns (SoC)

To enable clean prompt/agent evaluations, we should split the script:

```
scripts/
├── review-agent.ts   <-- Core agent, prompts, and schema (no side-effects, export-only)
└── review-gemini.ts  <-- Thin CLI wrapper (reads stdin, imports review-agent.ts, logs result)
```

This ensures:
1.  No changes are required to the GitHub Actions composite action `.github/actions/code-review/action.yml` (since it still executes `npx tsx scripts/review-gemini.ts`).
2.  The `review-agent.ts` is 100% clean and importable by Promptfoo providers or unit tests.

### Other OSS Eval Toolkits Considered

For completeness, we reviewed alternative open-source evaluation tools:
*   **DeepEval**: Highly capable, but Python-centric. It would introduce a Python runtime dependency to our pure JS/TS environment.
*   **Langfuse**: Exceptional for logging/tracing and runs evaluations (LLM-as-a-judge), but requires setting up an external database, backend, and SDK integration. Too heavy for simple CLI/local prompt evaluations.
*   **Ragas**: Python-centric and heavily geared towards RAG architectures (retrieval vs generation metrics), which is not our use case.

Thus, **Promptfoo** is the ideal choice.

## Code References

*   [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) - The current code-reviewer implementation.
*   [review-gemini.ts:3-7](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L3-L7) - Current `SYSTEM_PROMPT` location.
*   [review-gemini.ts:145-153](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts#L145-L153) - Current entry point blocking importability.

## Architecture Insights

*   **Custom Provider Pattern**: We can implement a file-based promptfoo provider `promptfoo-provider.ts` that exports a standard interface:
    ```typescript
    import { review } from './review-agent';
    import type { ApiProvider, ProviderResponse } from 'promptfoo';

    export default class ReusableReviewerProvider implements ApiProvider {
      id() { return 'code-reviewer-agent'; }
      async callApi(prompt: string, options?: any, context?: any): Promise<ProviderResponse> {
        // promptfoo passes the variables through the context
        const diff = context.vars.diff;
        const result = await review(diff);
        return {
          output: JSON.stringify(result)
        };
      }
    }
    ```
*   **Assertion-Led Evaluation**: Test assertions can then parse the output JSON and check:
    - Verdict equivalence (e.g. `output.verdict === 'pass'`)
    - JSON validation against the schema
    - Custom thresholds on scores

## Related Research

*   No prior evaluations or promptfoo configurations exist in this repository (first-time setup).

## Open Questions

1.  **Model version lock**: We are currently locked into `gemini-3.1-flash-lite` due to free-tier quotas. Should the evaluations test model variations (like `gemini-1.5-pro` or `gemini-2.5-flash` if available in the future) to compare performance/cost?
2.  **Dataset collection**: How many sample diffs (good vs buggy) do we want to seed in the test suite to establish a reliable baseline?
