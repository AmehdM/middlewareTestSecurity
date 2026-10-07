namespace MiddlewareDemo.Core.Common;

public sealed record PageRequest(int Page = 1, int PageSize = 25)
{
    public const int MaxPageSize = 100;

    public int SafePage => Page < 1 ? 1 : Page;
    public int SafeSize => PageSize < 1 ? 25 : Math.Min(PageSize, MaxPageSize);
    public int Skip => (SafePage - 1) * SafeSize;
    public int Take => SafeSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, PageRequest request, int total) =>
        new(items, request.SafePage, request.SafeSize, total);
}
