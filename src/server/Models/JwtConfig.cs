using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("jwt_config")]
public class JwtConfig
{
    public int id { get; set; }
    public string secretKey { get; set; } = string.Empty;
    public string issuer { get; set; } = string.Empty;
    public string audience { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
}

