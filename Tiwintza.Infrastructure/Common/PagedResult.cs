namespace Tiwintza.Infrastructure.Common;

public sealed class PagedResult<T>(IReadOnlyList<T> items, int total, int page, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int Total { get; } = total;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
}
