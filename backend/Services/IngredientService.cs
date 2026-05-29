using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.DTOs;

namespace _10x_cookbook_backend.Services
{
    public class IngredientService
    {
        private readonly AppDbContext _dbContext;

        public IngredientService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<IngredientResponseDto>> GetIngredientsAsync()
        {
            return await _dbContext.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new IngredientResponseDto(i.Id, i.Name, i.IsSpiceOrStaple))
                .ToListAsync();
        }
    }
}
