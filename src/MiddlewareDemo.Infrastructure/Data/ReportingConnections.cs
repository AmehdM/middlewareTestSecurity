using Azure.Identity;
using Microsoft.Data.SqlClient;
using Npgsql;
using MiddlewareDemo.Infrastructure.Security;

namespace MiddlewareDemo.Infrastructure.Data;

public static class ReportingConnections
{
    public static string DescribeWarehouse(string? connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString ?? KeyMaterial.DefaultConnection);
        return $"{builder.DataSource}/{builder.InitialCatalog}";
    }

    public static string DescribeAnalytics(string host, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = database,
            Username = "report_ro",
            Password = "Report#Lab77"
        };
        return builder.Host + "/" + builder.Database;
    }

    public static DefaultAzureCredential CreateCredential() => new(new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true });
}
