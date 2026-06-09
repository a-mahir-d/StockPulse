namespace StockPulse.WebAPI.Models;

public class StockTick(string symbol, decimal price)
{
    public string Symbol { get; set; } = symbol;
    public decimal Price { get; set; } = price;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

