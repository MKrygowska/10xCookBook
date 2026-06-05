using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _10x_cookbook_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolishIngredientsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "IsSpiceOrStaple", "Name" },
                values: new object[,]
                {
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711021"), false, "twaróg" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711022"), false, "kiełbasa" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711023"), false, "kapusta kiszona" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711024"), false, "ogórek kiszony" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711025"), false, "schab" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711026"), false, "śmietana" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711027"), true, "koperek" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711028"), true, "natka pietruszki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711029"), true, "majeranek" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711030"), true, "liść laurowy" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711031"), true, "ziele angielskie" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711032"), true, "olej rzepakowy" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711033"), false, "kasza gryczana" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711034"), true, "bułka tarta" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711035"), false, "burak" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711036"), false, "pieczarki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711037"), false, "boczek wędzony" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711038"), true, "chrzan" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711039"), false, "korzeń pietruszki" },
                    { new Guid("4d4d1011-25ef-4573-82ef-dcf1db711040"), false, "seler" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711021"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711022"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711023"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711024"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711025"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711026"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711027"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711028"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711029"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711030"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711031"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711032"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711033"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711034"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711035"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711036"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711037"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711038"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711039"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("4d4d1011-25ef-4573-82ef-dcf1db711040"));
        }
    }
}
