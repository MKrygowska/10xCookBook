---
change_id: testing-gdpr-content-coverage
title: GDPR and content coverage tests
status: archived
created: 2026-06-05
updated: 2026-06-05
archived_at: 2026-06-05T10:30:00Z
---

## Notes

Open a change folder for rollout Phase 3 of context/foundation/test-plan.md: "GDPR & content coverage".
Risks covered: #5, #6. Test types planned: integration + manual smoke.
Risk response intent:
- Risk #5: Prove that after DELETE /api/users/me, the deleted user's private recipes return zero rows. Challenge the assumption that "cascade delete works in EF just because it is configured"; avoid asserting only that DeleteUser returns true rather than that the recipes are actually gone.
- Risk #6: Prove that the ingredient catalog covers common Polish cooking ingredients beyond the 20 seeded. Challenge the assumption that "20 seeds should be enough for MVP"; avoid asserting only that specific 20 named seeds exist by name.
