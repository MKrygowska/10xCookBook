# Testing with Promptfoo — Plan Brief

> Full plan: `context/changes/testing-with-promptfoo/plan.md`
> Research: `context/changes/testing-with-promptfoo/research.md`

## What & Why

We want to introduce automated evaluations for our code reviewer agent using `promptfoo`. The goal is to compare prompt performance and reliability across three different models from Google Gemini (`gemini-3.1-flash-lite`, `gemini-1.5-pro`, and `gemini-1.5-flash`) directly, utilizing the existing `GEMINI_API_KEY` without requiring any other credentials or external gateways. We will evaluate their ability to detect three severe architectural and lifecycle flaws in a complex React migration diff.

## Starting Point

The current agent is written as a self-executing CLI script [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) that reads from standard input and calls Google Gemini directly via the local SDK. It has no mechanism for being imported by evaluation tools and does not support passing a model override dynamically.

## Desired End State

1.  A clean, side-effect-free module `review-agent.ts` exporting the review agent functions and prompts.
2.  `promptfoo` is integrated as a local dev dependency and runs a matrix test across the three Google Gemini models using a custom provider.
3.  The evaluation suite executes a test case using a React migration diff fixture, verifying that the models correctly output valid JSON, fail the diff (verdict: `fail`), and correctly flag the three specific React flaws using LLM-as-a-judge.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| :--- | :--- | :--- | :--- |
| **Separation of concerns** | Split into agent module & thin CLI runner | Enables clean importing by promptfoo without breaking existing GHA workflow. | Research |
| **Multi-model connection** | Direct API calls to Gemini | Uses direct SDK calls with the existing `GEMINI_API_KEY` for simplicity. | Plan |
| **React Diff storage** | Separate file fixture (`react-migration.diff`) | Keeps the YAML config file clean, readable, and easy to maintain. | Plan |
| **Judge model** | `gemini-3.1-flash-lite` | Free-tier friendly, fast, and sufficient for structured grading tasks. | Plan |
| **Assertion types** | Javascript validation + LLM-as-a-judge | Direct Javascript parses the JSON output for static fields, while LLM checks semantic understanding. | Plan |

## Scope

**In scope:**
- Refactoring `review-gemini.ts` into a clean importable module `review-agent.ts` and thin CLI wrapper.
- Implementing dynamic model configuration inside the agent for Gemini models.
- Creating the promptfoo configuration and custom provider.
- Creating the React 16 to 19+ migration diff fixture with three specific flaws.
- Running and displaying promptfoo comparison results locally.

**Out of scope:**
- Modifying the text of the system prompt.
- Setting up promptfoo online dashboard (viewing is strictly local via `npx promptfoo view`).
- Integrating OpenRouter or Anthropic Claude API keys.

## Architecture / Approach

We will separate the execution wrapper from the core logic:
- `review-agent.ts` is the logic module. It initializes the Google GenAI SDK and passes the selected model to the `generateContent` method.
- `promptfoo-provider.ts` is a custom provider class that imports `review` and passes the diff from test variables.
- `promptfooconfig.yaml` binds the custom provider to each model and defines the test cases.

```
[promptfoo] -> [promptfoo-provider.ts] -> [review-agent.ts] -> [Gemini SDK]
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
| :--- | :--- | :--- |
| **1. Refactoring** | Clean importable `review-agent.ts` | Potential regressions in existing CLI/GHA runner functionality. |
| **2. Provider Setup** | Promptfoo dependency and TS provider script | Promptfoo loading TS files incorrectly due to ES module configurations. |
| **3. Test Suite** | React diff fixture and YAML config with assertions | LLM-as-a-judge false positives/negatives in grading. |

**Prerequisites:** Valid `GEMINI_API_KEY` (or `GOOGLE_API_KEY`) environment variable.
**Estimated effort:** ~1 session across 3 phases.

## Open Risks & Assumptions
- **API Limits**: Google AI Studio has free-tier rate limits. Promptfoo handles throttling, but concurrency should be kept low to avoid 429 errors.

## Success Criteria (Summary)
- Running `npx promptfoo eval` runs to completion and shows a side-by-side comparison matrix of the three Gemini models.
- The test case correctly asserts that the code reviewer verdict is `fail` and that all three React flaws are explicitly called out by the models.
