namespace ecommercesite.ViewModels
{
    /// <summary>
    /// Generic paged-result container used by list pages and services.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>The rows on the current page.</summary>
        public IReadOnlyList<T> Items { get; set; } = [];

        /// <summary>Total row count across all pages.</summary>
        public int TotalCount { get; set; }

        /// <summary>1-indexed current page.</summary>
        public int Page { get; set; }

        /// <summary>Number of rows per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of pages (always ≥ 1).</summary>
        public int TotalPages => PageSize <= 0
            ? 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>True when a previous page exists.</summary>
        public bool HasPrevious => Page > 1;

        /// <summary>True when a next page exists.</summary>
        public bool HasNext => Page < TotalPages;
    }
}