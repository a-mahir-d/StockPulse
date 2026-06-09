using StackExchange.Redis;
using StockPulse.WebAPI.Interfaces;
using StockPulse.WebAPI.Models;
using System.Text.Json;

namespace StockPulse.WebAPI.Services;

public class StockService(IConnectionMultiplexer redis) : IStockService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();

    public async Task<IEnumerable<StockTick>> GetRecentLogsAsync(int count)
    {
        var redisResult = await _redisDb.ListRangeAsync("stocks:recent_logs", 0, count - 1);

        if (redisResult.Length == 0)
        {
            return [];
        }

        var logs = new List<StockTick>();
        foreach (var value in redisResult)
        {
            if (value.HasValue)
            {
                var tick = JsonSerializer.Deserialize<StockTick>(value.ToString());
                if (tick != null) logs.Add(tick);
            }
        }

        return logs;
    }
}
