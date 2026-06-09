using System.ComponentModel.DataAnnotations;

namespace StockPulse.WebAPI.Models.Settings;

public class ClientSettings
{
    [Required]
    public required string BaseUrl { get; set; }
}

