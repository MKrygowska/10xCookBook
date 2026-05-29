using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            app.MapDelete("/api/users/me", (ClaimsPrincipal user, UserService userService) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var success = userService.DeleteUser(userId, out var errorMessage);
                if (!success)
                {
                    return Results.BadRequest(new { error = errorMessage });
                }

                return Results.NoContent();
            })
            .RequireAuthorization();
        }
    }
}
