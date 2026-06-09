using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using StockPulse.WebAPI.Models.Settings;
using System.Data;

namespace StockPulse.WebAPI.Services;

public sealed class DatabaseHostedService(IOptions<DbSettings> options, ILogger<DatabaseHostedService> logger) : IHostedService
{
    private readonly DbSettings _settings = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[Database Initializer] Checking database schema on startup...");

        const string createTableQuery = """
            CREATE TABLE IF NOT EXISTS Stocks (
                Symbol VARCHAR(10) PRIMARY KEY,
                Price DECIMAL(18, 2) NOT NULL,
                LastUpdated TIMESTAMP NOT NULL
            );
        """;

        try
        {
            using IDbConnection connection = new NpgsqlConnection(_settings.ConnectionString);
            await connection.ExecuteAsync(new CommandDefinition(createTableQuery, cancellationToken: cancellationToken));
            logger.LogInformation("[Database Initializer] Database schema is verified and ready.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "[Database Initializer] Fatal error occurred while initializing database schema!");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
