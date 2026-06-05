# GDPR & Content Coverage Testing — Plan Brief

> Full plan: `context/changes/testing-gdpr-content-coverage/plan.md`
> Research: `context/changes/testing-gdpr-content-coverage/research.md`

## What & Why

Implement backend cascade deletion tests to fulfill user data privacy (GDPR) requirements and expand the database seed ingredients with common Polish cooking staples to support realistic recipe creation.

## Starting Point

Currently, deleting a user leaves behind orphaned recipes in InMemory testing because `DeleteUser` retrieves the user entity without eager-loading relationship links. Additionally, the global ingredient catalog is restricted to only 20 seed records.

## Desired End State

- User deletion cascades cleanly to delete all related private recipes, verified by automated unit tests.
- The global ingredient catalog is expanded to 40 items (including key Polish cooking staples), verified by a smoke test verifying coverage.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| **Cascade Behavior in Test DB** | Eager-load recipes (`.Include(u => u.Recipes)`) | Enables EF Core's Change Tracker to execute cascade deletions on `InMemoryDatabase` during testing. | Research |
| **Database Seeding** | Add EF Core migration | Standard database update pattern that deploys cleanly to production. | Plan |
| **Catalog Verification** | Verify catalog size and check for key Polish staples | Ensures Polish ingredients are present without pinning spelling details that would make tests brittle. | Plan |
| **Test Layer for GDPR** | Unit test in `UserServiceTests` | Fastest execution speed and aligns with existing user logic tests. | Plan |

## Scope

**In scope:**
- Eagerly loading user recipes in `DeleteUser`.
- Adding 20 Polish cooking ingredients (twaróg, kiełbasa, kapusta kiszona, etc.) to seed data.
- Generating and running an EF Core migration for the seeds.
- Writing a cascade delete test in `UserServiceTests.cs`.
- Writing a catalog coverage smoke test.

**Out of scope:**
- Modifying UI components or layout.
- Changing database providers or adding complex search algorithms to the backend.

## Architecture / Approach

1. **Service modification**: Modify the Entity Framework query to eagerly load user private recipes before removal.
2. **Catalog seeding**: Append the Polish ingredients to the seed array inside `AppDbContext` and generate a standard migration file to update database instances.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| **1. Backend & Migration** | Include eager-loading and seed 20 new Polish ingredients. | EF Core migration issues or validation failures. |
| **2. Automated Tests** | Write cascade delete unit tests and catalog smoke tests. | Incomplete validation of cascade behaviors. |
| **3. Cookbook Sync** | Update rollout status and document testing patterns in `test-plan.md`. | Out-of-date documentation. |

**Prerequisites:** None.
**Estimated effort:** ~1 session across 3 phases.

## Open Risks & Assumptions

- **InMemory Limitations**: We assume using `.Include(u => u.Recipes)` is sufficient to simulate cascade deletes on `InMemoryDatabase` (verified in research).

## Success Criteria (Summary)

- User deletion deletes their recipes in tests.
- The ingredient catalog contains at least 40 items and covers key Polish staples in smoke tests.
- All 43 C# backend tests compile and pass.
