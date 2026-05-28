using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Services;
using _10x_cookbook_backend.Models;
using System.Security.Claims;

namespace _10x_cookbook_backend.Endpoints
{
    public static class RecipeEndpoints
    {
        public static void MapRecipeEndpoints(this WebApplication app)
        {
            app.MapGet("/api/ingredients", async (AppDbContext dbContext) =>
            {
                var ingredients = await dbContext.Ingredients
                    .AsNoTracking()
                    .OrderBy(i => i.Name)
                    .Select(i => new { i.Id, i.Name, i.IsSpiceOrStaple })
                    .ToListAsync();

                return Results.Ok(ingredients);
            })
            .RequireAuthorization();

            app.MapPost("/api/recipes/match", async (
                ClaimsPrincipal user,
                [FromBody] MatchRecipesRequest request, 
                RecipeService recipeService) =>
            {
                if (request == null || request.Ingredients == null)
                {
                    return Results.BadRequest(new { error = "Lista składników jest wymagana." });
                }

                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                {
                    userId = parsedId;
                }

                var matchedRecipes = await recipeService.MatchRecipesAsync(request.Ingredients, userId);
                return Results.Ok(matchedRecipes);
            })
            .RequireAuthorization();

            app.MapGet("/api/recipes/my", async (ClaimsPrincipal user, AppDbContext dbContext) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var recipes = await dbContext.Recipes
                    .AsNoTracking()
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .Where(r => r.UserId == userId)
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.Instructions,
                        r.IsPublic,
                        Ingredients = r.RecipeIngredients.Select(ri => new
                        {
                            ri.IngredientId,
                            Name = ri.Ingredient != null ? ri.Ingredient.Name : string.Empty,
                            ri.Quantity
                        }).ToList()
                    })
                    .ToListAsync();

                return Results.Ok(recipes);
            })
            .RequireAuthorization();

            app.MapPost("/api/recipes", async (
                ClaimsPrincipal user, 
                [FromBody] CreateRecipeRequest request, 
                AppDbContext dbContext) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions))
                {
                    return Results.BadRequest(new { error = "Tytuł i instrukcje są wymagane." });
                }

                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title.Trim(),
                    Instructions = request.Instructions.Trim(),
                    IsPublic = false, // Always private for user-created recipes
                    UserId = userId,
                    RecipeIngredients = new List<RecipeIngredient>()
                };

                if (request.Ingredients != null && request.Ingredients.Any())
                {
                    var reqIngredientIds = request.Ingredients.Select(ri => ri.IngredientId).ToList();
                    var validIngredientIds = await dbContext.Ingredients
                        .Where(i => reqIngredientIds.Contains(i.Id))
                        .Select(i => i.Id)
                        .ToListAsync();

                    if (validIngredientIds.Count != reqIngredientIds.Distinct().Count())
                    {
                        return Results.BadRequest(new { error = "Jeden lub więcej składników jest niepoprawnych." });
                    }

                    foreach (var reqIng in request.Ingredients)
                    {
                        recipe.RecipeIngredients.Add(new RecipeIngredient
                        {
                            RecipeId = recipe.Id,
                            IngredientId = reqIng.IngredientId,
                            Quantity = reqIng.Quantity.Trim()
                        });
                    }
                }

                dbContext.Recipes.Add(recipe);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/recipes/{recipe.Id}", new
                {
                    recipe.Id,
                    recipe.Title,
                    recipe.Instructions,
                    recipe.IsPublic,
                    Ingredients = recipe.RecipeIngredients.Select(ri => new
                    {
                        ri.IngredientId,
                        ri.Quantity
                    }).ToList()
                });
            })
            .RequireAuthorization();

            app.MapPut("/api/recipes/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user, 
                [FromBody] UpdateRecipeRequest request, 
                AppDbContext dbContext) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var recipe = await dbContext.Recipes
                    .Include(r => r.RecipeIngredients)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                {
                    return Results.NotFound(new { error = "Nie znaleziono przepisu." });
                }

                if (recipe.UserId != userId)
                {
                    return Results.Json(new { error = "Brak uprawnień do edycji tego przepisu." }, statusCode: StatusCodes.Status403Forbidden);
                }

                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions))
                {
                    return Results.BadRequest(new { error = "Tytuł i instrukcje są wymagane." });
                }

                recipe.Title = request.Title.Trim();
                recipe.Instructions = request.Instructions.Trim();

                // Update ingredients (Clear old ones and add new ones)
                recipe.RecipeIngredients.Clear();

                if (request.Ingredients != null && request.Ingredients.Any())
                {
                    var reqIngredientIds = request.Ingredients.Select(ri => ri.IngredientId).ToList();
                    var validIngredientIds = await dbContext.Ingredients
                        .Where(i => reqIngredientIds.Contains(i.Id))
                        .Select(i => i.Id)
                        .ToListAsync();

                    if (validIngredientIds.Count != reqIngredientIds.Distinct().Count())
                    {
                        return Results.BadRequest(new { error = "Jeden lub więcej składników jest niepoprawnych." });
                    }

                    foreach (var reqIng in request.Ingredients)
                    {
                        recipe.RecipeIngredients.Add(new RecipeIngredient
                        {
                            RecipeId = recipe.Id,
                            IngredientId = reqIng.IngredientId,
                            Quantity = reqIng.Quantity.Trim()
                        });
                    }
                }

                await dbContext.SaveChangesAsync();
                return Results.Ok(new
                {
                    recipe.Id,
                    recipe.Title,
                    recipe.Instructions,
                    recipe.IsPublic,
                    Ingredients = recipe.RecipeIngredients.Select(ri => new
                    {
                        ri.IngredientId,
                        ri.Quantity
                    }).ToList()
                });
            })
            .RequireAuthorization();

            app.MapDelete("/api/recipes/{id:guid}", async (
                Guid id,
                ClaimsPrincipal user, 
                AppDbContext dbContext) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var recipe = await dbContext.Recipes.FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                {
                    return Results.NotFound(new { error = "Nie znaleziono przepisu." });
                }

                if (recipe.UserId != userId)
                {
                    return Results.Json(new { error = "Brak uprawnień do usunięcia tego przepisu." }, statusCode: StatusCodes.Status403Forbidden);
                }

                dbContext.Recipes.Remove(recipe);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            })
            .RequireAuthorization();
        }
    }

    public record MatchRecipesRequest(List<string> Ingredients);
    public record RecipeIngredientRequest(Guid IngredientId, string Quantity);
    public record CreateRecipeRequest(string Title, string Instructions, List<RecipeIngredientRequest>? Ingredients);
    public record UpdateRecipeRequest(string Title, string Instructions, List<RecipeIngredientRequest>? Ingredients);
}
