using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using server.Data;
using server.HostedServices;
using server.Logging;
using server.Services;

var builder = WebApplication.CreateBuilder(args);

var workingDirectory = Directory.GetCurrentDirectory()+ "teste";


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

// Configure CORS - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Ensure data directory exists
var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
if (!Directory.Exists(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
}

// Helper function to resolve database paths
string ResolveDbPath(string connectionString)
{
    if (connectionString.StartsWith("Data Source="))
    {
        var dbPath = connectionString.Substring("Data Source=".Length);
        // If path contains "data/", resolve it to the data directory
        if (dbPath.StartsWith("data/") || dbPath.StartsWith("data\\"))
        {
            var fileName = Path.GetFileName(dbPath);
            dbPath = Path.Combine(dataDirectory, fileName);
        }
        // If it's a relative path, make it relative to data directory
        else if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(dataDirectory, dbPath);
        }
        return $"Data Source={dbPath}";
    }
    return connectionString;
}

// Configure Entity Framework Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlite(ResolveDbPath(connectionString)));

// Configure separate SQLite database for logs
var logsConnectionString = builder.Configuration.GetConnectionString("LogsConnection")
    ?? "Data Source=data/logs.db";

builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseSqlite(ResolveDbPath(logsConnectionString)));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Register custom logger provider
// Clear default providers and add custom logger
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CustomLoggerProvider());

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<ITokenStore, TokenStore>();
builder.Services.AddScoped<BackupPlanExecutor>();

// Register hosted services
builder.Services.AddHostedService<BackupRunner>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// CORS must be before UseHttpsRedirection
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    // Disable HTTPS redirection in development to allow HTTP requests
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        // In development, you might want to throw to see the error
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Protected endpoint example
app.MapGet("/api/protected", () =>
{
    return Results.Ok(new { message = "This is a protected endpoint", timestamp = DateTime.UtcNow });
})
.RequireAuthorization()
.WithName("GetProtectedData");



app.Run();
