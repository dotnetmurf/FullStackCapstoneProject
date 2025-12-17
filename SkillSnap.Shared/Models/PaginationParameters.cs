namespace SkillSnap.Shared.Models;

/// <summary>
/// Parameters for paginating API requests.
/// Enforces valid page numbers and page sizes.
/// </summary>
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    /// <summary>
    /// The page number to retrieve (1-based, minimum 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The number of items per page (minimum 1, maximum 100, default 20).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>
    /// Validates and normalizes pagination parameters.
    /// </summary>
    public void Validate()
    {
        if (Page < 1) Page = 1;
        if (_pageSize < 1) _pageSize = 1;
        if (_pageSize > MaxPageSize) _pageSize = MaxPageSize;
    }
}
