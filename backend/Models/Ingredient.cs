using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace _10x_cookbook_backend.Models
{
    public class Ingredient
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsSpiceOrStaple { get; set; }

        [JsonIgnore]
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
