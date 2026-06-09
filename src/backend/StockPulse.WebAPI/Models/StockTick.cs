namespace StockPulse.WebAPI.Models;

public class StockTick
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public StockTick()
    {
    }

    public StockTick(string symbol, decimal price)
    {
        Symbol = symbol;
        Price = price;
    }
}


