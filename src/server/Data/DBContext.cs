using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data;

public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    private readonly IConfiguration _configuration;
    private readonly ILogger<DBContext>? _logger;

    public DBContext(DbContextOptions<DBContext> options, IConfiguration configuration, ILogger<DBContext> logger)
        : base(options)
    {
        _configuration = configuration;
        _logger = logger;
    }
 
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseNpgsql(connectionString);
            _logger?.LogInformation($"Connection string: {connectionString}");
        }
    }


    public DbSet<Agent> Agents { get; set; }
    public DbSet<BackupPlan> BackupPlans { get; set; }


}

