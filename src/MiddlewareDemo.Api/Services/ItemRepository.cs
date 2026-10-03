using Microsoft.Data.Sqlite;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class ItemRepository
{
    private readonly string _connectionString =
        $"Data Source={Path.Combine(Path.GetTempPath(), "middleware-demo.db")}";

    public ItemRepository()
    {
        using var connection = Open();
        using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE IF NOT EXISTS items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, price REAL NOT NULL)";
        create.ExecuteNonQuery();

        using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM items";
        if (Convert.ToInt32(count.ExecuteScalar()) == 0)
        {
            Add("Keyboard", 49.9);
            Add("Mouse", 19.5);
            Add("Monitor", 189.0);
        }
    }

    public IReadOnlyList<Item> GetAll()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, price FROM items";
        return ReadItems(command);
    }

    public IReadOnlyList<Item> Search(string name)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT id, name, price FROM items WHERE name LIKE '%{name}%'";
        return ReadItems(command);
    }

    public Item? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, price FROM items WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return ReadItems(command).FirstOrDefault();
    }

    public Item Add(string name, double price)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO items (name, price) VALUES ($name, $price); SELECT last_insert_rowid()";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$price", price);
        var id = Convert.ToInt32(command.ExecuteScalar());
        return new Item(id, name, price);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static List<Item> ReadItems(SqliteCommand command)
    {
        var items = new List<Item>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new Item(reader.GetInt32(0), reader.GetString(1), reader.GetDouble(2)));
        }

        return items;
    }
}
