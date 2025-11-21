using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("notification")]
public class Notification
{
    public Guid id { get; set; } = Guid.NewGuid();
    public string type { get; set; } = string.Empty; // "BackupCompleted", "SimulationCompleted"
    public string title { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public Guid? backupPlanId { get; set; }
    public Guid? executionId { get; set; }
    public bool isRead { get; set; } = false;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
}

