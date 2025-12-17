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
/// API controller for managing projects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly CacheHelper _cacheHelper;
    private readonly ILogger<ProjectsController> _logger;

    private const string ProjectsCacheKey = "AllProjects";
    private const string ProjectCacheKeyPrefix = "Project_";
    private const string ProjectsPagedCacheKeyPrefix = "ProjectsPaged_";
    private const string ProjectsTotalCountCacheKey = "ProjectsTotalCount";
    private const string ProjectsPagedCacheKeysListKey = "ProjectsPagedCacheKeys"; // Tracks all paginated cache keys

    public ProjectsController(
        SkillSnapContext context,
        CacheHelper cacheHelper,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _cacheHelper = cacheHelper;
        _logger = logger;
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <returns>A list of all projects.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        try
        {
            // Try to get from cache
            var cachedProjects = _cacheHelper.TryGetFromCache<List<Project>>(ProjectsCacheKey, "Projects");
            if (cachedProjects != null)
            {
                return Ok(cachedProjects);
            }

            // If not in cache, get from database
            _logger.LogInformation("Projects retrieved from database");
            var projects = await _context.Projects
                .AsNoTracking()
                .Include(p => p.PortfolioUser)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // Store in cache
            _cacheHelper.SetCache(ProjectsCacheKey, projects, "Projects");
            
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets projects with pagination support.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <returns>Paginated list of projects.</returns>
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<Project>>> GetProjectsPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var cacheKey = $"{ProjectsPagedCacheKeyPrefix}Page{page}_Size{pageSize}";

            // Try to get from cache
            var cachedResult = _cacheHelper.TryGetFromCache<PagedResult<Project>>(cacheKey, $"Projects page {page}");
            if (cachedResult != null)
            {
                return Ok(cachedResult);
            }

            // Get total count (cached separately)
            var totalCount = await GetOrCacheTotalProjectCount();

            // Get paginated data
            _logger.LogInformation("Projects page {Page} retrieved from database", page);
            var projects = await _context.Projects
                .AsNoTracking()
                .Include(p => p.PortfolioUser)
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<Project>
            {
                Items = projects,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // Cache the result
            _cacheHelper.SetCache(cacheKey, result, $"Projects page {page}");
            
            // Track this cache key for later invalidation
            TrackPagedCacheKey(cacheKey);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated projects");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a specific project by ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The requested project.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        try
        {
            var cacheKey = $"{ProjectCacheKeyPrefix}{id}";

            // Try to get from cache
            var cachedProject = _cacheHelper.TryGetFromCache<Project>(cacheKey, $"Project {id}");
            if (cachedProject != null)
            {
                return Ok(cachedProject);
            }

            // If not in cache, get from database
            _logger.LogInformation("Project {ProjectId} retrieved from database", id);
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.PortfolioUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            // Store in cache
            _cacheHelper.SetCache(cacheKey, project, $"Project {id}");

            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project {ProjectId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a lightweight list of projects optimized for list views (no navigation properties).
    /// </summary>
    /// <returns>A list of project summaries.</returns>
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> GetProjectsSummary()
    {
        try
        {
            const string summaryCacheKey = "AllProjectsSummary";

            // Try to get from cache
            var cachedSummary = _cacheHelper.TryGetFromCache<List<ProjectSummaryDto>>(summaryCacheKey, "Project summaries");
            if (cachedSummary != null)
            {
                return Ok(cachedSummary);
            }

            // If not in cache, get from database with optimized projection
            _logger.LogInformation("Project summaries retrieved from database");
            var summaries = await _context.Projects
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    PortfolioUserId = p.PortfolioUserId,
                    PortfolioUserName = p.PortfolioUser != null ? p.PortfolioUser.Name : string.Empty
                })
                .ToListAsync();

            // Store in cache
            _cacheHelper.SetCache(summaryCacheKey, summaries, "Project summaries");

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving project summaries");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project.</returns>
    [HttpPost]
    public async Task<ActionResult<Project>> PostProject(Project project)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify PortfolioUser exists
            var userExists = await _context.PortfolioUsers.AnyAsync(u => u.Id == project.PortfolioUserId);
            if (!userExists)
            {
                return BadRequest($"PortfolioUser with ID {project.PortfolioUserId} does not exist.");
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Invalidate all project caches
            InvalidateProjectCaches();
            _logger.LogInformation("Created project {ProjectId} and invalidated caches", project.Id);

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="project">The updated project data.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProject(int id, Project project)
    {
        if (id != project.Id)
        {
            return BadRequest("Project ID mismatch.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _context.Entry(project).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Invalidate all project caches

            InvalidateProjectCaches();
            _logger.LogInformation("Updated project {ProjectId} and invalidated caches", id);
            
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await ProjectExists(id))
            {
                return NotFound($"Project with ID {id} not found.");
            }
            
            _logger.LogError(ex, "Concurrency error updating project {ProjectId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            var project = await _context.Projects.FindAsync(id);
            
            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            // Invalidate all project caches

            InvalidateProjectCaches();
            _logger.LogInformation("Deleted project {ProjectId} and invalidated caches", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets or caches the total count of projects.
    /// </summary>
    private async Task<int> GetOrCacheTotalProjectCount()
    {
        return await _cacheHelper.GetOrCacheCountAsync(
            ProjectsTotalCountCacheKey,
            () => _context.Projects.CountAsync(),
            "Projects");
    }

    /// <summary>
    /// Invalidates all project-related caches including paginated results.
    /// Note: IMemoryCache doesn't support pattern-based removal, so we invalidate known cache keys.
    /// For production at scale, consider Redis with pattern matching.
    /// </summary>
    private void InvalidateProjectCaches()
    {
        // Remove primary cache entries
        _cacheHelper.RemoveMultiple(
            ProjectsCacheKey,              // Main projects list cache
            "AllProjectsSummary",          // Summary/projection cache
            ProjectsTotalCountCacheKey);   // Total count cache for pagination
        
        // Clear all tracked paginated cache keys
        _cacheHelper.InvalidatePagedCaches(ProjectsPagedCacheKeysListKey, "project");
        
        _logger.LogInformation("Invalidated project caches (all, summary, total count, and paginated)");
    }

    /// <summary>
    /// Tracks a paginated cache key so it can be invalidated later.
    /// </summary>
    private void TrackPagedCacheKey(string cacheKey)
    {
        _cacheHelper.TrackPagedCacheKey(ProjectsPagedCacheKeysListKey, cacheKey);
    }

    private async Task<bool> ProjectExists(int id)
    {
        return await _context.Projects.AnyAsync(e => e.Id == id);
    }
}
