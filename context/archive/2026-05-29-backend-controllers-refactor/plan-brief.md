# Backend Controllers Refactor — Plan Brief

> Full plan: `context/changes/backend-controllers-refactor/plan.md`

## What & Why

We are refactoring the ASP.NET Core backend API to replace the inline Minimal API endpoints with standard, highly maintainable MVC controllers (`ControllerBase`). This ensures architectural separation of concerns by pulling all raw database access, transforms, and business rule validations into dedicated services, keeping our controllers extremely clean and aligned with C#/.NET enterprise best practices.

## Starting Point

Today, all HTTP endpoints reside inside `backend/Endpoints/` where static extension methods register routing logic directly into `Program.cs`. The largest file, `RecipeEndpoints.cs`, queries `AppDbContext` and manipulates database models inline, mixing presentation, validation, and data access concerns in single handlers.

## Desired End State

The entire static `backend/Endpoints/` directory is deleted, and endpoints are cleanly routed via classic controllers (`AuthController`, `UserController`, `IngredientsController`, `RecipesController`) inheriting from `BaseApiController`. The service layers are enriched to hold all business logic, raising custom domain exceptions that controllers catch inline explicitly to preserve backward-compatible HTTP structures and keep client interactions transparently unbroken.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| **Service Failure Communication** | Throwing Custom Domain Exceptions | This is the most idiomatic C# design pattern, avoiding cluttering method signatures and separating failures from normal execution flows. |
| **Request/Response DTOs Placement** | Dedicated `backend/DTOs/` Folder | Centralizes payload definitions to facilitate sharing across controllers and services, maintaining clean code organization. |
| **Input Validation Boundary** | Shared: Controller (presence) + Service (database & logic) | Leverages built-in controller validation engines while preserving the service's domain self-integrity. |
| **Controller Actions Return Types** | `ActionResult<T>` for GET / `IActionResult` for CRUD | Offers type-safety forSwagger auto-documentation of payloads while preserving dynamic status responses. |
| **Domain Exception Mapping** | Explicit Inline `try-catch` blocks | Ensures explicit error-to-response mapping without "magic" filters, keeping endpoint outcomes clear and traceable. |
| **Shared Controller Logic** | Custom `BaseApiController` Class | Avoids code replication for claim extraction (like reading authenticated user's Guid) and global API routing headers. |
| **Ingredients Endpoint Logic** | Brand New `IngredientService` | Prevents bloating `RecipeService` and strictly separates independent database tables into separate domain service units. |
| **Migration Transition Strategy** | Clean Cut Removal of Minimal APIs | Minimizes workspace bloat and enforces singular implementation focus verified through existing HTTP integration scripts. |

## Scope

**In scope:**
* Deleting `backend/Endpoints/` and maps in `Program.cs`.
* Creating custom exceptions (`ValidationException`, `NotFoundException`, `ForbiddenException`).
* Creating `backend/DTOs/` for registration, login, recipe requests, and response transformations.
* Refactoring `RecipeService` and creating `IngredientService`.
* Implementing `BaseApiController` and 4 concrete derived controllers in `backend/Controllers/`.
* Writing XUnit service test additions in `RecipeServiceTests.cs`.
* Retaining perfect backward compatibility with existing Angular client requests and responses.

**Out of scope:**
* Modifying database schemas or active migrations.
* Modifying client-side Angular files or interfaces.
* Changing JWT authentication details or key configurations.

## Architecture / Approach

Incoming HTTP requests hit controllers inheriting from `BaseApiController`. The controllers validate model state and delegate to domain services (`UserService`, `RecipeService`, `IngredientService`). The services execute DB operations via `AppDbContext` and throw domain exceptions on violations. The controllers catch these exceptions and return exact status codes:

```
[ HTTP Client ] ---> [ Controller ] --(Call)--> [ Service ] ---> [ AppDbContext ]
                          |                          |
                    (try-catch)               (Throws Domain Exception)
                          |
             [ 400 BadRequest / 404 NotFound / 403 Forbidden ]
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| **1. Domain Exceptions & DTOs Setup** | Exception hierarchy, centralized DTO records, and `IngredientService` scaffolding. | Incomplete validation properties on new DTO models. |
| **2. Service Layer Refactoring** | Robust, DB-isolated service methods in `RecipeService` and `IngredientService` validated via unit tests. | Broken LINQ query transformations or edge cases in eager loading. |
| **3. Controller Layer & Cleanup** | Standard controllers mapping all API routes, deleting static endpoints, and wiring `Program.cs`. | Endpoint signature mismatch or routing path drift. |
| **4. Verification** | 100% green tests (unit, e2e integration, and Angular compile check). | Minor API property serialization differences breaking clients. |

**Prerequisites:** Backend compiling cleanly on a fresh git branch.
**Estimated effort:** ~1 session across 4 phases.

## Open Risks & Assumptions

* **Assumption:** The existing `backend/10x-cookbook-backend.http` integration tests are the canonical definition of successful API execution.
* **Risk:** Differences in C# JSON serialization names (camelCase vs PascalCase) if not automatically managed by standard controllers configuration. (This is resolved by relying on ASP.NET Core's default camelCase formatter).

## Success Criteria (Summary)

* Backend compiles cleanly and all 14 (plus newly added) backend unit tests pass with `dotnet test`.
* No regressions occur on the client: Angular client builds perfectly, and end-to-end recipe CRUD processes function seamlessly.
* All endpoints in `10x-cookbook-backend.http` respond with correct payloads and status codes.
