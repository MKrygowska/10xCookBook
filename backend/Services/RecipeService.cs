using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;

namespace _10x_cookbook_backend.Services
{
    public class RecipeService
    {
        private readonly AppDbContext _dbContext;

        public RecipeService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RecipeMatchResult>> MatchRecipesAsync(List<string> userIngredientNames)
        {
            if (userIngredientNames == null)
            {
                return new List<RecipeMatchResult>();
            }

            // Normalize input: trim and lowercase
            var normalizedUserIngredients = userIngredientNames
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim().ToLower())
                .ToHashSet();

            // Resolve matching ingredients from names at the DB level
            var matchingIngredients = await _dbContext.Ingredients
                .AsNoTracking()
                .Where(i => normalizedUserIngredients.Contains(i.Name.ToLower()))
                .Select(i => new { i.Id, i.IsSpiceOrStaple })
                .ToListAsync();

            var matchingIngredientIds = matchingIngredients.Select(mi => mi.Id).ToList();
            var primaryIngredientIds = matchingIngredients.Where(mi => !mi.IsSpiceOrStaple).Select(mi => mi.Id).ToList();

            // Fetch public recipes, pre-filtered to those containing at least one matched primary ingredient.
            // If only spices were entered, fall back to matching any ingredient.
            var filterIds = primaryIngredientIds.Any() ? primaryIngredientIds : matchingIngredientIds;

            // Fetch public recipes with ingredients eager loaded, pre-filtered by matching ingredient IDs
            var publicRecipes = await _dbContext.Recipes
                .AsNoTracking()
                .Where(r => r.IsPublic && r.RecipeIngredients.Any(ri => filterIds.Contains(ri.IngredientId)))
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();

            var matchedRecipes = new List<RecipeMatchResult>();

            foreach (var recipe in publicRecipes)
            {
                var recipeIngredients = recipe.RecipeIngredients.ToList();
                if (!recipeIngredients.Any()) continue;

                var matched = new List<string>();
                var missing = new List<MissingIngredientResultDto>();

                double matchedWeight = 0;
                double totalWeight = 0;
                int missingPrimaryCount = 0;

                foreach (var ri in recipeIngredients)
                {
                    if (ri.Ingredient == null) continue;

                    var name = ri.Ingredient.Name.Trim().ToLower();
                    var isSpice = ri.Ingredient.IsSpiceOrStaple;
                    double weight = isSpice ? 0.1 : 1.0;

                    totalWeight += weight;

                    if (normalizedUserIngredients.Contains(name))
                    {
                        matched.Add(ri.Ingredient.Name); // Keep original casing from DB seed
                        matchedWeight += weight;
                    }
                    else
                    {
                        missing.Add(new MissingIngredientResultDto(ri.Ingredient.Name, isSpice, ri.Quantity));
                        if (!isSpice)
                        {
                            missingPrimaryCount++;
                        }
                    }
                }

                // Compute weighted match percentage rounded to nearest integer
                double matchRateDouble = totalWeight > 0 ? (matchedWeight / totalWeight) * 100 : 0;
                int matchRate = (int)Math.Round(matchRateDouble);

                // We only want recipes that have at least one matched ingredient (matchRate > 0)
                if (matchRate > 0)
                {
                    matchedRecipes.Add(new RecipeMatchResult(
                        recipe.Id,
                        recipe.Title,
                        recipe.Instructions,
                        matchRate,
                        matched,
                        missing,
                        missingPrimaryCount
                    ));
                }
            }

            // Sorting:
            // 1. MatchRate descending
            // 2. MissingPrimaryCount ascending (tie-breaker)
            // 3. Title ascending
            return matchedRecipes
                .OrderByDescending(r => r.MatchRate)
                .ThenBy(r => r.MissingPrimaryCount)
                .ThenBy(r => r.Title)
                .Take(20)
                .ToList();
        }
    }

    public record MissingIngredientResultDto(string Name, bool IsSpiceOrStaple, string Quantity);

    public record RecipeMatchResult(
        Guid Id,
        string Title,
        string Instructions,
        int MatchRate,
        List<string> MatchedIngredients,
        List<MissingIngredientResultDto> MissingIngredients,
        int MissingPrimaryCount
    );
}
