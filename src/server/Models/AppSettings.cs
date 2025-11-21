using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("app_settings")]
public class AppSettings
{
    public Guid id { get; set; } = Guid.NewGuid();
    public string key { get; set; } = string.Empty;
    public string value { get; set; } = string.Empty;
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
}

