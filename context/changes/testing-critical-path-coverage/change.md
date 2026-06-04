---
change_id: testing-critical-path-coverage
title: Phase 1 test rollout — critical-path coverage for data isolation and match rate
status: implemented
created: 2026-06-04
updated: 2026-06-04
archived_at: null
---

## Notes

Open a change folder for rollout Phase 1 of context/foundation/test-plan.md: "Critical-path coverage".
Risks covered: #1 (private recipe data isolation — controller JWT wiring), #2 (match rate silent miscalculation).
Test types planned: unit + integration (xUnit, in-memory DB).
Risk response intent:
- Risk #1: prove search results for User A never contain User B's private recipes; challenge "service tests pass so the full flow works"; the HTTP → JWT → service chain is untested.
- Risk #2: prove known ingredient combinations produce exact expected match rates and a formula change breaks an existing test; challenge "existing service tests cover this"; avoid the oracle problem — do not copy the formula into the assertion.
