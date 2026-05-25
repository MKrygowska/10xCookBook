using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Endpoints;
using _10x_cookbook_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add UserService as a scoped service (to consume scoped DbContext)
builder.Services.AddScoped<UserService>();

// Add RecipeService as a scoped service
builder.Services.AddScoped<RecipeService>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSWA", policy =>
    {
        policy.WithOrigins("https://calm-water-01776d503.7.azurestaticapps.net", "http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add JWT Authentication
var secret = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "10xCookBookAPI";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "10xCookBookClient";
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSWA");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Map our authentication endpoints
app.MapAuthEndpoints();

// Map our recipe endpoints
app.MapRecipeEndpoints();

Console.WriteLine("10xCookBook API Started");
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
