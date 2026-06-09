using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using StockPulse.WebAPI.Hubs;
using StockPulse.WebAPI.Interfaces;
using StockPulse.WebAPI.Models;
using System.Text.Json;

namespace StockPulse.WebAPI.BackgroundServices;

public class RedisRouteToSignalRWorker(IConnectionMultiplexer redis, IHubContext<StockHub, IStockClient> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(RedisChannel.Literal("stocks:ticks"), async (channel, message) =>
        {
            if (!message.HasValue) return;

            try
            {
                var stockTick = JsonSerializer.Deserialize<StockTick>(message.ToString());

                if (stockTick != null)
                {
                    await hubContext.Clients.All.ReceiveStockTick(stockTick);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisRouteToSignalR] Hata oluştu: {ex.Message}");
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
