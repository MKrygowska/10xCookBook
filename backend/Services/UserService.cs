using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using _10x_cookbook_backend.Models;

namespace _10x_cookbook_backend.Services
{
    public class UserService
    {
        private static readonly List<User> _users = new();
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public User? Register(string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            email = email.Trim().ToLower();

            lock (_users)
            {
                if (_users.Any(u => u.Email == email))
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

                _users.Add(user);
                return user;
            }
        }

        public string? Login(string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            email = email.Trim().ToLower();

            User? user;
            lock (_users)
            {
                user = _users.FirstOrDefault(u => u.Email == email);
            }

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                errorMessage = "Niepoprawny e-mail lub hasło.";
                return null;
            }

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var secret = _configuration["JwtSettings:Secret"] ?? "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong";
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
