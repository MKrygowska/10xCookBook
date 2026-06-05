using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace _10x_cookbook_backend.Models
{
    public class RecipeIngredient
    {
        public Guid RecipeId { get; set; }

        [JsonIgnore]
        public Recipe? Recipe { get; set; }

        public Guid IngredientId { get; set; }

        public Ingredient? Ingredient { get; set; }

        [Required]
        [MaxLength(100)]
        public string Quantity { get; set; } = string.Empty;
    }
}
