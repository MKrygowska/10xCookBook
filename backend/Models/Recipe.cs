using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace _10x_cookbook_backend.Models
{
    public class Recipe
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Instructions { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        public Guid? UserId { get; set; }
        
        [JsonIgnore]
        public User? User { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
