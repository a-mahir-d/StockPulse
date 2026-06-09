using Microsoft.Extensions.Options;
using Npgsql;
using StockPulse.WebAPI.Models.Settings;
using System.Data;

namespace StockPulse.WebAPI.Context;

public sealed class DapperContext(IOptions<DbSettings> options)
{
    private readonly DbSettings _settings = options.Value;

    public IDbConnection CreateConnection() => new NpgsqlConnection(_settings.ConnectionString);
}
