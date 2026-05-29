using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _10x_cookbook_backend.Services;

namespace _10x_cookbook_backend.Controllers
{
    [Authorize]
    public class IngredientsController : BaseApiController
    {
        private readonly IngredientService _ingredientService;

        public IngredientsController(IngredientService ingredientService)
        {
            _ingredientService = ingredientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetIngredients()
        {
            var ingredients = await _ingredientService.GetIngredientsAsync();
            return Ok(ingredients);
        }
    }
}
