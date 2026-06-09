using Dapper;
using StockPulse.WebAPI.Context;
using System.Data;

namespace StockPulse.WebAPI.BackgroundServices;

public sealed class DatabaseCleaningWorker(IServiceScopeFactory scopeFactory, ILogger<DatabaseCleaningWorker> logger) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[Database Cleanup] Background worker started.");
        logger.LogInformation("[Database Cleanup] Initial startup database flush triggered...");
        await ReseStocksDatabaseAsync(stoppingToken);

        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation("[Database Cleanup] Scheduled hourly database flush triggered...");
                await ReseStocksDatabaseAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[Database Cleanup] Worker is stopping due to application shutdown.");
        }
    }

    private async Task ReseStocksDatabaseAsync(CancellationToken cancellationToken)
    {
        var currentUtcHour = DateTime.UtcNow.Hour;
        logger.LogInformation("[Database Cleanup] Database truncation sequence starting at {Hour}:00 UTC...", currentUtcHour);

        using var scope = scopeFactory.CreateScope();
        var dapperContext = scope.ServiceProvider.GetRequiredService<DapperContext>();

        const string truncateQuery = "TRUNCATE TABLE Stocks RESTART IDENTITY CASCADE;";

        try
        {
            using IDbConnection connection = dapperContext.CreateConnection();
            await connection.ExecuteAsync(new CommandDefinition(truncateQuery, cancellationToken: cancellationToken));
            logger.LogInformation("[Database Cleanup] Stocks table has been successfully truncated and identity reset.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Database Cleanup] Critical error occurred while truncating Stocks table!");
        }
    }
}
