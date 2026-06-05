---
change_id: hook-enhancements
title: Expand local hooks with branch guards, scoped tests, and pre-push validations
status: complete
created: 2026-06-05
updated: 2026-06-05
archived_at: null
---

## Notes

Add:
1. Branch Name Guard to prevent direct commits to main.
2. Scoped unit test runs in per-edit hook when files in risk areas are modified.
3. Pre-push build and test validation via Lefthook.
