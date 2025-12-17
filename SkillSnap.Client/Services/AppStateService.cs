using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

/// <summary>
/// Client-side state management service with caching and event notifications.
/// Reduces API calls by caching frequently accessed data and notifying components of changes.
/// </summary>
public class AppStateService
{
    // Cache storage
    private List<PortfolioUser>? _cachedPortfolioUsers;
    private List<Project>? _cachedProjects;
    private List<Skill>? _cachedSkills;

    // Cache timestamps for invalidation
    private DateTime? _portfolioUsersCacheTime;
    private DateTime? _projectsCacheTime;
    private DateTime? _skillsCacheTime;

    // Cache expiration time (5 minutes)
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Events for notifying components of data changes
    public event Action? OnPortfolioUsersChanged;
    public event Action? OnProjectsChanged;
    public event Action? OnSkillsChanged;

    // PortfolioUsers cache methods
    public List<PortfolioUser>? GetCachedPortfolioUsers()
    {
        // Check if we have cached data and a valid timestamp
        if (_cachedPortfolioUsers != null && _portfolioUsersCacheTime != null)
        {
            // Calculate age of cached data and compare to expiration threshold
            if (DateTime.UtcNow - _portfolioUsersCacheTime.Value < _cacheExpiration)
            {
                return _cachedPortfolioUsers;  // Cache is still valid
            }
            // If we reach here, cache has expired - will return null below
        }
        return null;  // No cache available or cache expired
    }

    public void SetCachedPortfolioUsers(List<PortfolioUser> users)
    {
        _cachedPortfolioUsers = users;
        _portfolioUsersCacheTime = DateTime.UtcNow;
    }

    public void InvalidatePortfolioUsersCache()
    {
        _cachedPortfolioUsers = null;
        _portfolioUsersCacheTime = null;
    }

    public void NotifyPortfolioUsersChanged()
    {
        // Clear cached data to force fresh fetch on next access
        InvalidatePortfolioUsersCache();
        
        // Notify all subscribed components (e.g., lists, grids) to refresh their data
        // Using null-conditional operator to safely invoke event
        OnPortfolioUsersChanged?.Invoke();
    }

    // Projects cache methods
    public List<Project>? GetCachedProjects()
    {
        if (_cachedProjects != null && _projectsCacheTime != null)
        {
            if (DateTime.UtcNow - _projectsCacheTime.Value < _cacheExpiration)
            {
                return _cachedProjects;
            }
        }
        return null;
    }

    public void SetCachedProjects(List<Project> projects)
    {
        _cachedProjects = projects;
        _projectsCacheTime = DateTime.UtcNow;
    }

    public void InvalidateProjectsCache()
    {
        _cachedProjects = null;
        _projectsCacheTime = null;
    }

    public void NotifyProjectsChanged()
    {
        InvalidateProjectsCache();
        OnProjectsChanged?.Invoke();
    }

    // Skills cache methods
    public List<Skill>? GetCachedSkills()
    {
        if (_cachedSkills != null && _skillsCacheTime != null)
        {
            if (DateTime.UtcNow - _skillsCacheTime.Value < _cacheExpiration)
            {
                return _cachedSkills;
            }
        }
        return null;
    }

    public void SetCachedSkills(List<Skill> skills)
    {
        _cachedSkills = skills;
        _skillsCacheTime = DateTime.UtcNow;
    }

    public void InvalidateSkillsCache()
    {
        _cachedSkills = null;
        _skillsCacheTime = null;
    }

    public void NotifySkillsChanged()
    {
        InvalidateSkillsCache();
        OnSkillsChanged?.Invoke();
    }

    // Clear all caches (useful for logout scenarios)
    public void ClearAllCaches()
    {
        InvalidatePortfolioUsersCache();
        InvalidateProjectsCache();
        InvalidateSkillsCache();
    }
}
