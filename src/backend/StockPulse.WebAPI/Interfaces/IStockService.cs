using StockPulse.WebAPI.Models;

namespace StockPulse.WebAPI.Interfaces;

public interface IStockService
{
    Task<IEnumerable<StockTick>> GetRecentLogsAsync(int count);
}
