using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Api.Services;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

/// <summary>
/// API controller for managing skills.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly CacheHelper _cacheHelper;
    private readonly ILogger<SkillsController> _logger;

    private const string SkillsCacheKey = "AllSkills";
    private const string SkillCacheKeyPrefix = "Skill_";
    private const string SkillsPagedCacheKeyPrefix = "SkillsPaged_";
    private const string SkillsTotalCountCacheKey = "SkillsTotalCount";
    private const string SkillsPagedCacheKeysListKey = "SkillsPagedCacheKeys"; // Tracks all paginated cache keys

    public SkillsController(
        SkillSnapContext context,
        CacheHelper cacheHelper,
        ILogger<SkillsController> logger)
    {
        _context = context;
        _cacheHelper = cacheHelper;
        _logger = logger;
    }

    /// <summary>
    /// Gets all skills.
    /// </summary>
    /// <returns>A list of all skills.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        try
        {
            // Try to get from cache
            var cachedSkills = _cacheHelper.TryGetFromCache<List<Skill>>(SkillsCacheKey, "Skills");
            if (cachedSkills != null)
            {
                return Ok(cachedSkills);
            }

            // If not in cache, get from database
            _logger.LogInformation("Skills retrieved from database");
            var skills = await _context.Skills
                .AsNoTracking()
                .Include(s => s.PortfolioUser)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Store in cache
            _cacheHelper.SetCache(SkillsCacheKey, skills, "Skills");
            
            return Ok(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skills");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets skills with pagination support.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <returns>Paginated list of skills.</returns>
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<Skill>>> GetSkillsPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var cacheKey = $"{SkillsPagedCacheKeyPrefix}Page{page}_Size{pageSize}";

            // Try to get from cache
            var cachedResult = _cacheHelper.TryGetFromCache<PagedResult<Skill>>(cacheKey, $"Skills page {page}");
            if (cachedResult != null)
            {
                return Ok(cachedResult);
            }

            // Get total count (cached separately)
            var totalCount = await GetOrCacheTotalSkillCount();

            // Get paginated data
            _logger.LogInformation("Skills page {Page} retrieved from database", page);
            var skills = await _context.Skills
                .AsNoTracking()
                .Include(s => s.PortfolioUser)
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<Skill>
            {
                Items = skills,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // Cache the result
            _cacheHelper.SetCache(cacheKey, result, $"Skills page {page}");
            
            // Track this cache key for later invalidation
            TrackPagedCacheKey(cacheKey);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated skills");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a specific skill by ID.
    /// </summary>
    /// <param name="id">The skill ID.</param>
    /// <returns>The requested skill.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        try
        {
            var cacheKey = $"{SkillCacheKeyPrefix}{id}";

            // Try to get from cache
            var cachedSkill = _cacheHelper.TryGetFromCache<Skill>(cacheKey, $"Skill {id}");
            if (cachedSkill != null)
            {
                return Ok(cachedSkill);
            }

            // If not in cache, get from database
            _logger.LogInformation("Skill {SkillId} retrieved from database", id);
            var skill = await _context.Skills
                .AsNoTracking()
                .Include(s => s.PortfolioUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                return NotFound($"Skill with ID {id} not found.");
            }

            // Store in cache
            _cacheHelper.SetCache(cacheKey, skill, $"Skill {id}");

            return Ok(skill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill {SkillId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new skill.
    /// </summary>
    /// <param name="skill">The skill to create.</param>
    /// <returns>The created skill.</returns>
    [HttpPost]
    public async Task<ActionResult<Skill>> PostSkill(Skill skill)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify PortfolioUser exists
            var userExists = await _context.PortfolioUsers.AnyAsync(u => u.Id == skill.PortfolioUserId);
            if (!userExists)
            {
                return BadRequest($"PortfolioUser with ID {skill.PortfolioUserId} does not exist.");
            }

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            // Invalidate all skill caches
            InvalidateSkillCaches();
            _logger.LogInformation("Created skill {SkillId} and invalidated caches", skill.Id);

            return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing skill.
    /// </summary>
    /// <param name="id">The skill ID.</param>
    /// <param name="skill">The updated skill data.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSkill(int id, Skill skill)
    {
        if (id != skill.Id)
        {
            return BadRequest("Skill ID mismatch.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _context.Entry(skill).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Invalidate all skill caches
            InvalidateSkillCaches(id);
            _logger.LogInformation("Updated skill {SkillId} and invalidated caches", id);
            
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await SkillExists(id))
            {
                return NotFound($"Skill with ID {id} not found.");
            }
            
            _logger.LogError(ex, "Concurrency error updating skill {SkillId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill {SkillId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a skill.
    /// </summary>
    /// <param name="id">The skill ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        try
        {
            var skill = await _context.Skills.FindAsync(id);
            
            if (skill == null)
            {
                return NotFound($"Skill with ID {id} not found.");
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            // Invalidate all skill caches
            InvalidateSkillCaches(id);
            _logger.LogInformation("Deleted skill {SkillId} and invalidated caches", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill {SkillId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets or caches the total count of skills.
    /// </summary>
    private async Task<int> GetOrCacheTotalSkillCount()
    {
        return await _cacheHelper.GetOrCacheCountAsync(
            SkillsTotalCountCacheKey,
            async () => await _context.Skills.CountAsync(),
            "Skills total count");
    }

    /// <summary>
    /// Invalidates all skill-related caches including paginated results.
    /// </summary>
    private void InvalidateSkillCaches(int? skillId = null)
    {
        // Remove individual skill cache if ID provided
        if (skillId.HasValue)
        {
            _cacheHelper.RemoveCache($"{SkillCacheKeyPrefix}{skillId.Value}");
        }

        // Remove all general caches and paginated caches
        _cacheHelper.RemoveMultiple(SkillsCacheKey, SkillsTotalCountCacheKey);
        _cacheHelper.InvalidatePagedCaches(SkillsPagedCacheKeysListKey, "Skills");
        
        _logger.LogInformation("Invalidated skill caches (all, total count, and paginated)");
    }

    /// <summary>
    /// Tracks a paginated cache key so it can be invalidated later.
    /// </summary>
    private void TrackPagedCacheKey(string cacheKey)
    {
        _cacheHelper.TrackPagedCacheKey(SkillsPagedCacheKeysListKey, cacheKey);
    }

    private async Task<bool> SkillExists(int id)
    {
        return await _context.Skills.AnyAsync(e => e.Id == id);
    }
}
