using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Models;

namespace _10x_cookbook_backend.Services
{
    public class UserService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public User? Register(string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            email = email.Trim().ToLower();

            if (_dbContext.Users.Any(u => u.Email == email))
            {
                errorMessage = "Ten e-mail jest już zajęty.";
                return null;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            try
            {
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
                return user;
            }
            catch (Exception ex)
            {
                errorMessage = "Wystąpił błąd podczas zapisywania użytkownika w bazie danych.";
                Console.WriteLine($"Register error: {ex.Message}");
                return null;
            }
        }

        public string? Login(string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            email = email.Trim().ToLower();

            var user = _dbContext.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                errorMessage = "Niepoprawny e-mail lub hasło.";
                return null;
            }

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret) || secret == "YOUR_JWT_SECRET_PLACEHOLDER")
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (env == "Development")
                {
                    secret = "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong";
                }
                else
                {
                    throw new InvalidOperationException("JWT Secret is not configured. Please set the 'JwtSettings:Secret' configuration value or the 'JwtSettings__Secret' environment variable.");
                }
            }

            var issuer = _configuration["JwtSettings:Issuer"] ?? "10xCookBookAPI";
            var audience = _configuration["JwtSettings:Audience"] ?? "10xCookBookClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
