using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _10x_cookbook_backend.DTOs;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseApiController
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {

            var user = _userService.Register(request.Email, request.Password, out var errorMessage);
            if (user == null)
            {
                return BadRequest(new { error = errorMessage });
            }

            var token = _userService.Login(request.Email, request.Password, out _);
            return Ok(new { token = token, email = user.Email });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {

            var token = _userService.Login(request.Email, request.Password, out var errorMessage);
            if (token == null)
            {
                return BadRequest(new { error = errorMessage });
            }

            return Ok(new { token = token, email = request.Email.Trim().ToLower() });
        }
    }
}
