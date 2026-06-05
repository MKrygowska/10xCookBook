using Microsoft.EntityFrameworkCore;
using Xunit;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Tests
{
    public class IngredientServiceTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new AppDbContext(options);
            // InMemoryDatabase doesn't run migrations, so EnsureCreated triggers seeding from OnModelCreating
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        [Fact]
        public async Task GetIngredients_ShouldCoverPolishStaples()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var service = new IngredientService(dbContext);

            // Act
            var ingredients = await service.GetIngredientsAsync();

            // Assert
            Assert.True(ingredients.Count >= 40, $"Expected at least 40 ingredients, but found {ingredients.Count}.");

            var names = ingredients.Select(i => i.Name.ToLower()).ToList();
            Assert.Contains("twaróg", names);
            Assert.Contains("kiełbasa", names);
            Assert.Contains("kapusta kiszona", names);
            Assert.Contains("ogórek kiszony", names);
            Assert.Contains("schab", names);
            Assert.Contains("śmietana", names);
            Assert.Contains("burak", names);
        }
    }
}
