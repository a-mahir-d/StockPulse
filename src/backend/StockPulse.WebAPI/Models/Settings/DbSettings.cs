using System.ComponentModel.DataAnnotations;

namespace StockPulse.WebAPI.Models.Settings;

public class DbSettings
{
    [Required]
    public required string ConnectionString { get; set; }
}
