namespace TaskFlow.Api.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static (int page, int size) Normalize(int page, int size) =>
        (Math.Max(page, 1), Math.Clamp(size, 1, 100));
}
