namespace SkillSnap.Shared.Models;

/// <summary>
/// Generic wrapper for paginated API responses.
/// Provides pagination metadata along with the data items.
/// </summary>
/// <typeparam name="T">The type of items in the paginated result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Indicates whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates whether there is a next page available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
