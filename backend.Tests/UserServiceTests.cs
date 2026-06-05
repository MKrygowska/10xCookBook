using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Tests
{
    public class UserServiceTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private IConfiguration CreateMockConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"JwtSettings:Secret", "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong"},
                {"JwtSettings:Issuer", "10xCookBookAPI"},
                {"JwtSettings:Audience", "10xCookBookClient"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void Register_ShouldCreateUser_WhenEmailIsUnique()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);

            // Act
            var user = userService.Register("unique@test.com", "Password123!", out string errorMessage);

            // Assert
            Assert.NotNull(user);
            Assert.Empty(errorMessage);
            Assert.Equal("unique@test.com", user.Email);
            Assert.True(dbContext.Users.Any(u => u.Email == "unique@test.com"));
        }

        [Fact]
        public void Register_ShouldReturnNull_WhenEmailAlreadyExists()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            userService.Register("duplicate@test.com", "Password123!", out _);

            // Act
            var result = userService.Register("duplicate@test.com", "NewPassword123!", out string errorMessage);

            // Assert
            Assert.Null(result);
            Assert.Equal("Ten e-mail jest już zajęty.", errorMessage);
        }

        [Fact]
        public void Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            userService.Register("login@test.com", "SecretPassword!", out _);

            // Act
            var token = userService.Login("login@test.com", "SecretPassword!", out string errorMessage);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Empty(errorMessage);
        }

        [Fact]
        public void Login_ShouldReturnNull_WhenCredentialsAreInvalid()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            userService.Register("login@test.com", "SecretPassword!", out _);

            // Act
            var token = userService.Login("login@test.com", "WrongPassword!", out string errorMessage);

            // Assert
            Assert.Null(token);
            Assert.Equal("Niepoprawny e-mail lub hasło.", errorMessage);
        }

        [Fact]
        public void Register_ShouldInitializeLastActive()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);

            // Act
            var user = userService.Register("active@test.com", "Password123!", out _);

            // Assert
            Assert.NotNull(user);
            Assert.True((DateTime.UtcNow - user.LastActive).TotalSeconds < 5);
        }

        [Fact]
        public void Login_ShouldUpdateLastActive()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            var user = userService.Register("loginactive@test.com", "Password123!", out _);
            var initialLastActive = user!.LastActive.AddHours(-1);
            user.LastActive = initialLastActive;
            dbContext.SaveChanges();

            // Act
            userService.Login("loginactive@test.com", "Password123!", out _);

            // Assert
            var updatedUser = dbContext.Users.Find(user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.LastActive > initialLastActive);
            Assert.True((DateTime.UtcNow - updatedUser.LastActive).TotalSeconds < 5);
        }

        [Fact]
        public void UpdateUserActivity_ShouldSetLastActiveToUtcNow()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            var user = userService.Register("activity@test.com", "Password123!", out _);
            var initialLastActive = user!.LastActive.AddHours(-1);
            user.LastActive = initialLastActive;
            dbContext.SaveChanges();

            // Act
            userService.UpdateUserActivity(user.Id);

            // Assert
            var updatedUser = dbContext.Users.Find(user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.LastActive > initialLastActive);
            Assert.True((DateTime.UtcNow - updatedUser.LastActive).TotalSeconds < 5);
        }

        [Fact]
        public void DeleteUser_ShouldRemoveUserFromDatabase()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            var user = userService.Register("delete@test.com", "Password123!", out _);

            // Act
            var success = userService.DeleteUser(user!.Id, out string errorMessage);

            // Assert
            Assert.True(success);
            Assert.Empty(errorMessage);
            Assert.Null(dbContext.Users.Find(user.Id));
        }

        [Fact]
        public void DeleteUser_ShouldCascadeDeletePrivateRecipes()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            var user = userService.Register("delete_cascade@test.com", "Password123!", out _);
            
            var recipe = new _10x_cookbook_backend.Models.Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Prywatny przepis testowy",
                Instructions = "Krok 1, Krok 2...",
                IsPublic = false,
                UserId = user!.Id
            };
            dbContext.Recipes.Add(recipe);
            dbContext.SaveChanges();

            // Act
            var success = userService.DeleteUser(user.Id, out string errorMessage);

            // Assert
            Assert.True(success);
            Assert.Empty(errorMessage);
            Assert.Null(dbContext.Users.Find(user.Id));
            Assert.Null(dbContext.Recipes.Find(recipe.Id));
        }

        [Fact]
        public async Task DataRetentionService_ShouldPurgeInactiveUsers()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var config = CreateMockConfiguration();
            var userService = new UserService(dbContext, config);
            
            // Create active user
            var activeUser = userService.Register("active_retention@test.com", "Password123!", out _);
            
            // Create inactive user
            var inactiveUser = userService.Register("inactive_retention@test.com", "Password123!", out _);
            inactiveUser!.LastActive = DateTime.UtcNow.AddMonths(-25);
            dbContext.SaveChanges();

            // Setup DataRetentionService configuration
            var inMemorySettings = new Dictionary<string, string?> {
                {"DataRetentionSettings:CleanupIntervalHours", "24"},
                {"DataRetentionSettings:RetentionPeriodMonths", "24"}
            };
            var drConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Mock service scope factory
            var services = new ServiceCollection();
            services.AddSingleton(dbContext);
            var serviceProvider = services.BuildServiceProvider();
            var scopeFactoryMock = new MockServiceScopeFactory(serviceProvider);

            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DataRetentionService>.Instance;
            var retentionService = new DataRetentionService(scopeFactoryMock, logger, drConfig);

            // Act
            // Start the service in a background task and stop it shortly after
            var cts = new CancellationTokenSource();
            var runTask = retentionService.StartAsync(cts.Token);
            
            // Wait slightly for execution of first run loop
            await Task.Delay(200);
            
            // Trigger cancellation to stop background service loop
            cts.Cancel();
            await runTask;

            // Assert
            Assert.NotNull(dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == activeUser!.Id));
            Assert.Null(dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == inactiveUser.Id));
        }

        private class MockServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IServiceProvider _serviceProvider;

            public MockServiceScopeFactory(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public IServiceScope CreateScope()
            {
                return new MockServiceScope(_serviceProvider);
            }
        }

        private class MockServiceScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; }

            public MockServiceScope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public void Dispose() { }
        }
    }
}
