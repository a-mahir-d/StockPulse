using System.ComponentModel.DataAnnotations;

namespace StockPulse.WebAPI.Models.Settings;

public class RedisSettings
{
    [Required]
    public required string ConnectionString { get; set; }
}
