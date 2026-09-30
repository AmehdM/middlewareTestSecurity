namespace MiddlewareDemo.Api;

public sealed class ItemStore
{
    private readonly object _lock = new();
    private readonly List<Item> _items =
    [
        new Item(1, "Alpha"),
        new Item(2, "Beta"),
        new Item(3, "Gamma"),
    ];

    public IReadOnlyList<Item> GetAll()
    {
        lock (_lock)
        {
            return _items.ToArray();
        }
    }

    public Item Add(string name)
    {
        lock (_lock)
        {
            var item = new Item(_items.Count + 1, name);
            _items.Add(item);
            return item;
        }
    }
}
