using Microsoft.EntityFrameworkCore;
using Xunit;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;
using _10x_cookbook_backend.Services;

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
    }
}
