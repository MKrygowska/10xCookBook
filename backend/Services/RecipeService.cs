using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;
using _10x_cookbook_backend.DTOs;
using _10x_cookbook_backend.Exceptions;

namespace _10x_cookbook_backend.Services
{
    public class RecipeService
    {
        private readonly AppDbContext _dbContext;
        private readonly UserService? _userService;

        public RecipeService(AppDbContext dbContext, UserService? userService = null)
        {
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task<List<RecipeMatchResult>> MatchRecipesAsync(List<string> userIngredientNames, Guid? userId = null)
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

            // Fetch public and current user's private recipes with ingredients eager loaded, pre-filtered by matching ingredient IDs
            var recipes = await _dbContext.Recipes
                .AsNoTracking()
                .Where(r => (r.IsPublic || (userId != null && r.UserId == userId)) && r.RecipeIngredients.Any(ri => filterIds.Contains(ri.IngredientId)))
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();

            var matchedRecipes = new List<RecipeMatchResult>();

            foreach (var recipe in recipes)
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
                        recipe.IsPublic,
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

        public async Task<List<RecipeResponseDto>> GetMyRecipesAsync(Guid userId)
        {
            return await _dbContext.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Where(r => r.UserId == userId)
                .Select(r => new RecipeResponseDto(
                    r.Id,
                    r.Title,
                    r.Instructions,
                    r.IsPublic,
                    r.RecipeIngredients.Select(ri => new RecipeIngredientResponseDto(
                        ri.IngredientId,
                        ri.Ingredient != null ? ri.Ingredient.Name : string.Empty,
                        ri.Quantity
                    )).ToList()
                ))
                .ToListAsync();
        }

        public async Task<CreateRecipeResponseDto> CreateRecipeAsync(Guid userId, CreateRecipeRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("Żądanie nie może być puste.");
            }

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions))
            {
                throw new ValidationException("Tytuł i instrukcje są wymagane.");
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
                if (request.Ingredients.Select(ri => ri.IngredientId).Distinct().Count() != request.Ingredients.Count)
                {
                    throw new ValidationException("Lista składników zawiera powtarzające się pozycje.");
                }

                var reqIngredientIds = request.Ingredients.Select(ri => ri.IngredientId).ToList();
                var validIngredientIds = await _dbContext.Ingredients
                    .Where(i => reqIngredientIds.Contains(i.Id))
                    .Select(i => i.Id)
                    .ToListAsync();

                if (validIngredientIds.Count != reqIngredientIds.Distinct().Count())
                {
                    throw new ValidationException("Jeden lub więcej składników jest niepoprawnych.");
                }

                foreach (var reqIng in request.Ingredients)
                {
                    if (string.IsNullOrWhiteSpace(reqIng.Quantity))
                    {
                        throw new ValidationException("Ilość składnika nie może być pusta.");
                    }
                    recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = reqIng.IngredientId,
                        Quantity = reqIng.Quantity.Trim()
                    });
                }
            }

            _dbContext.Recipes.Add(recipe);
            await _dbContext.SaveChangesAsync();

            _userService?.UpdateUserActivity(userId);

            return new CreateRecipeResponseDto(
                recipe.Id,
                recipe.Title,
                recipe.Instructions,
                recipe.IsPublic,
                recipe.RecipeIngredients.Select(ri => new CreateRecipeIngredientResponseDto(
                    ri.IngredientId,
                    ri.Quantity
                )).ToList()
            );
        }

        public async Task<CreateRecipeResponseDto> UpdateRecipeAsync(Guid id, Guid userId, UpdateRecipeRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("Żądanie nie może być puste.");
            }

            var recipe = await _dbContext.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                throw new NotFoundException("Nie znaleziono przepisu.");
            }

            if (recipe.UserId != userId)
            {
                throw new ForbiddenException("Brak uprawnień do edycji tego przepisu.");
            }

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions))
            {
                throw new ValidationException("Tytuł i instrukcje są wymagane.");
            }

            recipe.Title = request.Title.Trim();
            recipe.Instructions = request.Instructions.Trim();

            recipe.RecipeIngredients.Clear();

            if (request.Ingredients != null && request.Ingredients.Any())
            {
                if (request.Ingredients.Select(ri => ri.IngredientId).Distinct().Count() != request.Ingredients.Count)
                {
                    throw new ValidationException("Lista składników zawiera powtarzające się pozycje.");
                }

                var reqIngredientIds = request.Ingredients.Select(ri => ri.IngredientId).ToList();
                var validIngredientIds = await _dbContext.Ingredients
                    .Where(i => reqIngredientIds.Contains(i.Id))
                    .Select(i => i.Id)
                    .ToListAsync();

                if (validIngredientIds.Count != reqIngredientIds.Distinct().Count())
                {
                    throw new ValidationException("Jeden lub więcej składników jest niepoprawnych.");
                }

                foreach (var reqIng in request.Ingredients)
                {
                    if (string.IsNullOrWhiteSpace(reqIng.Quantity))
                    {
                        throw new ValidationException("Ilość składnika nie może być pusta.");
                    }
                    recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = reqIng.IngredientId,
                        Quantity = reqIng.Quantity.Trim()
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            _userService?.UpdateUserActivity(userId);

            return new CreateRecipeResponseDto(
                recipe.Id,
                recipe.Title,
                recipe.Instructions,
                recipe.IsPublic,
                recipe.RecipeIngredients.Select(ri => new CreateRecipeIngredientResponseDto(
                    ri.IngredientId,
                    ri.Quantity
                )).ToList()
            );
        }

        public async Task DeleteRecipeAsync(Guid id, Guid userId)
        {
            var recipe = await _dbContext.Recipes.FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                throw new NotFoundException("Nie znaleziono przepisu.");
            }

            if (recipe.UserId != userId)
            {
                throw new ForbiddenException("Brak uprawnień do usunięcia tego przepisu.");
            }

            _dbContext.Recipes.Remove(recipe);
            await _dbContext.SaveChangesAsync();

            _userService?.UpdateUserActivity(userId);
        }
    }

    public record MissingIngredientResultDto(string Name, bool IsSpiceOrStaple, string Quantity);

    public record RecipeMatchResult(
        Guid Id,
        string Title,
        string Instructions,
        bool IsPublic,
        int MatchRate,
        List<string> MatchedIngredients,
        List<MissingIngredientResultDto> MissingIngredients,
        int MissingPrimaryCount
    );
}
