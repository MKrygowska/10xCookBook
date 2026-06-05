# Test Plan

> Phased test rollout for this project. Strategy is frozen at the top
> (§1–§5); cookbook patterns at the bottom (§6) fill in as phases ship.
> Read before writing any new test.
>
> Refresh: re-run `/10x-test-plan --refresh` when stale (see §8).
>
> Last updated: 2026-06-02

---

## 1. Strategy

Tests follow three non-negotiable principles for this project:

1. **Cost × signal.** The cheapest test that gives a real signal for the
   risk wins. Do not promote to e2e because e2e "feels safer." Do not put a
   vision model on top of a deterministic visual diff that already catches
   the regression.
2. **User concerns are first-class evidence.** Risks anchored in "the
   team is worried about X, and the failure would surface somewhere in
   <area>" carry the same weight as PRD lines or hot-spot data.
3. **Risks are scenarios, not code locations.** This plan documents *what
   could fail* and *why we believe it's likely* — drawn from documents,
   interview, and codebase *signal* (churn, structure, test base). It does
   NOT claim to know which line owns the failure. That knowledge is
   produced by `/10x-research` during each rollout phase. If the plan and
   research disagree about where the failure lives, research is the
   ground truth.

Hot-spot scope used for likelihood weighting: `frontend/src/`, `backend/` — excluding `node_modules`, `dist`, `bin`, `obj`, `Data/Migrations`.

---

## 2. Risk Map

The top failure scenarios this project must protect against, ordered by
risk = impact × likelihood. Risks are failure scenarios in user / business
terms, not test names. The Source column cites the *evidence that surfaced
this risk* — never a specific file as "where the failure lives" (that is
research's job, see §1 principle #3).

| # | Risk (failure scenario) | Impact | Likelihood | Source (evidence — not anchor) |
|---|---|---|---|---|
| 1 | A logged-in user receives another user's private recipes in search results because the controller passes the wrong or missing `userId` to the service | High | Medium | interview Q1; archive `unified-matching/plan.md`; abuse lens (IDOR — ownership vs. authentication) |
| 2 | The ingredient scoring formula silently produces wrong match percentages — results look plausible but are incorrect | High | Medium | interview Q1; hot-spot dir `backend/Services/` (3 commits/30d); PRD §FR-001, FR-002 |
| 3 | A direct API call (Postman, curl) bypasses Angular validation and submits malformed data that passes through controllers unchecked, causing bad data or 500 errors | High | High | interview Q4; test-base profile (zero controller-layer tests); abuse lens (untrusted input) |
| 4 | Angular auth guard fails or is misconfigured — unauthenticated users reach `/dashboard`, `/my-recipes`, `/settings` | High | Medium | interview Q3+Q4; hot-spot dir `frontend/src/app` (45 commits/30d) |
| 5 | After account deletion, a user's private recipes survive in the database — cascade delete misconfigured | High | Medium | PRD §GDPR compliance; archive `user-data-retention/plan.md` |
| 6 | The global ingredient catalog (20 seeds) is too sparse — users cannot find common ingredients when creating private recipes | Medium | Medium | interview Q1; PRD §FR-005 |

**Impact × Likelihood rubric:**

| Rating | Impact | Likelihood |
|---|---|---|
| High | user loses access, data, or money; failure is publicly visible | area changes weekly, or we have already been burned here |
| Medium | feature degrades, a workaround exists, only some users affected | touched occasionally, has been a source of bugs |
| Low | cosmetic, easily reverted, no data effect | stable code, rarely touched |

### Risk Response Guidance

| Risk | What would prove protection | Must challenge | Context `/10x-research` must ground | Likely cheapest layer | Anti-pattern to avoid |
|---|---|---|---|---|---|
| #1 | Search results for User A never contain User B's private recipes | "Service tests pass so the full flow works" — the controller wiring of JWT → userId is untested | How the controller extracts `userId` from JWT claims and passes it to the match service | Integration (controller + service + in-memory DB) | Testing only the service method; not the HTTP → JWT → service chain |
| #2 | Known ingredient combinations produce exact expected match rates — a formula change breaks an existing test | "The existing service tests cover this already" | Scoring formula weights (primary vs. spice/staple) and rounding behavior | Unit (extend RecipeService cases) | Copying the formula into the assertion — oracle problem; test passes even when formula is wrong |
| #3 | A POST to `/api/auth/register` or `/api/recipes` with missing or invalid fields returns 400, not 200 or 500 | "Angular validates so the server never sees bad data" | Which DTO fields carry `[Required]`/`[MaxLength]`; whether `ModelState.IsValid` is checked in every controller action | Integration (direct HTTP to controller via TestServer) | Testing only the happy path with valid data |
| #4 | Navigating to `/dashboard` without a valid token redirects to `/login`; expired token also redirects | "The route has `canActivate` so it works" | How the Angular guard reads the token, what happens on expiry, what the redirect target is | Unit (Angular TestBed + router testing) | Testing only that the guard returns `true` for authenticated users |
| #5 | After `DELETE /api/users/me`, the deleted user's private recipes return zero rows | "Cascade delete is configured in EF so it works" | EF `OnDelete(Cascade)` config vs. actual behavior; whether in-memory DB matches SQL Server cascade semantics | Integration (service + in-memory DB verifying cascade) | Asserting only that `DeleteUser` returns `true`, not that associated recipes are gone |
| #6 | The ingredient catalog covers common Polish cooking ingredients beyond the 20 seeded | "20 seeds should be enough for MVP" | Current seed list vs. real user recipe ingredient patterns | Manual smoke / content assertion | Asserting that specific 20 named seeds exist by name — brittle, catches zero gaps |

---

## 3. Phased Rollout

Each row is a discrete rollout phase that will open its own change folder
via `/10x-new`. Status moves left-to-right through the values below; the
orchestrator updates Status as artifacts appear on disk.

| # | Phase name | Goal (one line) | Risks covered | Test types | Status | Change folder |
|---|---|---|---|---|---|---|
| 1 | Critical-path coverage | Prove data isolation and match rate correctness at cheapest layer | #1, #2 | unit + integration (xUnit, in-memory DB) | complete | context/archive/2026-06-04-testing-critical-path-coverage/ |
| 2 | Controller & validation layer | Prove server-side validation parity and auth guard correctness | #3, #4 | integration (ASP.NET Core TestServer) + unit (Angular TestBed) | complete | context/archive/2026-06-05-testing-controller-validation-layer/ |
| 3 | GDPR & content coverage | Prove account deletion cascades correctly; surface ingredient catalog gaps | #5, #6 | integration + manual smoke | change opened | context/changes/testing-gdpr-content-coverage/ |
| 4 | Quality gates wiring | Lock lint + typecheck + unit/integration gates in CI | cross-cutting | CI gates | not started | — |

**Status vocabulary** (fixed — parser literals):

| Value | Meaning |
|---|---|
| `not started` | No change folder for this rollout phase yet. |
| `change opened` | `context/changes/<id>/` exists with `change.md`; research not done. |
| `researched` | `research.md` exists in the change folder. |
| `planned` | `plan.md` exists with a `## Progress` section. |
| `implementing` | Progress section has at least one `[x]` and at least one `[ ]`. |
| `complete` | Progress section is fully `[x]`. |

---

## 4. Stack

The classic test base for this project. AI-native tools (if any) carry a
`checked:` date so future readers can see which lines need re-verification.
Recommendations in this section must be grounded in local manifests/configs
plus the MCP/tools actually exposed in the current session.

| Layer | Tool | Version | Notes |
|---|---|---|---|
| unit + integration (backend) | xUnit | 2.9.0 | Configured in `10x-cookbook-backend.csproj`; 2 test files in `backend/Tests/` |
| in-memory DB (backend tests) | EF Core InMemory | 8.0.8 | Used in existing `RecipeServiceTests` and `UserServiceTests` |
| HTTP integration (backend) | ASP.NET Core TestServer | 8.0.x | None yet — see §3 Phase 2 |
| unit + integration (frontend) | Karma + Jasmine | Angular 17 defaults | Configured; 3 spec files present, likely scaffold stubs |
| e2e | none yet | — | Out of scope for current rollout phases; no risk justified e2e over cheaper layers |
| AI-native | none | — | No AI-native layer added; no risk justified it under cost × signal |

**Stack grounding tools (current session):**
- Docs: none — no Context7 or framework docs MCP available; checked: 2026-06-02
- Search: web search available — used for stack validation if needed; checked: 2026-06-02
- Runtime/browser: no Playwright MCP; not available in current session; checked: 2026-06-02
- Provider/platform: no GitHub/Azure MCP; not used; checked: 2026-06-02

---

## 5. Quality Gates

The full set of gates that must pass before a change reaches production.
"Required for §3 Phase <N>" means the gate is enforced once that rollout
phase lands; before that, the gate is `planned`.

| Gate | Where | Required? | Catches |
|---|---|---|---|
| lint + typecheck | local + CI | required | syntactic / type drift |
| `dotnet build` | local + CI | required (already in AGENTS.md) | backend compilation errors |
| `npm run build` | local + CI | required (already in AGENTS.md) | frontend compilation errors |
| unit + integration (backend) | local + CI | required after §3 Phase 1 | logic regressions in services and controller wiring |
| unit + integration (frontend) | local + CI | required after §3 Phase 2 | auth guard and component regressions |
| CI gate wiring (GitHub Actions) | CI on push to `main` | required after §3 Phase 4 | catches all of the above before merge |

---

## 6. Cookbook Patterns

How to add new tests in this project. Each sub-section is filled in once
the relevant rollout phase ships; before that, the sub-section reads
"TBD — see §3 Phase <N>."

### 6.1 Adding a backend unit or integration test

To add a backend unit/integration test (e.g. for service logic or data isolation):
1. **Location**: Add your test to the appropriate class in the `backend.Tests` project (e.g. `RecipeServiceTests.cs` or `RecipeIntegrationTests.cs`).
2. **Setup**: Use `DbContextOptionsBuilder` with `.UseInMemoryDatabase(Guid.NewGuid().ToString())` to ensure tests run in isolation and do not leak state to other tests.
3. **Asserting Isolation**: When testing resource matching or ownership, seed the database with items belonging to distinct user IDs (`Guid.NewGuid()`), execute the query passing one specific user ID, and assert that no items belonging to other user IDs are returned.
4. **Scoring Verification**: To verify match rate or scoring formulas, specify exact input ingredient sets and mock recipe ingredient configurations, asserting the mathematically expected score derived from requirements (rather than copying the implementation logic).

### 6.2 Adding a backend controller integration test (validation / HTTP layer)

To verify that HTTP requests are validated and handled correctly by the controllers:
1. **Location**: Add your test to `backend.Tests/ValidationIntegrationTests.cs` or create a new test file under the `backend.Tests` directory.
2. **Setup**: Use `WebApplicationFactory<Program>` configured with `builder.UseEnvironment("Development")` and replacement of database services to use a unique in-memory database instance.
3. **JWT Authentication**: When testing endpoints that require authorization, generate a valid JWT using `JwtSecurityTokenHandler` with claims (e.g. name identifier, email) matching your mock user, and add it to the `DefaultRequestHeaders.Authorization` as a Bearer token.
4. **Validation Bounds**: Send invalid models (e.g., titles exceeding max length limits, missing fields, or invalid emails) via `client.PostAsJsonAsync` or `client.PutAsJsonAsync` and assert that the server returns `HttpStatusCode.BadRequest` (400) instead of throwing 500 errors or writing corrupted rows.

### 6.3 Adding a frontend unit test (guard, service, component)

To verify frontend route guards or services:
1. **Location**: Place Jasmine unit tests in a `*.spec.ts` file adjacent to the service/guard being tested (e.g. `auth.guard.spec.ts` or `auth.service.spec.ts`).
2. **Setup**: Configure `TestBed` using `HttpClientTestingModule` or mock providers rather than invoking real HTTP services.
3. **State Isolation**: Call `localStorage.clear()` inside both `beforeEach` and `afterEach` hooks to guarantee test isolation.
4. **Guard Protection**: Mock the `Router` and the active route, run the guard's activation observable/function, and assert that:
   - Users with a valid, non-expired token in `localStorage` are permitted through.
   - Unauthenticated or expired tokens cause the guard to return `false` (or redirect to `/login`).

### 6.4 Adding a GDPR / cascade-delete integration test

TBD — see §3 Phase 3 for account deletion and cascade verification patterns.

### 6.5 Verifying ingredient catalog coverage

TBD — see §3 Phase 3 for content smoke pattern.

---

## 7. What We Deliberately Don't Test

Exclusions agreed during the rollout (Phase 2 interview, Q5). Future
contributors should respect these unless the underlying assumption changes.

- **`/api/health` endpoint** — temporary debugging tool added during Azure deployment; planned for removal. Re-evaluate if it becomes a permanent feature. (Source: Phase 2 interview Q5; roadmap open question #2.)
- **UI styling and SCSS** — animations and colour regressions carry no data or access risk and break constantly under minor browser updates. Re-evaluate if a design system is introduced. (Source: Phase 2 interview Q5.)
- **Seed data migrations** — static, hand-written once; the migration files themselves are not hand-authored logic. Re-evaluate if seed logic becomes dynamic. (Source: Phase 2 interview Q5.)

---

## 8. Freshness Ledger

- Strategy (§1–§5) last reviewed: 2026-06-02
- Stack versions last verified: 2026-06-02
- AI-native tool references last verified: 2026-06-02 (none added)

Refresh (`/10x-test-plan --refresh`) when:

- a new top-3 risk surfaces from the roadmap or archive,
- a recommended tool's `checked:` date is older than three months,
- the project's tech stack changes (new framework, new test runner),
- §7 negative-space no longer matches what the team believes.
