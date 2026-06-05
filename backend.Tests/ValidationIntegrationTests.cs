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

namespace _10x_cookbook_backend.Tests
{
    public class ValidationIntegrationTests : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;
        private readonly string? _originalEnv;

        public ValidationIntegrationTests()
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
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

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

        [Theory]
        [InlineData("invalid-email", "Password123")]
        [InlineData("", "Password123")]
        [InlineData("test@example.com", "123")]
        [InlineData("test@example.com", "")]
        public async Task Register_ShouldReturnBadRequest_WhenPayloadIsInvalid(string email, string password)
        {
            // Arrange
            var client = _factory.CreateClient();
            var payload = new { Email = email, Password = password };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/register", payload);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_ShouldReturnBadRequest_WhenTitleExceeds200Characters()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = Guid.NewGuid();
            var token = GenerateToken(userId, "user@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var longTitle = new string('A', 201);
            var payload = new CreateRecipeRequest(longTitle, "Instructions", new List<RecipeIngredientRequest>());

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes", payload);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_ShouldReturnBadRequest_WhenQuantityExceeds100Characters()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = Guid.NewGuid();
            var token = GenerateToken(userId, "user@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var longQuantity = new string('A', 101);
            var ingredientId = Guid.NewGuid();

            // Seed ingredient first
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "test", IsSpiceOrStaple = false });
                await dbContext.SaveChangesAsync();
            }

            var payload = new CreateRecipeRequest("Pizza", "Instructions", new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(ingredientId, longQuantity)
            });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes", payload);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateRecipe_ShouldReturnBadRequest_WhenQuantityIsEmptyOrWhitespace(string quantity)
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = Guid.NewGuid();
            var token = GenerateToken(userId, "user@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ingredientId = Guid.NewGuid();

            // Seed ingredient first
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "test", IsSpiceOrStaple = false });
                await dbContext.SaveChangesAsync();
            }

            var payload = new CreateRecipeRequest("Pizza", "Instructions", new List<RecipeIngredientRequest>
            {
                new RecipeIngredientRequest(ingredientId, quantity)
            });

            // Act
            var response = await client.PostAsJsonAsync("/api/recipes", payload);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateRecipe_ShouldReturnBadRequest_WhenTitleExceeds200Characters()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = Guid.NewGuid();
            var token = GenerateToken(userId, "user@test.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var recipeId = Guid.NewGuid();
            // Seed recipe first
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Recipes.Add(new Recipe { Id = recipeId, Title = "Old Title", Instructions = "Old Instructions", UserId = userId });
                await dbContext.SaveChangesAsync();
            }

            var longTitle = new string('A', 201);
            var payload = new UpdateRecipeRequest(longTitle, "Instructions", new List<RecipeIngredientRequest>());

            // Act
            var response = await client.PutAsJsonAsync($"/api/recipes/{recipeId}", payload);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
