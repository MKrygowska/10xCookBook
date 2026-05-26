using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Models;

namespace _10x_cookbook_backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<Ingredient> Ingredients { get; set; } = null!;
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure compound key for RecipeIngredient
            modelBuilder.Entity<RecipeIngredient>()
                .HasKey(ri => new { ri.RecipeId, ri.IngredientId });

            // Configure relationships
            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Cascade Delete for private recipes
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique index for User Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Seed Ingredients (20 znormalizowanych, popularnych składników - lowercase, trimmed)
            var ingredients = new List<Ingredient>
            {
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711001"), Name = "pomidor", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711002"), Name = "cebula", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711003"), Name = "czosnek", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711004"), Name = "makaron", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711005"), Name = "oliwa z oliwek", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711006"), Name = "bazylia", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711007"), Name = "pierś z kurczaka", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711008"), Name = "ryż", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711009"), Name = "marchewka", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711010"), Name = "ziemniaki", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711011"), Name = "sól", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711012"), Name = "pieprz", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711013"), Name = "masło", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711014"), Name = "mleko", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711015"), Name = "mąka pszenna", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711016"), Name = "jajko", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711017"), Name = "ser żółty", IsSpiceOrStaple = false },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711018"), Name = "papryka słodka", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711019"), Name = "cukier", IsSpiceOrStaple = true },
                new() { Id = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711020"), Name = "cytryna", IsSpiceOrStaple = false }
            };

            modelBuilder.Entity<Ingredient>().HasData(ingredients);

            // Seed Recipes (3 publiczne przepisy - UserId = null, IsPublic = true)
            var recipe1Id = Guid.Parse("9d9d2022-36fa-4684-93ef-ecf2dc822001");
            var recipe2Id = Guid.Parse("9d9d2022-36fa-4684-93ef-ecf2dc822002");
            var recipe3Id = Guid.Parse("9d9d2022-36fa-4684-93ef-ecf2dc822003");

            modelBuilder.Entity<Recipe>().HasData(
                new Recipe
                {
                    Id = recipe1Id,
                    Title = "Makaron z sosem pomidorowym",
                    Instructions = "Ugotuj makaron al dente. Na patelni rozgrzej oliwę z oliwek, zeszklij pokrojoną cebulę i posiekany czosnek. Dodaj rozgniecione pomidory, sól, pieprz i bazylię. Duś przez 10 minut. Wymieszaj makaron z sosem i posyp serem.",
                    IsPublic = true,
                    UserId = null
                },
                new Recipe
                {
                    Id = recipe2Id,
                    Title = "Ryż z kurczakiem i warzywami",
                    Instructions = "Ugotuj ryż. Pierś z kurczaka pokrój w kostkę, dopraw solą, pieprzem i słodką papryką, a następnie usmaż na oliwie z oliwek. Dodaj pokrojoną cebulę i marchewkę. Smaż przez 10 minut. Podawaj kurczaka z warzywami na ryżu.",
                    IsPublic = true,
                    UserId = null
                },
                new Recipe
                {
                    Id = recipe3Id,
                    Title = "Jajecznica na maśle",
                    Instructions = "Rozgrzej masło na patelni. Wbij jajka, posól, popieprz i smaż na wolnym ogniu, ciągle mieszając, aż do uzyskania pożądanej konsystencji.",
                    IsPublic = true,
                    UserId = null
                }
            );

            // Seed RecipeIngredients
            modelBuilder.Entity<RecipeIngredient>().HasData(
                // Recipe 1 Ingredients
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711004"), Quantity = "200g" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711001"), Quantity = "3 sztuki" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711005"), Quantity = "2 łyżki" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711002"), Quantity = "1 sztuka" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711003"), Quantity = "2 ząbki" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711006"), Quantity = "kilka listków" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711011"), Quantity = "do smaku" },
                new RecipeIngredient { RecipeId = recipe1Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711012"), Quantity = "do smaku" },

                // Recipe 2 Ingredients
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711008"), Quantity = "150g" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711007"), Quantity = "300g" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711009"), Quantity = "1 sztuka" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711002"), Quantity = "1 sztuka" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711005"), Quantity = "2 łyżki" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711018"), Quantity = "1 łyżeczka" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711011"), Quantity = "szczypta" },
                new RecipeIngredient { RecipeId = recipe2Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711012"), Quantity = "szczypta" },

                // Recipe 3 Ingredients
                new RecipeIngredient { RecipeId = recipe3Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711016"), Quantity = "3 sztuki" },
                new RecipeIngredient { RecipeId = recipe3Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711013"), Quantity = "1 łyżka" },
                new RecipeIngredient { RecipeId = recipe3Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711011"), Quantity = "szczypta" },
                new RecipeIngredient { RecipeId = recipe3Id, IngredientId = Guid.Parse("4d4d1011-25ef-4573-82ef-dcf1db711012"), Quantity = "szczypta" }
            );
        }
    }
}
