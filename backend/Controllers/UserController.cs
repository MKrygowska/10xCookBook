using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Controllers
{
    [Authorize]
    public class UserController : BaseApiController
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpDelete("me")]
        public IActionResult DeleteMe()
        {
            try
            {
                var userId = GetUserId();
                var success = _userService.DeleteUser(userId, out var errorMessage);
                if (!success)
                {
                    return BadRequest(new { error = errorMessage });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
