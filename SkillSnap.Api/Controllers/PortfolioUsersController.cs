using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Api.Services;
using SkillSnap.Shared.DTOs;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

/// <summary>
/// API controller for managing portfolio users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioUsersController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly CacheHelper _cacheHelper;
    private readonly ILogger<PortfolioUsersController> _logger;

    private const string PortfolioUsersCacheKey = "AllPortfolioUsers";
    private const string PortfolioUserCacheKeyPrefix = "PortfolioUser_";
    private const string PortfolioUsersPagedCacheKeyPrefix = "PortfolioUsersPaged_";
    private const string PortfolioUsersTotalCountCacheKey = "PortfolioUsersTotalCount";
    private const string PortfolioUsersPagedCacheKeysListKey = "PortfolioUsersPagedCacheKeys"; // Tracks all paginated cache keys

    public PortfolioUsersController(SkillSnapContext context, CacheHelper cacheHelper, ILogger<PortfolioUsersController> logger)
    {
        _context = context;
        _cacheHelper = cacheHelper;
        _logger = logger;
    }

    /// <summary>
    /// Gets all portfolio users with their related projects and skills.
    /// </summary>
    /// <returns>A list of all portfolio users.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetPortfolioUsers()
    {
        try
        {
            // Try to get from cache
            var cachedUsers = _cacheHelper.TryGetFromCache<List<PortfolioUser>>(PortfolioUsersCacheKey, "PortfolioUsers");
            if (cachedUsers != null)
            {
                return Ok(cachedUsers);
            }

            _logger.LogInformation("PortfolioUsers retrieved from database");
            var users = await _context.PortfolioUsers
                .AsNoTracking()
                .Include(u => u.Projects)
                .Include(u => u.Skills)
                .AsSplitQuery()
                .OrderBy(u => u.Name)
                .ToListAsync();

            // Store in cache
            _cacheHelper.SetCache(PortfolioUsersCacheKey, users, "PortfolioUsers");
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PortfolioUsers");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a lightweight list of portfolio users optimized for list views (no related collections).
    /// Includes only user summary data with project/skill counts for better performance.
    /// </summary>
    /// <returns>A list of portfolio user summaries.</returns>
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PortfolioUserSummaryDto>>> GetPortfolioUsersSummary()
    {
        try
        {
            const string summaryCacheKey = "AllPortfolioUsersSummary";

            // Try to get from cache
            var cachedSummary = _cacheHelper.TryGetFromCache<List<PortfolioUserSummaryDto>>(summaryCacheKey, "PortfolioUser summaries");
            if (cachedSummary != null)
            {
                return Ok(cachedSummary);
            }

            // If not in cache, get from database with optimized projection
            _logger.LogInformation("PortfolioUser summaries retrieved from database");
            var summaries = await _context.PortfolioUsers
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new PortfolioUserSummaryDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    ProjectCount = u.Projects.Count,
                    SkillCount = u.Skills.Count
                })
                .ToListAsync();

            // Store in cache
            _cacheHelper.SetCache(summaryCacheKey, summaries, "PortfolioUser summaries");

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PortfolioUser summaries");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets portfolio users with pagination support (includes related projects and skills).
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <returns>Paginated list of portfolio users with related data.</returns>
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PortfolioUser>>> GetPortfolioUsersPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var cacheKey = $"{PortfolioUsersPagedCacheKeyPrefix}Page{page}_Size{pageSize}";

            // Try to get from cache
            var cachedResult = _cacheHelper.TryGetFromCache<PagedResult<PortfolioUser>>(cacheKey, $"PortfolioUsers page {page}");
            if (cachedResult != null)
            {
                return Ok(cachedResult);
            }

            // Get total count (cached separately)
            var totalCount = await GetOrCacheTotalPortfolioUserCount();

            // Get paginated data
            _logger.LogInformation("PortfolioUsers page {Page} retrieved from database", page);
            var users = await _context.PortfolioUsers
                .AsNoTracking()
                .Include(u => u.Projects)
                .Include(u => u.Skills)
                .AsSplitQuery()
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<PortfolioUser>
            {
                Items = users,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // Cache the result
            _cacheHelper.SetCache(cacheKey, result, $"PortfolioUsers page {page}");

            // Track this cache key for later invalidation
            TrackPagedCacheKey(cacheKey);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated PortfolioUsers");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a specific portfolio user by ID with related projects and skills.
    /// </summary>
    /// <param name="id">The portfolio user ID.</param>
    /// <returns>The requested portfolio user.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PortfolioUser>> GetPortfolioUser(int id)
    {
        try
        {
            var cacheKey = $"{PortfolioUserCacheKeyPrefix}{id}";

            // Try to get from cache
            var cachedUser = _cacheHelper.TryGetFromCache<PortfolioUser>(cacheKey, $"PortfolioUser {id}");
            if (cachedUser != null)
            {
                return Ok(cachedUser);
            }

            _logger.LogInformation("PortfolioUser {Id} retrieved from database", id);
            var user = await _context.PortfolioUsers
                .AsNoTracking()
                .Include(u => u.Projects)
                .Include(u => u.Skills)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound($"PortfolioUser with ID {id} not found.");
            }

            // Store in cache
            _cacheHelper.SetCache(cacheKey, user, $"PortfolioUser {id}");

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PortfolioUser {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new portfolio user.
    /// </summary>
    /// <param name="user">The portfolio user to create.</param>
    /// <returns>The created portfolio user.</returns>
    [HttpPost]
    public async Task<ActionResult<PortfolioUser>> PostPortfolioUser(PortfolioUser user)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.PortfolioUsers.Add(user);
            await _context.SaveChangesAsync();

            // Invalidate all portfolio user caches
            InvalidatePortfolioUserCaches();
            _logger.LogInformation("Created PortfolioUser {Id} and invalidated caches", user.Id);

            return CreatedAtAction(nameof(GetPortfolioUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PortfolioUser");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing portfolio user.
    /// </summary>
    /// <param name="id">The portfolio user ID.</param>
    /// <param name="user">The updated portfolio user data.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPortfolioUser(int id, PortfolioUser user)
    {
        if (id != user.Id)
        {
            return BadRequest("PortfolioUser ID mismatch.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Invalidate all portfolio user caches
            InvalidatePortfolioUserCaches(id);
            _logger.LogInformation("Updated PortfolioUser {Id} and invalidated caches", id);
            
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await PortfolioUserExists(id))
            {
                return NotFound($"PortfolioUser with ID {id} not found.");
            }
            
            _logger.LogError(ex, "Concurrency error updating PortfolioUser {Id}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PortfolioUser {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a portfolio user.
    /// </summary>
    /// <param name="id">The portfolio user ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePortfolioUser(int id)
    {
        try
        {
            var user = await _context.PortfolioUsers.FindAsync(id);
            
            if (user == null)
            {
                return NotFound($"PortfolioUser with ID {id} not found.");
            }

            _context.PortfolioUsers.Remove(user);
            await _context.SaveChangesAsync();

            // Invalidate all portfolio user caches
            InvalidatePortfolioUserCaches(id);
            _logger.LogInformation("Deleted PortfolioUser {Id} and invalidated caches", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting PortfolioUser {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets or caches the total count of portfolio users.
    /// </summary>
    private async Task<int> GetOrCacheTotalPortfolioUserCount()
    {
        return await _cacheHelper.GetOrCacheCountAsync(
            PortfolioUsersTotalCountCacheKey,
            async () => await _context.PortfolioUsers.CountAsync(),
            "PortfolioUsers total count");
    }

    /// <summary>
    /// Invalidates all portfolio user-related caches including paginated results.
    /// </summary>
    private void InvalidatePortfolioUserCaches(int? userId = null)
    {
        // Remove individual user cache if ID provided
        if (userId.HasValue)
        {
            _cacheHelper.RemoveCache($"{PortfolioUserCacheKeyPrefix}{userId.Value}");
        }

        // Remove all general caches and paginated caches
        _cacheHelper.RemoveMultiple(PortfolioUsersCacheKey, "AllPortfolioUsersSummary", PortfolioUsersTotalCountCacheKey);
        _cacheHelper.InvalidatePagedCaches(PortfolioUsersPagedCacheKeysListKey, "PortfolioUsers");
        
        _logger.LogInformation("Invalidated portfolio user caches (all, summary, total count, and paginated)");
    }

    /// <summary>
    /// Tracks a paginated cache key so it can be invalidated later.
    /// </summary>
    private void TrackPagedCacheKey(string cacheKey)
    {
        _cacheHelper.TrackPagedCacheKey(PortfolioUsersPagedCacheKeysListKey, cacheKey);
    }

    private async Task<bool> PortfolioUserExists(int id)
    {
        return await _context.PortfolioUsers.AnyAsync(e => e.Id == id);
    }
}
