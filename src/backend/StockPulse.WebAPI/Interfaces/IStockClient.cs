using StockPulse.WebAPI.Models;

namespace StockPulse.WebAPI.Interfaces;

public interface IStockClient
{
    Task ReceiveStockTick(StockTick stockTick);
}
