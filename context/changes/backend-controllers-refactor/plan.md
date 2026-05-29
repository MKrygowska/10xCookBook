# Backend Controllers Refactor — Implementation Plan

This plan details the migration of our backend Minimal APIs into classic ASP.NET Core `ControllerBase` classes. It enforces strict separation of concerns by moving all database queries, validation checks, and transformation logic into services (`RecipeService.cs`, `UserService.cs`, and a new `IngredientService.cs`), and throwing domain-specific exceptions which controllers catch explicitly inline to yield backward-compatible HTTP responses.

## Overview

Transitioning from static minimal endpoints mapped directly in `Program.cs` to standard MVC controllers ensures a highly maintainable, scalable, and idiomatic C# backend architecture. The controllers will remain ultra-slim, handling routing and HTTP-specific logic, while all business rules and database manipulations reside in domain services.

## Current State Analysis

Currently, our backend exposes endpoints through three minimal API modules in the `backend/Endpoints/` directory:
- `UserEndpoints.cs`: Handles user deletion (`DELETE /api/users/me`), mapping directly to `UserService`.
- `AuthEndpoints.cs`: Handles registration and login, defining local request DTO records and handling basic parameter verification before calling `UserService`.
- `RecipeEndpoints.cs`: Contains extensive inline Entity Framework Core queries and updates (fetching private recipes, validation, database writes for new/updated recipes, duplicates checking). It directly communicates with `AppDbContext` and operates on domain models.

All endpoint mappings are registered in `Program.cs` via static extension methods.

## Desired End State

* All legacy Minimal API endpoints (`backend/Endpoints/`) are deleted.
* A standard MVC Controller layout is introduced in a new `backend/Controllers/` directory:
  * `AuthController.cs` handles public authentication endpoints (`POST /api/auth/register`, `POST /api/auth/login`).
  * `UserController.cs` handles user account actions (`DELETE /api/users/me`).
  * `IngredientController.cs` handles listing ingredients (`GET /api/ingredients`).
  * `RecipeController.cs` handles recipe matching and CRUD (`POST /api/recipes/match`, `GET /api/recipes/my`, `POST /api/recipes`, `PUT /api/recipes/{id}`, `DELETE /api/recipes/{id}`).
* All controllers inherit from `BaseApiController.cs` (located in `backend/Controllers/`) which maps routes (`api/[controller]`) and offers a clean helper to extract the authenticated user's ID as a `Guid`.
* A new `IngredientService.cs` manages ingredient lookup.
* `RecipeService.cs` is fully extended to handle private recipe retrieval, recipe creation, update, and deletion, including database integration, validation logic, and updating user activity.
* Proper domain exceptions (`ValidationException`, `NotFoundException`, `ForbiddenException`) are defined. Services throw these exceptions, and controllers catch them inside explicit `try-catch` blocks to return backward-compatible HTTP responses, ensuring no changes to the frontend contract.

### Key Discoveries:
- Minimal API endpoints in `RecipeEndpoints.cs` contain raw Linq operations like `.Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient).Where(...)`. These must be moved intact to service queries.
- `UserService.UpdateUserActivity(Guid userId)` is called during recipe CRUD operations. Services should perform this update rather than controllers.
- Backend integration tests are represented in `backend/10x-cookbook-backend.http`. Maintaining backward compatibility is critical for this file and the Angular frontend.

## What We're NOT Doing

* We are NOT changing any database schemas or migrations.
* We are NOT changing the JWT token generation, issuer, audience, or security details.
* We are NOT changing the client-side Angular code.
* We are NOT introducing an external exception filter or global middleware since explicit, inline `try-catch` blocks are requested.

## Implementation Approach

* **Incremental Build**: Create exceptions, DTOs, and services first, then compile and test them via C# unit tests. Once the service layer is robust, create the controller layer and rewire `Program.cs`.
* **Zero-Breakage Contract**: Return identical JSON payload schemas and HTTP status codes (400, 401, 403, 404, 200, 201, 204) for all API interactions to match current clients exactly.

---

## Phase 1: Domain Exceptions, DTOs Setup & Services Configuration

### Overview
Create the domain exceptions, centralize request/response DTO structures in a dedicated folder, and configure service bindings in `Program.cs`.

### Changes Required:

#### 1. Custom Exceptions
- **File**: `backend/Exceptions/ValidationException.cs` [NEW]
- **Intent**: Exception thrown when parameter validations or business rule validations fail.
- **Contract**: Inherits from `Exception`. Accepts a string message.

- **File**: `backend/Exceptions/NotFoundException.cs` [NEW]
- **Intent**: Exception thrown when a requested resource (like a recipe or user) is not found.
- **Contract**: Inherits from `Exception`. Accepts a string message.

- **File**: `backend/Exceptions/ForbiddenException.cs` [NEW]
- **Intent**: Exception thrown when an authenticated user attempts to access or modify a resource they do not own.
- **Contract**: Inherits from `Exception`. Accepts a string message.

#### 2. DTO Layout
- **File**: `backend/DTOs/AuthDtos.cs` [NEW]
- **Intent**: Houses authentication request payloads.
- **Contract**:
  ```csharp
  using System.ComponentModel.DataAnnotations;

  namespace _10x_cookbook_backend.DTOs
  {
      public record RegisterRequest(
          [Required(ErrorMessage = "E-mail jest wymagany.")]
          [EmailAddress(ErrorMessage = "Niepoprawny format e-mail.")]
          string Email,
          
          [Required(ErrorMessage = "Hasło jest wymagane.")]
          [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
          string Password
      );

      public record LoginRequest(
          [Required(ErrorMessage = "E-mail jest wymagany.")]
          string Email,
          
          [Required(ErrorMessage = "Hasło jest wymagane.")]
          string Password
      );
  }
  ```

- **File**: `backend/DTOs/RecipeDtos.cs` [NEW]
- **Intent**: Houses recipe requests and backward-compatible response schemas.
- **Contract**:
  * `public record MatchRecipesRequest(List<string> Ingredients);`
  * `public record RecipeIngredientRequest(Guid IngredientId, string Quantity);`
  * `public record CreateRecipeRequest([Required] string Title, [Required] string Instructions, List<RecipeIngredientRequest>? Ingredients);`
  * `public record UpdateRecipeRequest([Required] string Title, [Required] string Instructions, List<RecipeIngredientRequest>? Ingredients);`
  * `public record RecipeResponseDto(Guid Id, string Title, string Instructions, bool IsPublic, List<RecipeIngredientResponseDto> Ingredients);`
  * `public record RecipeIngredientResponseDto(Guid IngredientId, string Name, string Quantity);`
  * `public record CreateRecipeResponseDto(Guid Id, string Title, string Instructions, bool IsPublic, List<CreateRecipeIngredientResponseDto> Ingredients);`
  * `public record CreateRecipeIngredientResponseDto(Guid IngredientId, string Quantity);`

- **File**: `backend/DTOs/IngredientDtos.cs` [NEW]
- **Intent**: Houses ingredient response schema.
- **Contract**:
  * `public record IngredientResponseDto(Guid Id, string Name, bool IsSpiceOrStaple);`

#### 3. Ingredient Service Setup
- **File**: `backend/Services/IngredientService.cs` [NEW]
- **Intent**: Scaffolds a dedicated service for handling ingredient database lookups.
- **Contract**:
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using _10x_cookbook_backend.Data;
  using _10x_cookbook_backend.DTOs;

  namespace _10x_cookbook_backend.Services
  {
      public class IngredientService
      {
          private readonly AppDbContext _dbContext;

          public IngredientService(AppDbContext dbContext)
          {
              _dbContext = dbContext;
          }

          public async Task<List<IngredientResponseDto>> GetIngredientsAsync()
          {
              return await _dbContext.Ingredients
                  .AsNoTracking()
                  .OrderBy(i => i.Name)
                  .Select(i => new IngredientResponseDto(i.Id, i.Name, i.IsSpiceOrStaple))
                  .ToListAsync();
          }
      }
  }
  ```

#### 4. Program.cs Wired
- **File**: `backend/Program.cs` [MODIFY]
- **Intent**: Register the new `IngredientService` in the dependency injection container.
- **Contract**: Inject `IngredientService` under the existing services block:
  `builder.Services.AddScoped<IngredientService>();`

### Success Criteria:

#### Automated Verification:
- Project compiles cleanly: `dotnet build`

---

## Phase 2: Service Layer Refactoring & Unit Tests Expansion

### Overview
Extract the queries, validations, and state changes from `RecipeEndpoints.cs` into `RecipeService.cs`, throw exceptions on violations, and cover them completely under `RecipeServiceTests.cs`.

### Changes Required:

#### 1. Recipe Service Methods Extension
- **File**: `backend/Services/RecipeService.cs` [MODIFY]
- **Intent**: Implement service methods to query and modify private recipes, validating requests and updating user activity.
- **Contract**:
  Add the following asynchronous methods:
  * `public async Task<List<RecipeResponseDto>> GetMyRecipesAsync(Guid userId)`: Returns private recipes belonging to `userId`.
  * `public async Task<CreateRecipeResponseDto> CreateRecipeAsync(Guid userId, CreateRecipeRequest request)`: Validates duplicates and existing ingredients. Adds recipe to `AppDbContext`, triggers `UserService.UpdateUserActivity(userId)`, saves, and returns `CreateRecipeResponseDto`. Throws `ValidationException` on duplicates or invalid ingredients.
  * `public async Task<CreateRecipeResponseDto> UpdateRecipeAsync(Guid id, Guid userId, UpdateRecipeRequest request)`: Checks if recipe exists (throws `NotFoundException`). Verifies owner is `userId` (throws `ForbiddenException`). Clears old ingredients, validates new ingredients, updates recipe, triggers `UserService.UpdateUserActivity(userId)`, saves, and returns `CreateRecipeResponseDto`.
  * `public async Task DeleteRecipeAsync(Guid id, Guid userId)`: Checks if recipe exists (throws `NotFoundException`). Verifies owner is `userId` (throws `ForbiddenException`). Removes recipe, triggers `UserService.UpdateUserActivity(userId)`, saves.

#### 2. Unit Tests Verification
- **File**: `backend/Tests/RecipeServiceTests.cs` [MODIFY]
- **Intent**: Add robust test cases to verify private recipe CRUD, including successful scenarios and exception triggers.
- **Contract**:
  Add tests matching these behaviors:
  * `GetMyRecipes_ShouldReturnCorrectRecipes`
  * `CreateRecipe_WithDuplicateIngredients_ShouldThrowValidationException`
  * `CreateRecipe_WithInvalidIngredients_ShouldThrowValidationException`
  * `CreateRecipe_ShouldSucceed`
  * `UpdateRecipe_NonExistent_ShouldThrowNotFoundException`
  * `UpdateRecipe_DifferentUser_ShouldThrowForbiddenException`
  * `UpdateRecipe_ShouldSucceed`
  * `DeleteRecipe_NonExistent_ShouldThrowNotFoundException`
  * `DeleteRecipe_DifferentUser_ShouldThrowForbiddenException`
  * `DeleteRecipe_ShouldSucceed`

### Success Criteria:

#### Automated Verification:
- Unit tests pass cleanly: `dotnet test`

---

## Phase 3: Controller Layer Implementation & Endpoint Cleanup (Clean Cut)

### Overview
Create the standard controllers, use explicit inline `try-catch` blocks to translate custom domain exceptions into matching backward-compatible HTTP responses, wire them in `Program.cs`, and delete the old endpoints directory.

### Changes Required:

#### 1. Base API Controller
- **File**: `backend/Controllers/BaseApiController.cs` [NEW]
- **Intent**: Abstract common route metadata and authentication helper routines for all API controllers.
- **Contract**:
  ```csharp
  using System.Security.Claims;
  using Microsoft.AspNetCore.Mvc;

  namespace _10x_cookbook_backend.Controllers
  {
      [ApiController]
      [Route("api/[controller]")]
      public abstract class BaseApiController : ControllerBase
      {
          protected Guid GetUserId()
          {
              var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
              if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
              {
                  throw new UnauthorizedAccessException("Użytkownik nie jest autoryzowany.");
              }
              return userId;
          }

          protected Guid? TryGetUserId()
          {
              var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
              if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
              {
                  return userId;
              }
              return null;
          }
      }
  }
  ```

#### 2. Auth Controller
- **File**: `backend/Controllers/AuthController.cs` [NEW]
- **Intent**: Standard controller representing login and registration endpoints.
- **Contract**:
  Inherits from `BaseApiController`. Annotated with `[AllowAnonymous]` since authentication endpoints are public.
  * `POST api/auth/register`: Binds `[FromBody] RegisterRequest request`. Checks model validity. Calls `UserService.Register` and `UserService.Login`. Catch exceptions inline, returning identical payload formats.
  * `POST api/auth/login`: Binds `[FromBody] LoginRequest request`. Calls `UserService.Login`. Inline `try-catch` maps credentials failures to `400 BadRequest`.

#### 3. User Controller
- **File**: `backend/Controllers/UserController.cs` [NEW]
- **Intent**: Controller representing user profile actions.
- **Contract**:
  Inherits from `BaseApiController`. Annotated with `[Authorize]`.
  * `DELETE api/users/me`: Calls `GetUserId()` and deletes the current user via `UserService.DeleteUser`. Catch errors inline, returning `204 NoContent` or `400 BadRequest`.

#### 4. Ingredient Controller
- **File**: `backend/Controllers/IngredientsController.cs` [NEW]
- **Intent**: Controller representing ingredient listing actions.
- **Contract**:
  Inherits from `BaseApiController`. Annotated with `[Authorize]`.
  * `GET api/ingredients`: Calls `IngredientService.GetIngredientsAsync()`. Return `200 Ok` with payload.

#### 5. Recipe Controller
- **File**: `backend/Controllers/RecipesController.cs` [NEW]
- **Intent**: Controller representing recipe matching and CRUD operations.
- **Contract**:
  Inherits from `BaseApiController`. Annotated with `[Authorize]`.
  * `POST api/recipes/match`: Calls `TryGetUserId()` and `RecipeService.MatchRecipesAsync`.
  * `GET api/recipes/my`: Calls `GetUserId()` and `RecipeService.GetMyRecipesAsync`.
  * `POST api/recipes`: Binds `[FromBody] CreateRecipeRequest request`. Calls `GetUserId()` and `RecipeService.CreateRecipeAsync`. Handles `try-catch` mapping to return `201 Created` or `400 BadRequest`.
  * `PUT api/recipes/{id:guid}`: Binds `Guid id` and `[FromBody] UpdateRecipeRequest request`. Calls `GetUserId()` and `RecipeService.UpdateRecipeAsync`. Inline `try-catch` intercepts exceptions:
    * `ValidationException` -> `BadRequest(new { error = ex.Message })`
    * `NotFoundException` -> `NotFound(new { error = ex.Message })`
    * `ForbiddenException` -> `StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message })`
  * `DELETE api/recipes/{id:guid}`: Binds `Guid id`. Calls `GetUserId()` and `RecipeService.DeleteRecipeAsync`. Inline `try-catch` handles custom exceptions returning `204 NoContent`, `404 NotFound`, or `403 Forbidden`.

#### 6. Program.cs Controller Activation
- **File**: `backend/Program.cs` [MODIFY]
- **Intent**: Wire controllers into the application builder and routing engine. Remove the Minimal API maps entirely.
- **Contract**:
  * Add `builder.Services.AddControllers();` in service collection configuration.
  * Add `app.MapControllers();` in routing mapping section.
  * Delete `app.MapAuthEndpoints();`, `app.MapRecipeEndpoints();`, `app.MapUserEndpoints();` from `Program.cs`.
  * Remove `using _10x_cookbook_backend.Endpoints;`.

#### 7. Clean-Cut Removal
- **File**: `backend/Endpoints/` [DELETE]
- **Intent**: Delete the entire directory containing Minimal API static setup to complete the clean-cut migration.
- **Contract**: Remove `AuthEndpoints.cs`, `RecipeEndpoints.cs`, and `UserEndpoints.cs`.

### Success Criteria:

#### Automated Verification:
- Backend compiles cleanly: `dotnet build`

---

## Phase 4: Verification and Zero-Regression Testing

### Overview
Execute tests across both the API environment and the frontend to confirm total system integrity.

### Success Criteria:

#### Automated Verification:
- All unit tests pass cleanly: `dotnet test`
- All HTTP integration tests inside `backend/10x-cookbook-backend.http` pass perfectly when calling the API backend.
- Angular frontend client compiles without errors.

#### Manual Verification:
- Launch the backend API using `dotnet run` inside `backend/`.
- Launch the frontend application using `npm run start` inside `frontend/`.
- Access the web interface, register a new account, log in, perform recipe matching, add a new recipe, edit it, list private recipes, and delete a recipe to ensure complete operational success.

---

## Testing Strategy

### Unit Tests:
- Ensure all legacy tests inside `RecipeServiceTests.cs` and `UserServiceTests.cs` remain intact.
- Verify CRUD scenarios on the refactored services using the new XUnit test methods.

### Integration Tests:
- Leverage the integration HTTP endpoints file `backend/10x-cookbook-backend.http` to test all authentication and matching routes locally.

### Manual Testing Steps:
1. Compile and launch the application backend (`dotnet run` or `dotnet watch`).
2. Interact with the Swagger UI at `https://localhost:7198/swagger/index.html` to confirm routing definitions are fully exported.
3. Perform the recipe CRUD cycle on the UI client and verify data synchronization.

---

## Performance Considerations

* All database reads that are read-only will employ `.AsNoTracking()` to improve LINQ execution time and reduce memory footprints in Entity Framework.

---

## References

- Legacy Minimal API mappings: `backend/Endpoints/RecipeEndpoints.cs`
- Service contracts baseline: `backend/Services/RecipeService.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Custom Exceptions, DTOs Setup & Services Configuration

#### Automated

- [x] 1.1 Create custom Exception classes (`ValidationException.cs`, `NotFoundException.cs`, `ForbiddenException.cs`) in `backend/Exceptions/` — 5d3fc3d
- [x] 1.2 Create centralized DTO records in `backend/DTOs/AuthDtos.cs`, `backend/DTOs/RecipeDtos.cs`, and `backend/DTOs/IngredientDtos.cs` — 5d3fc3d
- [x] 1.3 Implement new `IngredientService.cs` in `backend/Services/` — 5d3fc3d
- [x] 1.4 Register the new `IngredientService` in `backend/Program.cs` and verify compilation — 5d3fc3d

### Phase 2: Service Layer Refactoring & Unit Tests Expansion

#### Automated

- [x] 2.1 Refactor and extend `RecipeService.cs` to handle private recipe retrieval, creation, update, and deletion, including domain exception throwing — 2aa44ee
- [x] 2.2 Write comprehensive XUnit test cases in `backend/Tests/RecipeServiceTests.cs` checking CRUD and error states — 2aa44ee
- [x] 2.3 Run tests and verify they pass cleanly (`dotnet test`) — 2aa44ee

### Phase 3: Controller Layer Implementation & Endpoint Cleanup (Clean Cut)

#### Automated

- [x] 3.1 Create `BaseApiController.cs` in `backend/Controllers/` to hold shared claims parser and ApiController annotations — b887f3f
- [x] 3.2 Implement `AuthController.cs` handling login and registration with inline try-catch mappings — b887f3f
- [x] 3.3 Implement `UserController.cs` handling user deletion with inline try-catch mappings — b887f3f
- [x] 3.4 Implement `IngredientController.cs` handling ingredient listings with inline try-catch mappings — b887f3f
- [x] 3.5 Implement `RecipeController.cs` handling matching and CRUD with inline try-catch mappings mapping exceptions to exact HTTP status codes — b887f3f
- [x] 3.6 Register Controllers in `backend/Program.cs` and delete static minimal API mappings — b887f3f
- [x] 3.7 Delete the entire `backend/Endpoints/` legacy folder and verify compilation — b887f3f

### Phase 4: Verification and Zero-Regression Testing

#### Automated

- [x] 4.1 Run all unit tests and verify 100% success (`dotnet test`)
- [x] 4.2 Validate backend routing using integration tests in `10x-cookbook-backend.http`
- [x] 4.3 Verify compilation of the frontend application (`npm run build` inside `frontend/`)

#### Manual

- [x] 4.4 Verify full recipe creation, list, update, and deletion workflows end-to-end through the web client interface
