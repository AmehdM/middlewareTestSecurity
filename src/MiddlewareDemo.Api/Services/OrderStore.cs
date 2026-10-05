using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class OrderStore
{
    private readonly object _lock = new();
    private readonly List<Order> _orders = [];

    public IReadOnlyList<Order> All()
    {
        lock (_lock)
        {
            return _orders.ToArray();
        }
    }

    public IReadOnlyList<Order> ForOwner(int ownerId)
    {
        lock (_lock)
        {
            return _orders.Where(o => o.OwnerId == ownerId).ToArray();
        }
    }

    public Order Add(int ownerId, string customer, int itemId, int quantity)
    {
        lock (_lock)
        {
            var order = new Order(_orders.Count + 1, ownerId, customer, itemId, quantity);
            _orders.Add(order);
            return order;
        }
    }
}
