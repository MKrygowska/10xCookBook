using Microsoft.AspNetCore.Mvc;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/register", ([FromBody] RegisterRequest request, UserService userService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest(new { error = "E-mail i hasło są wymagane." });
                }

                if (request.Password.Length < 6)
                {
                    return Results.BadRequest(new { error = "Hasło musi mieć co najmniej 6 znaków." });
                }

                var user = userService.Register(request.Email, request.Password, out var errorMessage);
                if (user == null)
                {
                    return Results.BadRequest(new { error = errorMessage });
                }

                var token = userService.Login(request.Email, request.Password, out _);
                return Results.Ok(new { token = token, email = user.Email });
            });

            app.MapPost("/api/auth/login", ([FromBody] LoginRequest request, UserService userService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest(new { error = "E-mail i hasło są wymagane." });
                }

                var token = userService.Login(request.Email, request.Password, out var errorMessage);
                if (token == null)
                {
                    return Results.BadRequest(new { error = errorMessage });
                }

                return Results.Ok(new { token = token, email = request.Email.Trim().ToLower() });
            });
        }
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);
}
