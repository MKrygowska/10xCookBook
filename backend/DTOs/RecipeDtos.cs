using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _10x_cookbook_backend.DTOs
{
    public record MatchRecipesRequest(
        [Required(ErrorMessage = "Lista składników jest wymagana.")]
        List<string> Ingredients
    );

    public record RecipeIngredientRequest(
        Guid IngredientId, 
        [Required]
        string Quantity
    );

    public record CreateRecipeRequest(
        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        string Title, 
        
        [Required(ErrorMessage = "Instrukcje są wymagane.")]
        string Instructions, 
        
        List<RecipeIngredientRequest>? Ingredients
    );

    public record UpdateRecipeRequest(
        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        string Title, 
        
        [Required(ErrorMessage = "Instrukcje są wymagane.")]
        string Instructions, 
        
        List<RecipeIngredientRequest>? Ingredients
    );

    public record RecipeResponseDto(
        Guid Id, 
        string Title, 
        string Instructions, 
        bool IsPublic, 
        List<RecipeIngredientResponseDto> Ingredients
    );

    public record RecipeIngredientResponseDto(
        Guid IngredientId, 
        string Name, 
        string Quantity
    );

    public record CreateRecipeResponseDto(
        Guid Id, 
        string Title, 
        string Instructions, 
        bool IsPublic, 
        List<CreateRecipeIngredientResponseDto> Ingredients
    );

    public record CreateRecipeIngredientResponseDto(
        Guid IngredientId, 
        string Quantity
    );
}
