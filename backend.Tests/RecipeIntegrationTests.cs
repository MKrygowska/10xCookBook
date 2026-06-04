using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;
using _10x_cookbook_backend.DTOs;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Tests
{
    public class RecipeIntegrationTests : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;
        private readonly string? _originalEnv;

        public RecipeIntegrationTests()
        {
            _originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            _dbName = Guid.NewGuid().ToString();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    var projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "backend"));
                    builder.UseContentRoot(projectDir);

                    builder.ConfigureServices(services =>
                    {
                        // Remove existing DbContextOptions
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add in-memory database
                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(_dbName);
                        });
                    });
                });
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnv);
            _factory.Dispose();
        }

        private string GenerateToken(Guid userId, string email)
        {
            // Same secret key as Development mode
            var secret = "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong";
            var issuer = "10xCookBookAPI";
            var audience = "10xCookBookClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task SeedTestDataAsync(AppDbContext dbContext, Guid userAId, Guid userBId)
        {
            // Clear existing
            dbContext.RecipeIngredients.RemoveRange(dbContext.RecipeIngredients);
            dbContext.Recipes.RemoveRange(dbContext.Recipes);
            dbContext.Ingredients.RemoveRange(dbContext.Ingredients);
            dbContext.Users.RemoveRange(dbContext.Users);
            await dbContext.SaveChangesAsync();

            // Seed Users
            var userA = new User { Id = userAId, Email = "usera@test.com", PasswordHash = "hash" };
            var userB = new User { Id = userBId, Email = "userb@test.com", PasswordHash = "hash" };
            dbContext.Users.AddRange(userA, userB);

            // Seed Ingredients
            var pomidor = new Ingredient { Id = Guid.NewGuid(), Name = "pomidor", IsSpiceOrStaple = false };
            var ser = new Ingredient { Id = Guid.NewGuid(), Name = "ser", IsSpiceOrStaple = false };
            dbContext.Ingredients.AddRange(pomidor, ser);

            // Seed Recipes
            // User A Private Recipe
            var privateRecipeA = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Private Pizza A",
                Instructions = "Instructions A",
                IsPublic = false,
                UserId = userAId
            };
            // User B Private Recipe
            var privateRecipeB = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Private Pasta B",
                Instructions = "Instructions B",
                IsPublic = false,
                UserId = userBId
            };
            // Public Recipe
            var publicRecipe = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Public Soup",
                Instructions = "Soup Instructions",
                IsPublic = true,
                UserId = userBId
            };

            dbContext.Recipes.AddRange(privateRecipeA, privateRecipeB, publicRecipe);
            await dbContext.SaveChangesAsync();

            // Seed Recipe Ingredients
            dbContext.RecipeIngredients.AddRange(
                new RecipeIngredient { RecipeId = privateRecipeA.Id, IngredientId = ser.Id, Quantity = "100g" },
                new RecipeIngredient { RecipeId = privateRecipeB.Id, IngredientId = ser.Id, Quantity = "150g" },
                new RecipeIngredient { RecipeId = publicRecipe.Id, IngredientId = pomidor.Id, Quantity = "2 sztuki" }
            );
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task MatchRecipes_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new MatchRecipesRequest(new List<string> { "pomidor" });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes/match", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MatchRecipes_ShouldReturnUnauthorized_WhenTokenIsMalformed()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformedTokenContentHere");
            var request = new MatchRecipesRequest(new List<string> { "pomidor" });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes/match", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MatchRecipes_ShouldIsolatePrivateRecipes()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userAId = Guid.NewGuid();
            var userBId = Guid.NewGuid();

            // Seed DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await SeedTestDataAsync(dbContext, userAId, userBId);
            }

            // Generate token for User A
            var token = GenerateToken(userAId, "usera@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Search for "ser" (which is in Private Pizza A and Private Pasta B)
            var request = new MatchRecipesRequest(new List<string> { "ser" });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes/match", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<RecipeMatchResult>>();
            Assert.NotNull(results);

            // User A should see "Private Pizza A" but NOT "Private Pasta B"
            Assert.Contains(results, r => r.Title == "Private Pizza A");
            Assert.DoesNotContain(results, r => r.Title == "Private Pasta B");
        }

        [Fact]
        public async Task MatchRecipes_ShouldIncludePublicRecipesForAnyAuthenticatedUser()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userAId = Guid.NewGuid();
            var userBId = Guid.NewGuid();

            // Seed DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await SeedTestDataAsync(dbContext, userAId, userBId);
            }

            // Generate token for User A
            var token = GenerateToken(userAId, "usera@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Search for "pomidor" (which is in "Public Soup")
            var request = new MatchRecipesRequest(new List<string> { "pomidor" });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes/match", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<RecipeMatchResult>>();
            Assert.NotNull(results);

            // User A should see "Public Soup"
            Assert.Contains(results, r => r.Title == "Public Soup");
        }
    }
}
