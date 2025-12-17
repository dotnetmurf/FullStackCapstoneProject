# SkillSnap Technology Stack Guidelines and Best Practices

## Introduction

This document provides specific guidelines, best practices, and patterns for building the SkillSnap application using its chosen technology stack. These instructions ensure consistency, maintainability, and optimal performance across all three projects in the solution.

---

## Technology Stack Overview

### Solution Architecture
- **Solution**: Three-project architecture with shared class library
- **Target Framework**: .NET 8.0 LTS (Long Term Support)
- **Language**: C# 12.0
- **Database**: SQLite (file-based relational database)
- **Authentication**: ASP.NET Core Identity with JWT tokens

### Projects
1. **SkillSnap.Api** - ASP.NET Core Web API (Backend)
2. **SkillSnap.Client** - Blazor WebAssembly (Frontend)
3. **SkillSnap.Shared** - Class Library (Shared Models & Resources)

---

## .NET 8.0 LTS Best Practices

### General .NET Guidelines

- **Use Modern C# Features**: Leverage C# 12 features like primary constructors, collection expressions, and improved pattern matching
- **Nullable Reference Types**: Enable and properly use nullable reference types throughout the solution
- **Async/Await**: Always use async/await for I/O operations (database, HTTP, file system)
- **String Handling**: Use `string.Empty` instead of `""` and leverage string interpolation with `$""`
- **Disposal**: Implement `IDisposable` or use `using` statements for resource cleanup
- **Configuration**: Use the Options pattern for strongly-typed configuration
- **Dependency Injection**: Use constructor injection exclusively; avoid service locator pattern

### Performance Optimization

```csharp
// Good: Use collection expressions (C# 12)
List<string> skills = ["C#", "Blazor", "ASP.NET Core"];

// Good: Use modern LINQ patterns
var activeProjects = projects.Where(p => p.IsActive).ToList();

// Good: Avoid unnecessary allocations
public string GetFullName() => $"{FirstName} {LastName}";

// Avoid: Creating intermediate collections unnecessarily
// Bad: projects.ToList().Where(p => p.IsActive).ToList();
```

---

## ASP.NET Core Web API Guidelines (SkillSnap.Api)

### Project Structure Best Practices

```
SkillSnap.Api/
├── Controllers/          # API endpoints (thin controllers)
├── Data/                # DbContext and database configuration
├── Services/            # Business logic and domain services
├── Middleware/          # Custom middleware components
├── Extensions/          # Extension methods for service registration
├── Filters/             # Action filters and exception filters
└── Migrations/          # EF Core migrations (auto-generated)
```

### Controller Design Patterns

**Always follow these controller principles:**

1. **Keep Controllers Thin**: Controllers should only handle HTTP concerns
2. **Use Async Actions**: All controller actions should be asynchronous
3. **Return ActionResult<T>**: Use strongly-typed action results
4. **Apply Attributes**: Use `[ApiController]`, `[Route]`, `[Authorize]` appropriately
5. **Validate Input**: Use model validation and return appropriate status codes
6. **Handle Exceptions**: Let global exception handlers catch errors, don't try-catch in controllers

**Controller Template:**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Api.Services;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

/// <summary>
/// Manages project-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all projects.
    /// </summary>
    /// <returns>A list of projects.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(projects);
    }

    /// <summary>
    /// Retrieves a specific project by ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The requested project.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        
        if (project == null)
        {
            return NotFound();
        }
        
        return Ok(project);
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Project>> CreateProject([FromBody] Project project)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdProject = await _projectService.CreateProjectAsync(project);
        
        return CreatedAtAction(
            nameof(GetProject), 
            new { id = createdProject.Id }, 
            createdProject);
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="project">The updated project data.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] Project project)
    {
        if (id != project.Id)
        {
            return BadRequest("ID mismatch");
        }

        var updated = await _projectService.UpdateProjectAsync(project);
        
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var deleted = await _projectService.DeleteProjectAsync(id);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

### Service Layer Pattern

**Create service interfaces and implementations for business logic:**

```csharp
// IProjectService.cs
namespace SkillSnap.Api.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(Project project);
    Task<bool> UpdateProjectAsync(Project project);
    Task<bool> DeleteProjectAsync(int id);
}

// ProjectService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Services;

public class ProjectService : IProjectService
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProjectService> _logger;
    private const string ProjectsCacheKey = "all_projects";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public ProjectService(
        SkillSnapContext context, 
        IMemoryCache cache,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        if (_cache.TryGetValue(ProjectsCacheKey, out List<Project>? cachedProjects))
        {
            _logger.LogInformation("Projects retrieved from cache");
            return cachedProjects!;
        }

        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.PortfolioUser)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        _cache.Set(ProjectsCacheKey, projects, _cacheExpiration);
        _logger.LogInformation("Projects retrieved from database and cached");

        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _context.Projects
            .AsNoTracking()
            .Include(p => p.PortfolioUser)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        _cache.Remove(ProjectsCacheKey);
        _logger.LogInformation("Created project {ProjectId} and invalidated cache", project.Id);

        return project;
    }

    public async Task<bool> UpdateProjectAsync(Project project)
    {
        var existing = await _context.Projects.FindAsync(project.Id);
        
        if (existing == null)
        {
            return false;
        }

        _context.Entry(existing).CurrentValues.SetValues(project);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        _cache.Remove(ProjectsCacheKey);
        _logger.LogInformation("Updated project {ProjectId} and invalidated cache", project.Id);

        return true;
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        
        if (project == null)
        {
            return false;
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        _cache.Remove(ProjectsCacheKey);
        _logger.LogInformation("Deleted project {ProjectId} and invalidated cache", id);

        return true;
    }
}
```

### Program.cs Configuration

**Use extension methods to organize service registration:**

```csharp
// Extensions/ServiceCollectionExtensions.cs
namespace SkillSnap.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ISkillService, SkillService>();
        // Add more services here
        
        return services;
    }

    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<SkillSnapContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=skillsnap.db"));
        
        return services;
    }

    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddResponseCaching();
        
        return services;
    }
}

// Program.cs
using SkillSnap.Api.Data;
using SkillSnap.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services using extension methods
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCachingServices();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["ClientUrl"] ?? "https://localhost:5001",
                "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Entity Framework Core 8.0 Guidelines

### DbContext Best Practices

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Data;

public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public SkillSnapContext(DbContextOptions<SkillSnapContext> options) 
        : base(options)
    {
    }

    // Use DbSet properties with init-only setters (C# 9+)
    public DbSet<PortfolioUser> PortfolioUsers => Set<PortfolioUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships explicitly
        ConfigurePortfolioUserRelationships(modelBuilder);
        ConfigureProjectRelationships(modelBuilder);
        ConfigureSkillRelationships(modelBuilder);
        
        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);
    }

    private static void ConfigurePortfolioUserRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Bio)
                .HasMaxLength(500);
            
            entity.Property(e => e.ProfileImageUrl)
                .HasMaxLength(255);

            entity.HasMany(u => u.Projects)
                .WithOne(p => p.PortfolioUser)
                .HasForeignKey(p => p.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Skills)
                .WithOne(s => s.PortfolioUser)
                .HasForeignKey(s => s.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProjectRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255);

            entity.HasOne(p => p.PortfolioUser)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSkillRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Level)
                .HasMaxLength(20);

            entity.HasOne(s => s.PortfolioUser)
                .WithMany(u => u.Skills)
                .HasForeignKey(s => s.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Add indexes for frequently queried columns
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.PortfolioUserId);
        
        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.PortfolioUserId);
        
        modelBuilder.Entity<PortfolioUser>()
            .HasIndex(u => u.Name);
    }
}
```

### Query Optimization Patterns

```csharp
// Good: Use AsNoTracking for read-only queries
var projects = await _context.Projects
    .AsNoTracking()
    .Where(p => p.PortfolioUserId == userId)
    .ToListAsync();

// Good: Use Include for eager loading to avoid N+1 queries
var user = await _context.PortfolioUsers
    .AsNoTracking()
    .Include(u => u.Projects)
    .Include(u => u.Skills)
    .FirstOrDefaultAsync(u => u.Id == userId);

// Good: Use Select for projection when you don't need all properties
var projectTitles = await _context.Projects
    .AsNoTracking()
    .Where(p => p.PortfolioUserId == userId)
    .Select(p => new { p.Id, p.Title })
    .ToListAsync();

// Avoid: Loading all data then filtering in memory
// Bad: var projects = await _context.Projects.ToListAsync();
//      var filtered = projects.Where(p => p.PortfolioUserId == userId);

// Good: Use AnyAsync instead of Count > 0
var hasProjects = await _context.Projects
    .AnyAsync(p => p.PortfolioUserId == userId);

// Avoid: var hasProjects = (await _context.Projects.CountAsync()) > 0;
```

### Migration Best Practices

```bash
# Always create descriptive migration names
dotnet ef migrations add AddPortfolioUserProjectRelationship

# Review migration before applying
dotnet ef migrations script

# Apply migration
dotnet ef database update

# Remove last migration if needed (before applying)
dotnet ef migrations remove

# List all migrations
dotnet ef migrations list
```

---

## Blazor WebAssembly Guidelines (SkillSnap.Client)

### Component Structure Best Practices

```
SkillSnap.Client/
├── Components/          # Reusable UI components
├── Pages/              # Routable pages
├── Layout/             # Layout components
├── Services/           # HTTP services and state management
├── wwwroot/            # Static assets (CSS, images, JS)
└── Shared/             # Shared UI components
```

### Component Design Patterns

**Follow these Blazor component principles:**

1. **Single Responsibility**: Each component should have one clear purpose
2. **Parameter Validation**: Validate component parameters
3. **Lifecycle Methods**: Use appropriate lifecycle methods (OnInitializedAsync, OnParametersSet)
4. **Error Boundaries**: Handle errors gracefully with try-catch in components
5. **Loading States**: Always show loading indicators for async operations
6. **Accessibility**: Include ARIA labels and semantic HTML

**Component Template:**

```razor
@* ProfileCard.razor *@
@namespace SkillSnap.Client.Components

<div class="profile-card">
    @if (IsLoading)
    {
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    }
    else if (User != null)
    {
        <div class="card">
            @if (!string.IsNullOrEmpty(User.ProfileImageUrl))
            {
                <img src="@User.ProfileImageUrl" 
                     class="card-img-top profile-image" 
                     alt="@User.Name profile picture"
                     loading="lazy" />
            }
            <div class="card-body">
                <h3 class="card-title">@User.Name</h3>
                <p class="card-text">@User.Bio</p>
            </div>
        </div>
    }
    else if (!string.IsNullOrEmpty(ErrorMessage))
    {
        <div class="alert alert-danger" role="alert">
            @ErrorMessage
        </div>
    }
</div>

@code {
    [Parameter]
    public int UserId { get; set; }

    [Parameter]
    public EventCallback OnProfileUpdated { get; set; }

    [Inject]
    private IPortfolioUserService UserService { get; set; } = default!;

    [Inject]
    private ILogger<ProfileCard> Logger { get; set; } = default!;

    private PortfolioUser? User { get; set; }
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadUserDataAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Reload if UserId parameter changes
        if (User?.Id != UserId)
        {
            await LoadUserDataAsync();
        }
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StateHasChanged();

            User = await UserService.GetUserByIdAsync(UserId);

            if (User == null)
            {
                ErrorMessage = "User not found.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading user {UserId}", UserId);
            ErrorMessage = "Failed to load user profile. Please try again.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshAsync()
    {
        await LoadUserDataAsync();
        await OnProfileUpdated.InvokeAsync();
    }
}
```

### HTTP Service Pattern for Blazor

```csharp
// IProjectService.cs (in SkillSnap.Client/Services)
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public interface IProjectService
{
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project?> CreateProjectAsync(Project project);
    Task<bool> UpdateProjectAsync(int id, Project project);
    Task<bool> DeleteProjectAsync(int id);
}

// ProjectService.cs
using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectService> _logger;
    private const string BaseUrl = "api/projects";

    public ProjectService(HttpClient httpClient, ILogger<ProjectService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<Project>> GetAllProjectsAsync()
    {
        try
        {
            var projects = await _httpClient.GetFromJsonAsync<List<Project>>(BaseUrl);
            return projects ?? new List<Project>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching projects");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projects");
            throw;
        }
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Project>($"{BaseUrl}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Project {ProjectId} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project {ProjectId}", id);
            throw;
        }
    }

    public async Task<Project?> CreateProjectAsync(Project project)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, project);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<Project>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating project");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            throw;
        }
    }

    public async Task<bool> UpdateProjectAsync(int id, Project project)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", project);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return false;
        }
    }
}
```

### Blazor Program.cs Configuration

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SkillSnap.Client;
using SkillSnap.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl)
});

// Register application services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UserSessionService>();

// Add local storage for token persistence
builder.Services.AddBlazoredLocalStorage();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree", LogLevel.None);

await builder.Build().RunAsync();
```

### State Management Pattern

```csharp
// UserSessionService.cs - Scoped service for managing user state
using Blazored.LocalStorage;
using System.Security.Claims;

namespace SkillSnap.Client.Services;

public class UserSessionService
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    private const string UserKey = "currentUser";

    public event Action? OnChange;

    public UserSessionService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public string? CurrentUserId { get; private set; }
    public string? CurrentUserName { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public async Task InitializeAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync(TokenKey);
        
        if (!string.IsNullOrEmpty(token))
        {
            // Parse token to get user info
            var claims = ParseClaimsFromJwt(token);
            CurrentUserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            CurrentUserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            IsAuthenticated = true;
        }
        
        NotifyStateChanged();
    }

    public async Task SetAuthenticationAsync(string token, string userId, string userName)
    {
        await _localStorage.SetItemAsStringAsync(TokenKey, token);
        
        CurrentUserId = userId;
        CurrentUserName = userName;
        IsAuthenticated = true;
        
        NotifyStateChanged();
    }

    public async Task ClearAuthenticationAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(UserKey);
        
        CurrentUserId = null;
        CurrentUserName = null;
        IsAuthenticated = false;
        
        NotifyStateChanged();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync(TokenKey);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
```

---

## Shared Library Guidelines (SkillSnap.Shared)

### Model Design Best Practices

**All models in SkillSnap.Shared/Models must follow these patterns:**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

/// <summary>
/// Represents a project in the portfolio.
/// </summary>
public class Project
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the project title.
    /// </summary>
    [Required(ErrorMessage = "Project title is required")]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "Title must be between 3 and 100 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project description.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the project image.
    /// </summary>
    [StringLength(255, ErrorMessage = "Image URL cannot exceed 255 characters")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreign key to the portfolio user.
    /// </summary>
    [Required]
    [ForeignKey(nameof(PortfolioUser))]
    public int PortfolioUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the portfolio user.
    /// </summary>
    public PortfolioUser? PortfolioUser { get; set; }

    /// <summary>
    /// Gets or sets the date when the project was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date when the project was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### Data Transfer Objects (DTOs)

**When you need different representations for API communication:**

```csharp
// SkillSnap.Shared/DTOs/ProjectDto.cs
namespace SkillSnap.Shared.DTOs;

/// <summary>
/// Data transfer object for project creation requests.
/// </summary>
public class CreateProjectDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    public int PortfolioUserId { get; set; }
}

/// <summary>
/// Data transfer object for project update requests.
/// </summary>
public class UpdateProjectDto
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for project responses (read operations).
/// </summary>
public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int PortfolioUserId { get; set; }
    public string PortfolioUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## SQLite Database Guidelines

### Connection String Best Practices

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=skillsnap.db;Cache=Shared"
  }
}

// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=skillsnap.db;Cache=Shared;Foreign Keys=True"
  }
}
```

### SQLite-Specific Considerations

1. **Foreign Key Enforcement**: Enable foreign keys in development
2. **Concurrency**: SQLite uses file-level locking; avoid long-running transactions
3. **Data Types**: SQLite has limited data types (NULL, INTEGER, REAL, TEXT, BLOB)
4. **Indexes**: Create indexes on foreign keys and frequently queried columns
5. **Backup**: Use `.backup` command or copy `.db` file when database is not in use

### Migration Patterns for SQLite

```csharp
// When adding a new column with a default value
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<DateTime>(
        name: "CreatedAt",
        table: "Projects",
        type: "TEXT",
        nullable: false,
        defaultValue: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
}

// When adding an index
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_Projects_PortfolioUserId",
        table: "Projects",
        column: "PortfolioUserId");
}
```

---

## Caching Strategy (Phase 4)

### In-Memory Caching Pattern

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace SkillSnap.Api.Services;

public class CachedProjectService : IProjectService
{
    private readonly IProjectService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedProjectService> _logger;

    public CachedProjectService(
        IProjectService innerService,
        IMemoryCache cache,
        ILogger<CachedProjectService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        const string cacheKey = "all_projects";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Project>? cachedProjects))
        {
            _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return cachedProjects!;
        }

        _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);
        var projects = await _innerService.GetAllProjectsAsync();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
            .SetPriority(CacheItemPriority.Normal)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogInformation(
                    "Cache entry {Key} evicted. Reason: {Reason}", 
                    key, reason);
            });

        _cache.Set(cacheKey, projects, cacheOptions);
        
        return projects;
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        var result = await _innerService.CreateProjectAsync(project);
        
        // Invalidate cache after create
        _cache.Remove("all_projects");
        _logger.LogInformation("Cache invalidated after creating project");
        
        return result;
    }

    // Similar patterns for Update and Delete...
}
```

### Cache Invalidation Strategy

**Invalidate cache on:**
- Create operations
- Update operations
- Delete operations

**Keep cache for:**
- Read operations (GET requests)

---

## Authentication & Authorization Patterns (Phase 3)

### JWT Token Service

```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSnap.Api.Services;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured")));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogInformation("Token generated for user {UserId}", user.Id);

        return tokenString;
    }
}
```

### Authentication Controller Pattern

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Api.Services;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, "User");

        _logger.LogInformation("User {Email} registered successfully", model.Email);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        return Ok(new { Token = token, Email = user.Email });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        
        if (user == null)
        {
            return Unauthorized("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, model.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} is locked out", model.Email);
                return Unauthorized("Account locked out");
            }

            return Unauthorized("Invalid email or password");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        _logger.LogInformation("User {Email} logged in successfully", model.Email);

        return Ok(new { Token = token, Email = user.Email });
    }
}

// DTOs
public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

---

## Error Handling and Logging

### Global Exception Handler

```csharp
// Middleware/GlobalExceptionHandler.cs
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace SkillSnap.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var statusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var response = new
        {
            StatusCode = statusCode,
            Message = exception.Message,
            Details = httpContext.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .IsDevelopment() ? exception.StackTrace : null
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response), 
            cancellationToken);

        return true;
    }
}

// Register in Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

### Structured Logging Pattern

```csharp
// Use structured logging with message templates
_logger.LogInformation("User {UserId} created project {ProjectId}", userId, projectId);

// Log levels appropriately
_logger.LogTrace("Entering method GetProjectByIdAsync");
_logger.LogDebug("Cache miss for key {CacheKey}", cacheKey);
_logger.LogInformation("User {Email} logged in successfully", email);
_logger.LogWarning("Project {ProjectId} not found", projectId);
_logger.LogError(exception, "Error creating project for user {UserId}", userId);
_logger.LogCritical("Database connection failed");
```

---

## Performance Best Practices

### API Performance

1. **Use Response Caching**: Cache GET endpoints that don't change frequently
2. **Enable Response Compression**: Compress API responses (gzip, brotli)
3. **Implement Pagination**: Don't return all records at once
4. **Use Async/Await**: All I/O operations must be asynchronous
5. **Connection Pooling**: Let EF Core manage connection pooling
6. **Minimize Payload**: Return only necessary data using DTOs or projections

### Blazor Performance

1. **Lazy Loading**: Load components and data only when needed
2. **Virtualization**: Use `<Virtualize>` for large lists
3. **Debouncing**: Debounce search inputs to reduce API calls
4. **Progressive Enhancement**: Load critical content first
5. **Image Optimization**: Use `loading="lazy"` for images
6. **Minimize Render Cycles**: Use `ShouldRender()` to control re-renders

---

## Testing Guidelines

### Unit Testing Pattern

```csharp
// SkillSnap.Api.Tests/Services/ProjectServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Api.Services;

public class ProjectServiceTests
{
    private readonly Mock<SkillSnapContext> _mockContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<ProjectService>> _mockLogger;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _mockContext = new Mock<SkillSnapContext>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<ProjectService>>();
        
        _service = new ProjectService(
            _mockContext.Object,
            _mockCache.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllProjectsAsync_ReturnsAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Title = "Project 1" },
            new() { Id = 2, Title = "Project 2" }
        };

        // Mock setup...

        // Act
        var result = await _service.GetAllProjectsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}
```

---

## Security Checklist

- [ ] Enable HTTPS in all environments
- [ ] Configure CORS restrictively
- [ ] Validate all user inputs with data annotations
- [ ] Use parameterized queries (EF Core does this automatically)
- [ ] Implement authentication and authorization
- [ ] Store secrets securely (User Secrets, Azure Key Vault)
- [ ] Enable anti-forgery tokens for state-changing operations
- [ ] Add security headers (CSP, X-Frame-Options, etc.)
- [ ] Log security events (login attempts, authorization failures)
- [ ] Implement rate limiting for API endpoints
- [ ] Use HTTPS-only cookies for authentication tokens
- [ ] Regularly update NuGet packages for security patches

---

## Development Workflow

### Local Development

1. Start API: `dotnet run --project SkillSnap.Api`
2. Start Client: `dotnet run --project SkillSnap.Client`
3. Watch for changes: `dotnet watch run`

### Database Management

```bash
# Create migration
dotnet ef migrations add MigrationName --project SkillSnap.Api

# Apply migration
dotnet ef database update --project SkillSnap.Api

# Generate SQL script
dotnet ef migrations script --project SkillSnap.Api

# Remove last migration
dotnet ef migrations remove --project SkillSnap.Api
```

### Build and Test

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Publish for deployment
dotnet publish -c Release
```

---

## Common Patterns Summary

### Dependency Injection
- Always use constructor injection
- Register services with appropriate lifetimes (Scoped, Transient, Singleton)
- Use interfaces for abstractions

### Async Programming
- All I/O operations must be asynchronous
- Use `async/await` consistently
- Never use `.Result` or `.Wait()` - this can cause deadlocks

### Error Handling
- Use try-catch in services and components
- Log errors with context
- Return appropriate HTTP status codes
- Don't expose internal errors to clients

### Validation
- Use data annotations on models
- Validate in both client and server
- Return validation errors in a consistent format

### Logging
- Use structured logging with message templates
- Log at appropriate levels
- Don't log sensitive information (passwords, tokens)
- Include correlation IDs for distributed tracing

---

**Last Updated**: December 2025  
**Target Framework**: .NET 8.0 LTS  
**Applies To**: SkillSnap Solution (Api, Client, Shared)
