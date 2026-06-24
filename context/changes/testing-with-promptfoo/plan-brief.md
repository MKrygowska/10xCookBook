# Testing with Promptfoo — Plan Brief

> Full plan: `context/changes/testing-with-promptfoo/plan.md`
> Research: `context/changes/testing-with-promptfoo/research.md`

## What & Why

We want to introduce automated evaluations for our code reviewer agent using `promptfoo`. The goal is to compare prompt performance and reliability across three different models (`gemini-3.1-flash-lite`, `z-ai/glm-5.1`, and `deepseek/deepseek-v4-flash`). We will evaluate their ability to detect three severe architectural and lifecycle flaws in a complex React migration diff.

## Starting Point

The current agent is written as a self-executing CLI script [review-gemini.ts](file:///c:/Users/reade/Documents/10xDev%20Project/scripts/review-gemini.ts) that reads from standard input and calls Google Gemini directly via the local SDK. It has no mechanism for being imported by evaluation tools and does not support OpenRouter models.

## Desired End State

1.  A clean, side-effect-free module `review-agent.ts` exporting the review agent functions and prompts.
2.  `promptfoo` is integrated as a local dev dependency and runs a matrix test across three models using a custom provider.
3.  The evaluation suite executes a test case using a React migration diff fixture, verifying that the models correctly output valid JSON, fail the diff (verdict: `fail`), and correctly flag the three specific React flaws using LLM-as-a-judge.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| :--- | :--- | :--- | :--- |
| **Separation of concerns** | Split into agent module & thin CLI runner | Enables clean importing by promptfoo without breaking existing GHA workflow. | Research |
| **Multi-model connection** | OpenRouter API for GLM and DeepSeek | Simplifies access to multiple third-party models using a single environment key. | Plan |
| **React Diff storage** | Separate file fixture (`react-migration.diff`) | Keeps the YAML config file clean, readable, and easy to maintain. | Plan |
| **Judge model** | `gemini-3.1-flash-lite` | Free-tier friendly, fast, and sufficient for structured grading tasks. | Plan |
| **Assertion types** | Javascript validation + LLM-as-a-judge | Direct Javascript parses the JSON output for static fields, while LLM checks semantic understanding. | Plan |

## Scope

**In scope:**
- Refactoring `review-gemini.ts` into a clean importable module `review-agent.ts` and thin CLI wrapper.
- Implementing OpenRouter API calling inside the agent for model overrides.
- Creating the promptfoo configuration and custom provider.
- Creating the React 16 to 19+ migration diff fixture with three specific flaws.
- Running and displaying promptfoo comparison results locally.

**Out of scope:**
- Modifying the text of the system prompt.
- Setting up promptfoo online dashboard (viewing is strictly local via `npx promptfoo view`).
- Integrating other languages (like Python) into the project.

## Architecture / Approach

We will separate the execution wrapper from the core logic:
- `review-agent.ts` is the logic module. It checks if the requested model is an OpenRouter model (begins with `openrouter/` or match) and routes via OpenRouter's completions endpoint using `fetch`. Otherwise, it uses `@google/genai`.
- `promptfoo-provider.ts` is a custom provider class that imports `review` and passes the diff from test variables.
- `promptfooconfig.yaml` binds the custom provider to each model and defines the test cases.

```
[promptfoo] -> [promptfoo-provider.ts] -> [review-agent.ts] -> [Gemini API / OpenRouter API]
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
| :--- | :--- | :--- |
| **1. Refactoring** | Clean importable `review-agent.ts` | Potential regressions in existing CLI/GHA runner functionality. |
| **2. Provider Setup** | Promptfoo dependency and TS provider script | Promptfoo loading TS files incorrectly due to ES module configurations. |
| **3. Test Suite** | React diff fixture and YAML config with assertions | LLM-as-a-judge false positives/negatives in grading. |

**Prerequisites:** Valid `GEMINI_API_KEY` and `OPENROUTER_API_KEY` environment variables.
**Estimated effort:** ~1 session across 3 phases.

## Open Risks & Assumptions
- **OpenRouter API Key**: Assumes the user has/will configure `OPENROUTER_API_KEY` with credits to run the OpenRouter model evaluations.
- **Model Availability**: Assumes the selected OpenRouter models (`z-ai/glm-5.1` and `deepseek/deepseek-v4-flash`) are online and responsive.

## Success Criteria (Summary)
- Running `npx promptfoo eval` runs to completion and shows a side-by-side comparison matrix of the three models.
- The test case correctly asserts that the code reviewer verdict is `fail` and that all three React flaws are explicitly called out by the models.
