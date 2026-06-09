using Dapper;
using StackExchange.Redis;
using StockPulse.WebAPI.Context;
using StockPulse.WebAPI.Models;
using System.Data;
using System.Text.Json;

namespace StockPulse.WebAPI.BackgroundServices;

public class StockPersistenceWorker(ILogger<StockPersistenceWorker> logger, IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly string[] _symbols = ["AAPL", "MSFT", "TSLA", "NVDA", "BTC"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[Stock Persistence] Background worker started.");
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    logger.LogInformation("Dapper kalıcılaştırma döngüsü tetiklendi. Redis'ten güncel fiyatlar okunuyor...");
                    await PersistStocksToDatabaseAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Veritabanına kaydetme işlemi sırasında bir hata oluştu.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[Stock Persistence] Worker is stopping due to application shutdown.");
        }
    }

    private async Task PersistStocksToDatabaseAsync(CancellationToken cancellationToken)
    {
        var stocksToUpdate = new List<StockTick>();

        foreach (var symbol in _symbols)
        {
            var redisValue = await _redisDb.StringGetAsync($"stock:{symbol}");

            if (redisValue.HasValue)
            {
                var stock = JsonSerializer.Deserialize<StockTick>(redisValue.ToString());
                if (stock != null)
                {
                    stocksToUpdate.Add(stock);
                }
            }
        }

        if (stocksToUpdate.Count == 0)
        {
            logger.LogWarning("Redis'te güncellenecek stok verisi bulunamadı.");
            return;
        }

        string upsertSql = """
            INSERT INTO Stocks (Symbol, Price, LastUpdated)
            VALUES (@Symbol, @Price, @LastUpdated)
            ON CONFLICT (Symbol) 
            DO UPDATE SET 
                Price = EXCLUDED.Price, 
                LastUpdated = EXCLUDED.LastUpdated;
        """;

        using var scope = scopeFactory.CreateScope();
        var dapperContext = scope.ServiceProvider.GetRequiredService<DapperContext>();
        using IDbConnection connection = dapperContext.CreateConnection();

        var command = new CommandDefinition(
            commandText: upsertSql,
            parameters: stocksToUpdate,
            cancellationToken: cancellationToken
        );

        int affectedRows = await connection.ExecuteAsync(command);
        logger.LogInformation("Dapper başarıyla çalıştı. {Count} adet kayıt işlendi.", affectedRows);
    }
}