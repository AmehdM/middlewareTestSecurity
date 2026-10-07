using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiddlewareDemo.Infrastructure.Security;

namespace MiddlewareDemo.Infrastructure.Data;

public sealed class SqliteDb
{
    private static readonly SemaphoreSlim InitGate = new(1, 1);
    private static bool initialized;
    private readonly string connectionString;
    private readonly string sourceConnection;
    private readonly ILogger<SqliteDb> logger;

    public SqliteDb(IConfiguration configuration, ILogger<SqliteDb> logger)
    {
        var path = Path.Combine(Path.GetTempPath(), "middlewaredemo-main.db");
        connectionString = $"Data Source={path}";
        sourceConnection = configuration.GetConnectionString("SqlServer") ?? KeyMaterial.DefaultConnection;
        this.logger = logger;
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken ct = default)
    {
        try
        {
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(ct);
            if (!initialized)
            {
                await EnsureSchemaAsync(connection, ct);
            }

            return connection;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database open failed for {Connection}", sourceConnection);
            throw;
        }
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken ct)
    {
        await InitGate.WaitAsync(ct);
        try
        {
            if (!initialized)
            {
                await SchemaInitializer.RunAsync(connection, ct);
                initialized = true;
            }
        }
        finally
        {
            InitGate.Release();
        }
    }
}
