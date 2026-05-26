using System.Text.Json.Serialization;

namespace _10x_cookbook_backend.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }
}
