using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using _10x_cookbook_backend.DTOs;
using _10x_cookbook_backend.Exceptions;
using _10x_cookbook_backend.Services;

using Microsoft.Extensions.Logging;

namespace _10x_cookbook_backend.Controllers
{
    [Authorize]
    public class RecipesController : BaseApiController
    {
        private readonly RecipeService _recipeService;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(RecipeService recipeService, ILogger<RecipesController> logger)
        {
            _recipeService = recipeService;
            _logger = logger;
        }

        [HttpPost("match")]
        public async Task<IActionResult> MatchRecipes([FromBody] MatchRecipesRequest request)
        {
            if (request == null || request.Ingredients == null)
            {
                return BadRequest(new { error = "Lista składników jest wymagana." });
            }

            var userId = TryGetUserId();
            var matchedRecipes = await _recipeService.MatchRecipesAsync(request.Ingredients, userId);
            return Ok(matchedRecipes);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRecipes()
        {
            try
            {
                var userId = GetUserId();
                var recipes = await _recipeService.GetMyRecipesAsync(userId);
                return Ok(recipes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetMyRecipes");
                return BadRequest(new { error = "Wystąpił nieoczekiwany błąd." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request)
        {
            try
            {
                var userId = GetUserId();
                var recipe = await _recipeService.CreateRecipeAsync(userId, request);
                return Created($"/api/recipes/{recipe.Id}", recipe);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateRecipe");
                return BadRequest(new { error = "Wystąpił nieoczekiwany błąd." });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRecipe(Guid id, [FromBody] UpdateRecipeRequest request)
        {
            try
            {
                var userId = GetUserId();
                var recipe = await _recipeService.UpdateRecipeAsync(id, userId, request);
                return Ok(recipe);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateRecipe");
                return BadRequest(new { error = "Wystąpił nieoczekiwany błąd." });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRecipe(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _recipeService.DeleteRecipeAsync(id, userId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteRecipe");
                return BadRequest(new { error = "Wystąpił nieoczekiwany błąd." });
            }
        }
    }
}
