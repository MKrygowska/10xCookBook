using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _10x_cookbook_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSpiceOrStaple = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.RecipeId, x.IngredientId });
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "IsSpiceOrStaple", "Name" },
                values: new object[,]
                {
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711001"), false, "pomidor" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711002"), false, "cebula" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711003"), false, "czosnek" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711004"), false, "makaron" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711005"), true, "oliwa z oliwek" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711006"), true, "bazylia" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711007"), false, "pierś z kurczaka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711008"), false, "ryż" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711009"), false, "marchewka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711010"), false, "ziemniaki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711011"), true, "sól" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711012"), true, "pieprz" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711013"), true, "masło" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711014"), false, "mleko" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711015"), true, "mąka pszenna" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711016"), false, "jajko" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711017"), false, "ser żółty" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711018"), true, "papryka słodka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711019"), true, "cukier" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711020"), false, "cytryna" }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "Instructions", "IsPublic", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "Ugotuj makaron al dente. Na patelni rozgrzej oliwę z oliwek, zeszklij pokrojoną cebulę i posiekany czosnek. Dodaj rozgniecione pomidory, sól, pieprz i bazylię. Duś przez 10 minut. Wymieszaj makaron z sosem i posyp serem.", true, "Makaron z sosem pomidorowym", null },
                    { new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "Ugotuj ryż. Pierś z kurczaka pokrój w kostkę, dopraw solą, pieprzem i słodką papryką, a następnie usmaż na oliwie z oliwek. Dodaj pokrojoną cebulę i marchewkę. Smaż przez 10 minut. Podawaj kurczaka z warzywami na ryżu.", true, "Ryż z kurczakiem i warzywami", null },
                    { new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822003"), "Rozgrzej masło na patelni. Wbij jajka, posól, popieprz i smaż na wolnym ogniu, ciągle mieszając, aż do uzyskania pożądanej konsystencji.", true, "Jajecznica na maśle", null }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "IngredientId", "RecipeId", "Quantity" },
                values: new object[,]
                {
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711001"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "3 sztuki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711002"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "1 sztuka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711003"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "2 ząbki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711004"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "200g" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711005"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "2 łyżki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711006"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "kilka listków" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711011"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "do smaku" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711012"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822001"), "do smaku" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711002"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "1 sztuka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711005"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "2 łyżki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711007"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "300g" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711008"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "150g" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711009"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "1 sztuka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711011"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "szczypta" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711012"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "szczypta" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711018"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822002"), "1 łyżeczka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711011"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822003"), "szczypta" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711012"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822003"), "szczypta" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711013"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822003"), "1 łyżka" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711016"), new Guid("9d9d2022-36fa-4684-93ef-ecf2dc822003"), "3 sztuki" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserId",
                table: "Recipes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
