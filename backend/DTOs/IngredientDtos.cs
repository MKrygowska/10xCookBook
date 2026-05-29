using System;

namespace _10x_cookbook_backend.DTOs
{
    public record IngredientResponseDto(Guid Id, string Name, bool IsSpiceOrStaple);
}
