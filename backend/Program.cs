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

// Add IngredientService as a scoped service
builder.Services.AddScoped<IngredientService>();

// Add DataRetentionService as a hosted service
builder.Services.AddHostedService<DataRetentionService>();

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
var secret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(secret) || secret == "YOUR_JWT_SECRET_PLACEHOLDER")
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    if (env == "Development")
    {
        secret = "SuperSecure10xCookBookSecretKey2026!ThatIsAtLeast32BytesLong";
    }
    else
    {
        throw new InvalidOperationException("JWT Secret is not configured. Please set the 'JwtSettings:Secret' configuration value or the 'JwtSettings__Secret' environment variable.");
    }
}
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

// Map our authentication endpoints
app.MapAuthEndpoints();

// Map our recipe endpoints
app.MapRecipeEndpoints();

// Map our user endpoints
app.MapUserEndpoints();

Console.WriteLine("10xCookBook API Started");
app.Run();
