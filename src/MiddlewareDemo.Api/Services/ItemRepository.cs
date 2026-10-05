using Microsoft.Data.Sqlite;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class ItemRepository
{
    private readonly string _connectionString =
        $"Data Source={Path.Combine(Path.GetTempPath(), "middleware-demo.db")}";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _initialized;

    public async Task<IReadOnlyList<Item>> GetPageAsync(int page, int pageSize)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, price FROM items ORDER BY id LIMIT $limit OFFSET $offset";
        command.Parameters.AddWithValue("$limit", pageSize);
        command.Parameters.AddWithValue("$offset", (page - 1) * pageSize);
        return await ReadItemsAsync(command);
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(string name)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, price FROM items WHERE name LIKE $pattern ORDER BY id LIMIT 100";
        command.Parameters.AddWithValue("$pattern", $"%{name}%");
        return await ReadItemsAsync(command);
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, price FROM items WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return (await ReadItemsAsync(command)).FirstOrDefault();
    }

    public async Task<IReadOnlyDictionary<int, Item>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0)
        {
            return new Dictionary<int, Item>();
        }

        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        var placeholders = new string[distinct.Length];
        for (var i = 0; i < distinct.Length; i++)
        {
            placeholders[i] = $"$id{i}";
            command.Parameters.AddWithValue(placeholders[i], distinct[i]);
        }

        command.CommandText = $"SELECT id, name, price FROM items WHERE id IN ({string.Join(",", placeholders)})";
        return (await ReadItemsAsync(command)).ToDictionary(i => i.Id);
    }

    public async Task<Item> AddAsync(string name, double price)
    {
        await using var connection = await OpenAsync();
        return await InsertAsync(connection, name, price);
    }

    private async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnsureCreatedAsync(connection);
        return connection;
    }

    private async Task EnsureCreatedAsync(SqliteConnection connection)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE IF NOT EXISTS items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, price REAL NOT NULL)";
            await create.ExecuteNonQueryAsync();

            await using var count = connection.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM items";
            if (Convert.ToInt32(await count.ExecuteScalarAsync()) == 0)
            {
                await InsertAsync(connection, "Keyboard", 49.9);
                await InsertAsync(connection, "Mouse", 19.5);
                await InsertAsync(connection, "Monitor", 189.0);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task<Item> InsertAsync(SqliteConnection connection, string name, double price)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO items (name, price) VALUES ($name, $price); SELECT last_insert_rowid()";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$price", price);
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return new Item(id, name, price);
    }

    private static async Task<List<Item>> ReadItemsAsync(SqliteCommand command)
    {
        var items = new List<Item>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Item(reader.GetInt32(0), reader.GetString(1), reader.GetDouble(2)));
        }

        return items;
    }
}
