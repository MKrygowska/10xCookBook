using Microsoft.EntityFrameworkCore;
using Xunit;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;
using _10x_cookbook_backend.Services;
using _10x_cookbook_backend.DTOs;
using _10x_cookbook_backend.Exceptions;

namespace _10x_cookbook_backend.Tests
{
    public class RecipeServiceTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private void SeedTestData(AppDbContext dbContext)
        {
            // Seed Ingredients
            var pomidor = new Ingredient { Id = Guid.NewGuid(), Name = "pomidor", IsSpiceOrStaple = false };
            var cebula = new Ingredient { Id = Guid.NewGuid(), Name = "cebula", IsSpiceOrStaple = false };
            var czosnek = new Ingredient { Id = Guid.NewGuid(), Name = "czosnek", IsSpiceOrStaple = false };
            var makaron = new Ingredient { Id = Guid.NewGuid(), Name = "makaron", IsSpiceOrStaple = false };
            var oliwa = new Ingredient { Id = Guid.NewGuid(), Name = "oliwa z oliwek", IsSpiceOrStaple = true };
            var sol = new Ingredient { Id = Guid.NewGuid(), Name = "sól", IsSpiceOrStaple = true };

            dbContext.Ingredients.AddRange(pomidor, cebula, czosnek, makaron, oliwa, sol);

            // Seed Recipes
            // Recipe 1: Tomato Soup (pomidor [primary], oliwa z oliwek [spice])
            var recipe1 = new Recipe { Id = Guid.NewGuid(), Title = "Zupa pomidorowa", Instructions = "Gotuj...", IsPublic = true };
            // Recipe 2: Tomato Pasta (pomidor [primary], cebula [primary], makaron [primary], sól [spice])
            var recipe2 = new Recipe { Id = Guid.NewGuid(), Title = "Makaron z sosem", Instructions = "Gotuj...", IsPublic = true };
            // Recipe 3: Onion Soup (cebula [primary], czosnek [primary], sól [spice])
            var recipe3 = new Recipe { Id = Guid.NewGuid(), Title = "Zupa cebulowa", Instructions = "Gotuj...", IsPublic = true };

            dbContext.Recipes.AddRange(recipe1, recipe2, recipe3);

            // Seed RecipeIngredients
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipe1.Id, IngredientId = pomidor.Id, Quantity = "500g" },
                new RecipeIngredient { RecipeId = recipe1.Id, IngredientId = oliwa.Id, Quantity = "2 łyżki" },

                new RecipeIngredient { RecipeId = recipe2.Id, IngredientId = pomidor.Id, Quantity = "3 sztuki" },
                new RecipeIngredient { RecipeId = recipe2.Id, IngredientId = cebula.Id, Quantity = "1 sztuka" },
                new RecipeIngredient { RecipeId = recipe2.Id, IngredientId = makaron.Id, Quantity = "200g" },
                new RecipeIngredient { RecipeId = recipe2.Id, IngredientId = sol.Id, Quantity = "szczypta" },

                new RecipeIngredient { RecipeId = recipe3.Id, IngredientId = cebula.Id, Quantity = "2 sztuki" },
                new RecipeIngredient { RecipeId = recipe3.Id, IngredientId = czosnek.Id, Quantity = "1 ząbek" },
                new RecipeIngredient { RecipeId = recipe3.Id, IngredientId = sol.Id, Quantity = "szczypta" }
            );

            dbContext.SaveChanges();
        }

        [Fact]
        public async Task MatchRecipes_ShouldComputeWeightedMatchRateWithSpicesCorrectly()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            SeedTestData(dbContext);
            var recipeService = new RecipeService(dbContext);

            // Act 1: User has "pomidor" (primary) but misses "oliwa z oliwek" (spice/staple) for Zupa pomidorowa
            // Math for Zupa pomidorowa:
            // - Primary matched: 1.0 (pomidor)
            // - Spice matched: 0 (oliwa)
            // - Total weight: 1.0 (primary) + 0.1 (spice) = 1.1
            // - Match rate: (1.0 / 1.1) * 100 = 90.9% => 91%
            var results = await recipeService.MatchRecipesAsync(new List<string> { "pomidor" });

            // Assert
            var tomatoSoup = results.FirstOrDefault(r => r.Title == "Zupa pomidorowa");
            Assert.NotNull(tomatoSoup);
            Assert.Equal(91, tomatoSoup.MatchRate);
            Assert.Contains("pomidor", tomatoSoup.MatchedIngredients);
            var missingOliwa = tomatoSoup.MissingIngredients.FirstOrDefault(m => m.Name == "oliwa z oliwek");
            Assert.NotNull(missingOliwa);
            Assert.True(missingOliwa.IsSpiceOrStaple);

            // Act 2: User has only "oliwa z oliwek" (spice) for Zupa pomidorowa
            // Math for Zupa pomidorowa:
            // - Matched weight: 0.1 (oliwa)
            // - Total weight: 1.1
            // - Match rate: (0.1 / 1.1) * 100 = 9.09% => 9%
            var results2 = await recipeService.MatchRecipesAsync(new List<string> { "oliwa z oliwek" });

            // Assert
            var tomatoSoup2 = results2.FirstOrDefault(r => r.Title == "Zupa pomidorowa");
            Assert.NotNull(tomatoSoup2);
            Assert.Equal(9, tomatoSoup2.MatchRate);
        }

        [Fact]
        public async Task MatchRecipes_ShouldSortByMatchRateAndLeastMissingPrimaryIngredients()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            SeedTestData(dbContext);
            var recipeService = new RecipeService(dbContext);

            // Act: User has "cebula" and "sól"
            // Let's compute weights:
            // 1. Zupa pomidorowa: Total weight = 1.1. Matched = 0. MatchRate = 0% (excluded)
            // 2. Makaron z sosem: Primary = pomidor (1.0), cebula (1.0), makaron (1.0). Spice = sól (0.1). Total = 3.1
            //    Matched: cebula (1.0), sól (0.1). Matched weight = 1.1.
            //    MatchRate = (1.1 / 3.1) * 100 = 35.48% => 35%
            //    Missing primary: pomidor, makaron = 2 missing primary.
            // 3. Zupa cebulowa: Primary = cebula (1.0), czosnek (1.0). Spice = sól (0.1). Total = 2.1
            //    Matched: cebula (1.0), sól (0.1). Matched weight = 1.1.
            //    MatchRate = (1.1 / 2.1) * 100 = 52.38% => 52%
            //    Missing primary: czosnek = 1 missing primary.
            var results = await recipeService.MatchRecipesAsync(new List<string> { "cebula", "sól" });

            // Assert: Zupa cebulowa (52%) must be before Makaron z sosem (35%)
            Assert.Equal(2, results.Count);
            Assert.Equal("Zupa cebulowa", results[0].Title);
            Assert.Equal("Makaron z sosem", results[1].Title);

            // Act 2: Let's test a tie-breaker.
            // User has "pomidor" and "cebula"
            // 1. Zupa pomidorowa: Matched: pomidor (1.0). Total: 1.1. MatchRate = 1.0/1.1 = 91%. Missing primary = 0
            // 2. Makaron z sosem: Matched: pomidor (1.0), cebula (1.0). Total: 3.1. MatchRate = 2.0/3.1 = 65%. Missing primary = 1 (makaron)
            // 3. Zupa cebulowa: Matched: cebula (1.0). Total: 2.1. MatchRate = 1.0/2.1 = 48%. Missing primary = 1 (czosnek)
            var results2 = await recipeService.MatchRecipesAsync(new List<string> { "pomidor", "cebula" });
            Assert.Equal(3, results2.Count);
            Assert.Equal("Zupa pomidorowa", results2[0].Title);
            Assert.Equal("Makaron z sosem", results2[1].Title);
            Assert.Equal("Zupa cebulowa", results2[2].Title);
        }

        [Fact]
        public async Task MatchRecipes_ShouldNormalizeCaseAndSpaces()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            SeedTestData(dbContext);
            var recipeService = new RecipeService(dbContext);

            // Act: inputs with mixed casings and whitespace padding
            var results = await recipeService.MatchRecipesAsync(new List<string> { "   poMiDor  ", "  oLiwA Z oLiWeK   " });

            // Assert
            var tomatoSoup = results.FirstOrDefault(r => r.Title == "Zupa pomidorowa");
            Assert.NotNull(tomatoSoup);
            Assert.Equal(100, tomatoSoup.MatchRate);
            Assert.Empty(tomatoSoup.MissingIngredients);
        }

        [Fact]
        public async Task MatchRecipes_ShouldIncludePrivateRecipesOfCurrentUser()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            
            // Seed ingredients
            var kurczak = new Ingredient { Id = Guid.NewGuid(), Name = "kurczak", IsSpiceOrStaple = false };
            dbContext.Ingredients.Add(kurczak);
            
            // Seed a public recipe
            var publicRecipe = new Recipe { Id = Guid.NewGuid(), Title = "Publiczny Kurczak", IsPublic = true };
            dbContext.Recipes.Add(publicRecipe);
            dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = publicRecipe.Id, IngredientId = kurczak.Id, Quantity = "100g" });

            // Seed a private recipe belonging to the current user
            var userId = Guid.NewGuid();
            var privateRecipe = new Recipe { Id = Guid.NewGuid(), Title = "Prywatny Kurczak", IsPublic = false, UserId = userId };
            dbContext.Recipes.Add(privateRecipe);
            dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = privateRecipe.Id, IngredientId = kurczak.Id, Quantity = "200g" });

            dbContext.SaveChanges();
            
            var recipeService = new RecipeService(dbContext);

            // Act
            var results = await recipeService.MatchRecipesAsync(new List<string> { "kurczak" }, userId);

            // Assert
            Assert.Equal(2, results.Count);
            var publicMatch = results.FirstOrDefault(r => r.Id == publicRecipe.Id);
            var privateMatch = results.FirstOrDefault(r => r.Id == privateRecipe.Id);
            Assert.NotNull(publicMatch);
            Assert.True(publicMatch.IsPublic);
            Assert.NotNull(privateMatch);
            Assert.False(privateMatch.IsPublic);
        }

        [Fact]
        public async Task MatchRecipes_ShouldExcludePrivateRecipesOfOtherUsers()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            
            // Seed ingredients
            var kurczak = new Ingredient { Id = Guid.NewGuid(), Name = "kurczak", IsSpiceOrStaple = false };
            dbContext.Ingredients.Add(kurczak);
            
            // Seed a private recipe belonging to user A
            var userAId = Guid.NewGuid();
            var privateRecipeA = new Recipe { Id = Guid.NewGuid(), Title = "Kurczak Użytkownika A", IsPublic = false, UserId = userAId };
            dbContext.Recipes.Add(privateRecipeA);
            dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = privateRecipeA.Id, IngredientId = kurczak.Id, Quantity = "100g" });

            // Seed a private recipe belonging to user B
            var userBId = Guid.NewGuid();
            var privateRecipeB = new Recipe { Id = Guid.NewGuid(), Title = "Kurczak Użytkownika B", IsPublic = false, UserId = userBId };
            dbContext.Recipes.Add(privateRecipeB);
            dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = privateRecipeB.Id, IngredientId = kurczak.Id, Quantity = "200g" });

            dbContext.SaveChanges();
            
            var recipeService = new RecipeService(dbContext);

            // Act: user A searches
            var resultsForUserA = await recipeService.MatchRecipesAsync(new List<string> { "kurczak" }, userAId);

            // Assert
            Assert.Single(resultsForUserA);
            Assert.Equal("Kurczak Użytkownika A", resultsForUserA[0].Title);
            Assert.False(resultsForUserA[0].IsPublic);
        }

        [Fact]
        public async Task GetMyRecipes_ShouldReturnCorrectRecipes()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "My Recipe", IsPublic = false, UserId = userId };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act
            var results = await recipeService.GetMyRecipesAsync(userId);

            // Assert
            Assert.Single(results);
            Assert.Equal("My Recipe", results[0].Title);
        }

        [Fact]
        public async Task CreateRecipe_WithDuplicateIngredients_ShouldThrowValidationException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var ingredientId = Guid.NewGuid();
            var recipeService = new RecipeService(dbContext);

            var reqList = new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(ingredientId, "100g"),
                new RecipeIngredientRequest(ingredientId, "200g")
            };
            var requestObj = new CreateRecipeRequest("Tytul", "Instrukcja", reqList);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => recipeService.CreateRecipeAsync(userId, requestObj));
        }

        [Fact]
        public async Task CreateRecipe_WithInvalidIngredients_ShouldThrowValidationException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipeService = new RecipeService(dbContext);

            var request = new CreateRecipeRequest("Tytul", "Instrukcja", new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(Guid.NewGuid(), "100g")
            });

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => recipeService.CreateRecipeAsync(userId, request));
        }

        [Fact]
        public async Task CreateRecipe_ShouldSucceed()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "pomidor", IsSpiceOrStaple = false };
            dbContext.Ingredients.Add(ingredient);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            var request = new CreateRecipeRequest("Zupa pomidorowa", "Gotuj...", new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(ingredient.Id, "500g")
            });

            // Act
            var result = await recipeService.CreateRecipeAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Zupa pomidorowa", result.Title);
            Assert.Single(result.Ingredients);
            Assert.Equal(ingredient.Id, result.Ingredients[0].IngredientId);
        }

        [Fact]
        public async Task UpdateRecipe_NonExistent_ShouldThrowNotFoundException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipeService = new RecipeService(dbContext);

            var request = new UpdateRecipeRequest("New Title", "New Instructions", null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => recipeService.UpdateRecipeAsync(Guid.NewGuid(), userId, request));
        }

        [Fact]
        public async Task UpdateRecipe_DifferentUser_ShouldThrowForbiddenException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Old Title", Instructions = "Old Instructions", UserId = ownerId };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);
            var request = new UpdateRecipeRequest("New Title", "New Instructions", null);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => recipeService.UpdateRecipeAsync(recipe.Id, otherUserId, request));
        }

        [Fact]
        public async Task UpdateRecipe_ShouldSucceed()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Old Title", Instructions = "Old Instructions", UserId = userId };
            var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "pomidor", IsSpiceOrStaple = false };
            dbContext.Recipes.Add(recipe);
            dbContext.Ingredients.Add(ingredient);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);
            var request = new UpdateRecipeRequest("New Title", "New Instructions", new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(ingredient.Id, "300g")
            });

            // Act
            var result = await recipeService.UpdateRecipeAsync(recipe.Id, userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Title", result.Title);
            Assert.Single(result.Ingredients);
            Assert.Equal("300g", result.Ingredients[0].Quantity);
        }

        [Fact]
        public async Task DeleteRecipe_NonExistent_ShouldThrowNotFoundException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipeService = new RecipeService(dbContext);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => recipeService.DeleteRecipeAsync(Guid.NewGuid(), userId));
        }

        [Fact]
        public async Task DeleteRecipe_DifferentUser_ShouldThrowForbiddenException()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Title", Instructions = "Instructions", UserId = ownerId };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => recipeService.DeleteRecipeAsync(recipe.Id, otherUserId));
        }

        [Fact]
        public async Task DeleteRecipe_ShouldSucceed()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var userId = Guid.NewGuid();
            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Title", Instructions = "Instructions", UserId = userId };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act
            await recipeService.DeleteRecipeAsync(recipe.Id, userId);

            // Assert
            var deletedRecipe = await dbContext.Recipes.FindAsync(recipe.Id);
            Assert.Null(deletedRecipe);
        }

        [Fact]
        public async Task MatchRecipes_ShouldFollowBankersRounding()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();

            // Recipe 1 & 2: 8 primary ingredients each. Total weight = 8.0
            var recipe1 = new Recipe { Id = Guid.NewGuid(), Title = "Recipe 12.5", Instructions = "Instructions", IsPublic = true };
            var recipe2 = new Recipe { Id = Guid.NewGuid(), Title = "Recipe 37.5", Instructions = "Instructions", IsPublic = true };
            dbContext.Recipes.AddRange(recipe1, recipe2);

            var primaryIngredients = new List<Ingredient>();
            for (int i = 0; i < 8; i++)
            {
                var ing = new Ingredient { Id = Guid.NewGuid(), Name = $"primary{i}", IsSpiceOrStaple = false };
                primaryIngredients.Add(ing);
                dbContext.Ingredients.Add(ing);
            }

            await dbContext.SaveChangesAsync();

            // Link all 8 ingredients to both recipes
            foreach (var ing in primaryIngredients)
            {
                dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = recipe1.Id, IngredientId = ing.Id, Quantity = "1" });
                dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = recipe2.Id, IngredientId = ing.Id, Quantity = "1" });
            }
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act 1: User has 1 primary ingredient -> Matched weight = 1.0. Ratio = 1.0 / 8.0 = 12.5% => rounds to 12 (even)
            var userIngredients1 = primaryIngredients.Take(1).Select(i => i.Name).ToList();
            var results1 = await recipeService.MatchRecipesAsync(userIngredients1);

            // Assert 1: rounds down to 12
            var matchedRecipe1 = results1.FirstOrDefault(r => r.Title == "Recipe 12.5");
            Assert.NotNull(matchedRecipe1);
            Assert.Equal(12, matchedRecipe1.MatchRate);

            // Act 2: User has 3 primary ingredients -> Matched weight = 3.0. Ratio = 3.0 / 8.0 = 37.5% => rounds to 38 (even)
            var userIngredients2 = primaryIngredients.Take(3).Select(i => i.Name).ToList();
            var results2 = await recipeService.MatchRecipesAsync(userIngredients2);

            // Assert 2: rounds up to 38
            var matchedRecipe2 = results2.FirstOrDefault(r => r.Title == "Recipe 37.5");
            Assert.NotNull(matchedRecipe2);
            Assert.Equal(38, matchedRecipe2.MatchRate);
        }

        [Fact]
        public async Task MatchRecipes_ShouldHandleAllSpiceRecipe()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();

            var spice1 = new Ingredient { Id = Guid.NewGuid(), Name = "sol", IsSpiceOrStaple = true };
            var spice2 = new Ingredient { Id = Guid.NewGuid(), Name = "pieprz", IsSpiceOrStaple = true };
            dbContext.Ingredients.AddRange(spice1, spice2);

            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "All Spice Recipe", Instructions = "Instructions", IsPublic = true };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipe.Id, IngredientId = spice1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipe.Id, IngredientId = spice2.Id, Quantity = "1" }
            );
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act: User has only "sol"
            var results = await recipeService.MatchRecipesAsync(new List<string> { "sol" });

            // Assert: Total weight = 0.2, Matched = 0.1 -> 50%
            var matchedRecipe = results.FirstOrDefault(r => r.Title == "All Spice Recipe");
            Assert.NotNull(matchedRecipe);
            Assert.Equal(50, matchedRecipe.MatchRate);
        }

        [Fact]
        public async Task MatchRecipes_ShouldExcludeZeroPercentMatches()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();

            var pomidor = new Ingredient { Id = Guid.NewGuid(), Name = "pomidor", IsSpiceOrStaple = false };
            var ogorek = new Ingredient { Id = Guid.NewGuid(), Name = "ogórek", IsSpiceOrStaple = false };
            dbContext.Ingredients.AddRange(pomidor, ogorek);

            var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Tomato Recipe", Instructions = "Instructions", IsPublic = true };
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            dbContext.RecipeIngredients.Add(new RecipeIngredient { RecipeId = recipe.Id, IngredientId = pomidor.Id, Quantity = "1" });
            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act: User has only "ogorek", which is not in the recipe
            var results = await recipeService.MatchRecipesAsync(new List<string> { "ogórek" });

            // Assert: excluded from the results
            Assert.DoesNotContain(results, r => r.Title == "Tomato Recipe");
        }

        [Fact]
        public async Task MatchRecipes_ShouldSortByTieBreakersCorrectly()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();

            var p1 = new Ingredient { Id = Guid.NewGuid(), Name = "p1", IsSpiceOrStaple = false };
            var p2 = new Ingredient { Id = Guid.NewGuid(), Name = "p2", IsSpiceOrStaple = false };
            var p3 = new Ingredient { Id = Guid.NewGuid(), Name = "p3", IsSpiceOrStaple = false };
            var p4 = new Ingredient { Id = Guid.NewGuid(), Name = "p4", IsSpiceOrStaple = false };
            dbContext.Ingredients.AddRange(p1, p2, p3, p4);

            // Seed recipes:
            // Alpha: p1, p2 -> Match 2/2 = 100%, 0 missing primary
            var recipeAlpha = new Recipe { Id = Guid.NewGuid(), Title = "Recipe Alpha", Instructions = "Inst", IsPublic = true };
            // Beta: p1, p3 -> Match 1/2 = 50%, 1 missing primary
            var recipeBeta = new Recipe { Id = Guid.NewGuid(), Title = "Recipe Beta", Instructions = "Inst", IsPublic = true };
            // Gamma: p1, p4 -> Match 1/2 = 50%, 1 missing primary
            var recipeGamma = new Recipe { Id = Guid.NewGuid(), Title = "Recipe Gamma", Instructions = "Inst", IsPublic = true };
            // Delta: p1, p2, p3 -> Match 2/3 = 67%, 1 missing primary
            var recipeDelta = new Recipe { Id = Guid.NewGuid(), Title = "Recipe Delta", Instructions = "Inst", IsPublic = true };
            // Epsilon: p1, p2, p3, p4 -> Match 2/4 = 50%, 2 missing primary
            var recipeEpsilon = new Recipe { Id = Guid.NewGuid(), Title = "Recipe Epsilon", Instructions = "Inst", IsPublic = true };

            dbContext.Recipes.AddRange(recipeAlpha, recipeBeta, recipeGamma, recipeDelta, recipeEpsilon);
            await dbContext.SaveChangesAsync();

            // RecipeAlpha: p1, p2
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipeAlpha.Id, IngredientId = p1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeAlpha.Id, IngredientId = p2.Id, Quantity = "1" }
            );

            // RecipeBeta: p1, p3
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipeBeta.Id, IngredientId = p1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeBeta.Id, IngredientId = p3.Id, Quantity = "1" }
            );

            // RecipeGamma: p1, p4
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipeGamma.Id, IngredientId = p1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeGamma.Id, IngredientId = p4.Id, Quantity = "1" }
            );

            // RecipeDelta: p1, p2, p3
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipeDelta.Id, IngredientId = p1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeDelta.Id, IngredientId = p2.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeDelta.Id, IngredientId = p3.Id, Quantity = "1" }
            );

            // RecipeEpsilon: p1, p2, p3, p4
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = recipeEpsilon.Id, IngredientId = p1.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeEpsilon.Id, IngredientId = p2.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeEpsilon.Id, IngredientId = p3.Id, Quantity = "1" },
                new RecipeIngredient { RecipeId = recipeEpsilon.Id, IngredientId = p4.Id, Quantity = "1" }
            );

            await dbContext.SaveChangesAsync();

            var recipeService = new RecipeService(dbContext);

            // Act: User has "p1" and "p2"
            var results = await recipeService.MatchRecipesAsync(new List<string> { "p1", "p2" });

            // Assert sorting order:
            // 1. Recipe Alpha (100% match, 0 missing primary)
            // 2. Recipe Delta (67% match, 1 missing primary)
            // 3. Recipe Beta (50% match, 1 missing primary) -> Wins tie-breaker over Epsilon (1 < 2), alphabetical over Gamma
            // 4. Recipe Gamma (50% match, 1 missing primary)
            // 5. Recipe Epsilon (50% match, 2 missing primary)
            Assert.Equal(5, results.Count);
            Assert.Equal("Recipe Alpha", results[0].Title);
            Assert.Equal("Recipe Delta", results[1].Title);
            Assert.Equal("Recipe Beta", results[2].Title);
            Assert.Equal("Recipe Gamma", results[3].Title);
            Assert.Equal("Recipe Epsilon", results[4].Title);
        }
    }
}
