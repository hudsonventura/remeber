using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using server.Data;
using server.Models;

namespace server.Controllers;

[ApiController]
public class SettingsController : ControllerBase
{
    private readonly DBContext _context;
    private readonly LogDbContext _logContext;
    private readonly ILogger<SettingsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public SettingsController(
        DBContext context, 
        LogDbContext logContext,
        ILogger<SettingsController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logContext = logContext;
        _logger = logger;
        _environment = environment;
    }

    private string ResolveDbPath(string connectionString)
    {
        if (connectionString.StartsWith("Data Source="))
        {
            var dbPath = connectionString.Substring("Data Source=".Length);
            // If path contains "data/", resolve it to the data directory
            var dataDirectory = Path.Combine(_environment.ContentRootPath, "data");
            if (!Directory.Exists(dataDirectory))
            {
                dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
            }

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
            return dbPath;
        }
        return connectionString;
    }

    [HttpGet("/api/settings/log-retention-date")]
    [ProducesResponseType(typeof(LogRetentionDateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogRetentionDate()
    {
        try
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.key == "LogRetentionDate");

            var response = new LogRetentionDateResponse
            {
                Date = setting != null && DateTime.TryParse(setting.value, out var date) ? date : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log retention date");
            return StatusCode(500, new { message = "An error occurred while retrieving log retention date", error = ex.Message });
        }
    }

    [HttpPost("/api/settings/log-retention-date")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetLogRetentionDate([FromBody] LogRetentionDateRequest request)
    {
        try
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.key == "LogRetentionDate");

            if (setting == null)
            {
                setting = new AppSettings
                {
                    id = Guid.NewGuid(),
                    key = "LogRetentionDate",
                    value = request.Date?.ToString("yyyy-MM-dd") ?? string.Empty,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };
                _context.AppSettings.Add(setting);
            }
            else
            {
                setting.value = request.Date?.ToString("yyyy-MM-dd") ?? string.Empty;
                setting.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Log retention date saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving log retention date");
            return StatusCode(500, new { message = "An error occurred while saving log retention date", error = ex.Message });
        }
    }

    [HttpPost("/api/settings/delete-logs-before-date")]
    [ProducesResponseType(typeof(DeleteLogsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLogsBeforeDate([FromBody] DeleteLogsRequest request)
    {
        try
        {
            if (!request.BeforeDate.HasValue)
            {
                return BadRequest(new { message = "BeforeDate is required" });
            }

            var beforeDate = request.BeforeDate.Value.Date;

            // Get executions that started before the date
            var executionsToDelete = await _logContext.BackupExecutions
                .Where(e => e.startDateTime < beforeDate)
                .Select(e => e.id)
                .ToListAsync();

            // Delete log entries for those executions
            var logsDeleted = await _logContext.LogEntries
                .Where(log => executionsToDelete.Contains(log.executionId))
                .CountAsync();

            _logContext.LogEntries.RemoveRange(
                _logContext.LogEntries.Where(log => executionsToDelete.Contains(log.executionId))
            );

            // Delete executions
            _logContext.BackupExecutions.RemoveRange(
                _logContext.BackupExecutions.Where(e => executionsToDelete.Contains(e.id))
            );

            await _logContext.SaveChangesAsync();

            // Get database file path and perform VACUUM to free disk space
            var logsConnectionString = "Data Source=data/logs.db";
            var dbPath = ResolveDbPath(logsConnectionString);

            // Perform VACUUM to reclaim disk space
            if (!string.IsNullOrEmpty(dbPath) && System.IO.File.Exists(dbPath))
            {
                try
                {
                    // Close the current connection
                    await _logContext.Database.CloseConnectionAsync();

                    // Execute VACUUM using raw SQL
                    using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "VACUUM";
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    _logger.LogInformation("VACUUM completed successfully on {DbPath}", dbPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to execute VACUUM on {DbPath}", dbPath);
                    // Continue even if VACUUM fails
                }
            }

            var response = new DeleteLogsResponse
            {
                ExecutionsDeleted = executionsToDelete.Count,
                LogsDeleted = logsDeleted,
                Message = $"Successfully deleted {executionsToDelete.Count} executions and {logsDeleted} log entries"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting logs before date");
            return StatusCode(500, new { message = "An error occurred while deleting logs", error = ex.Message });
        }
    }
}

public class LogRetentionDateRequest
{
    public DateTime? Date { get; set; }
}

public class LogRetentionDateResponse
{
    public DateTime? Date { get; set; }
}

public class DeleteLogsRequest
{
    public DateTime? BeforeDate { get; set; }
}

public class DeleteLogsResponse
{
    public int ExecutionsDeleted { get; set; }
    public int LogsDeleted { get; set; }
    public string Message { get; set; } = string.Empty;
}

