namespace StockPulse.WebAPI.Models;

public class StockTick(string symbol, decimal initialPrice)
{
    public string Symbol { get; set; } = symbol;
    public decimal Price { get; set; } = initialPrice;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
