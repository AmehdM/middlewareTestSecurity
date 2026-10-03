using System.Collections.Concurrent;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public static class ResponseCache
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<Item>> Entries = new();

    public static IReadOnlyList<Item> GetOrAdd(string key, Func<IReadOnlyList<Item>> factory) =>
        Entries.GetOrAdd(key, _ => factory());
}
