namespace MiddlewareDemo.Api;

public sealed record Item(int Id, string Name);

public sealed record CreateItemRequest(string? Name);
