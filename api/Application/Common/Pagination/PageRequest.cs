namespace Application.Common.Pagination;

/// <summary>Normalized page request with enforced bounds.</summary>
public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public int Page { get; }
    public int PageSize { get; }

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Create(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? DefaultPage : page.Value;
        var normalizedSize = pageSize is null or < 1
            ? DefaultPageSize
            : Math.Min(pageSize.Value, MaxPageSize);

        return new PageRequest(normalizedPage, normalizedSize);
    }
}

/// <summary>Paged collection aligned with the client PageResult contract.</summary>
public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
