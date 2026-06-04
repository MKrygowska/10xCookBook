using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using _10x_cookbook_backend.Data;
using _10x_cookbook_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

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

// Auto-apply EF migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Database migration failed: {ex.Message}");
    // Don't crash the app — health endpoint will surface this
}

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

// Health check endpoint — diagnose DB and config without logs
app.MapGet("/api/health", (AppDbContext db, IConfiguration config) =>
{
    var results = new Dictionary<string, string>();

    // Check DB connectivity AND schema (query Users table)
    try
    {
        var userCount = db.Users.Count();
        results["database"] = $"ok (Users table accessible, {userCount} rows)";
    }
    catch (Exception ex)
    {
        results["database"] = $"error: {ex.Message}";
    }

    // Check JWT secret (without revealing it)
    var jwtSecret = config["JwtSettings:Secret"];
    results["jwtSecret"] = string.IsNullOrEmpty(jwtSecret) ? "missing"
        : jwtSecret == "YOUR_JWT_SECRET_PLACEHOLDER" ? "placeholder - not configured"
        : $"set ({jwtSecret.Length} chars)";

    // Check connection string (without revealing credentials)
    var connStr = config.GetConnectionString("DefaultConnection");
    results["connectionString"] = string.IsNullOrEmpty(connStr) ? "missing" : "set";

    // Check environment
    results["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "not set (defaults to Production)";

    var allOk = results["database"].StartsWith("ok")
        && results["jwtSecret"].StartsWith("set")
        && results["connectionString"] == "set";

    return Results.Json(new { status = allOk ? "healthy" : "degraded", checks = results });
}).AllowAnonymous();

app.MapControllers();

Console.WriteLine("10xCookBook API Started");
app.Run();

// Trigger backend redeployment to live Sweden Central App Service to load latest Controllers endpoints.

public partial class Program { }
