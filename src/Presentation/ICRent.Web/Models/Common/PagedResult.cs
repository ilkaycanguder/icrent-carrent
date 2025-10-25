namespace ICRent.Web.Models.Common
{
    public sealed class PagedResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public int TotalCount { get; init; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
