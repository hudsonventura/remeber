using Microsoft.EntityFrameworkCore;
using agent.Data;
using agent.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=agent.db";

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "An error occurred while migrating the database.");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Generate initial pairing code on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Check if there's an active code, if not generate one
    var hasActiveCode = dbContext.PairingCodes
        .Any(pc => pc.expires_at > DateTime.UtcNow);
    
    if (!hasActiveCode)
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        var expiresAt = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10));

        var pairingCode = new PairingCode
        {
            code = code,
            created_at = DateTime.UtcNow,
            expires_at = expiresAt
        };

        dbContext.PairingCodes.Add(pairingCode);
        dbContext.SaveChanges();
        
        logger.LogInformation("=== PAIRING CODE GENERATED ===");
        logger.LogInformation("Code: {PairingCode}", code);
        logger.LogInformation("Valid for 10 minutes");
        logger.LogInformation("==============================");
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("  PAIRING CODE: " + code);
        Console.WriteLine("  Valid for 10 minutes");
        Console.WriteLine("  Use this code to pair the agent");
        Console.WriteLine("========================================\n");
    }
    else
    {
        var activeCode = dbContext.PairingCodes
            .Where(pc => pc.expires_at > DateTime.UtcNow)
            .OrderByDescending(pc => pc.created_at)
            .First();
        
        var timeRemaining = activeCode.expires_at - DateTime.UtcNow;
        var minutesRemaining = (int)timeRemaining.TotalMinutes;
        
        logger.LogInformation("=== ACTIVE PAIRING CODE EXISTS ===");
        logger.LogInformation("Code: {PairingCode}", activeCode.code);
        logger.LogInformation("Expires at: {ExpiresAt}", activeCode.expires_at);
        logger.LogInformation("Time remaining: {MinutesRemaining} minutes", minutesRemaining);
        logger.LogInformation("==================================");
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("  ⚠️  ACTIVE PAIRING CODE EXISTS");
        Console.WriteLine("  Code: " + activeCode.code);
        Console.WriteLine("  Expires at: " + activeCode.expires_at.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        Console.WriteLine("  Time remaining: " + minutesRemaining + " minutes");
        Console.WriteLine("  Use this code to pair the agent");
        Console.WriteLine("========================================\n");
    }
}

app.Run();
