using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MiddlewareDemo.Infrastructure.Data;

public static class DbExtensions
{
    public static SqliteCommand Command(this SqliteConnection connection, string sql, params (string Name, object? Value)[] args)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in args)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    public static async Task<int> InsertAsync(this SqliteConnection connection, string sql, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var command = connection.Command(sql + "; SELECT last_insert_rowid();", args);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public static string Str(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    public static int Int(this SqliteDataReader reader, string column) => reader.GetInt32(reader.GetOrdinal(column));

    public static decimal Dec(this SqliteDataReader reader, string column) => Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal(column)));

    public static DateTime Date(this SqliteDataReader reader, string column) =>
        DateTime.Parse(reader.Str(column), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static string Iso(this DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);
}
