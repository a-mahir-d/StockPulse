using StackExchange.Redis;
using StockPulse.WebAPI.Models;
using System.Text.Json;

namespace StockPulse.WebAPI.BackgroundServices;

public enum SimulatorSpeed
{
    Stopped = 0,
    Slow = 1,
    Medium = 3,
    Fast = 5
}

public class StockSimulatorWorker(ILogger<StockSimulatorWorker> logger, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly ISubscriber _redisPubSub = redis.GetSubscriber();
    private SimulatorSpeed _currentSpeed = SimulatorSpeed.Stopped;
    private readonly Lock _speedLock = new();

    private readonly List<StockTick> _stocks =
    [
        new StockTick("AAPL", 182.50m),
        new StockTick("MSFT", 415.20m),
        new StockTick("TSLA", 170.10m),
        new StockTick("NVDA", 875.00m),
        new StockTick("BTC", 67250.00m)
    ];

    public void ChangeSpeed(SimulatorSpeed newSpeed)
    {
        lock (_speedLock)
        {
            _currentSpeed = newSpeed;
            logger.LogInformation("Simülasyon hızı güncellendi: {Speed}", newSpeed);
        }
    }

    public SimulatorSpeed GetCurrentSpeed()
    {
        lock (_speedLock)
        {
            return _currentSpeed;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stock Simulator Worker başlatıldı.");
        while (!stoppingToken.IsCancellationRequested)
        {
            SimulatorSpeed activeSpeed;
            lock (_speedLock)
            {
                activeSpeed = _currentSpeed;
            }

            if (activeSpeed == SimulatorSpeed.Stopped)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            int delayMilliseconds = 1000 / (int)activeSpeed;

            try
            {
                await RunSimulationCycleAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Simülasyon döngüsünde bir hata oluştu.");
            }

            await Task.Delay(delayMilliseconds, stoppingToken);
        }
    }

    private async Task RunSimulationCycleAsync()
    {
        foreach (var stock in _stocks)
        {
            decimal changePercent = ((decimal)Random.Shared.NextDouble() - 0.5m) * 0.008m;
            stock.Price += stock.Price * changePercent;
            stock.Price = Math.Round(stock.Price, 2);
            stock.LastUpdated = DateTime.UtcNow;

            string jsonPayload = JsonSerializer.Serialize(stock);

            await _redisDb.StringSetAsync($"stock:{stock.Symbol}", jsonPayload);

            await _redisDb.ListLeftPushAsync("stocks:recent_logs", jsonPayload);
            await _redisDb.ListTrimAsync("stocks:recent_logs", 0, 499);

            await _redisPubSub.PublishAsync(RedisChannel.Literal("stocks:ticks"), jsonPayload);
        }
    }
}
