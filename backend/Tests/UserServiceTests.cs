using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    }
}
