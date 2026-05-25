using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Services;

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

            app.MapPost("/api/recipes/match", async ([FromBody] MatchRecipesRequest request, RecipeService recipeService) =>
            {
                if (request == null || request.Ingredients == null)
                {
                    return Results.BadRequest(new { error = "Lista składników jest wymagana." });
                }

                var matchedRecipes = await recipeService.MatchRecipesAsync(request.Ingredients);
                return Results.Ok(matchedRecipes);
            })
            .RequireAuthorization();
        }
    }

    public record MatchRecipesRequest(List<string> Ingredients);
}
