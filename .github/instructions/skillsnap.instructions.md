# SkillSnap Copilot Instructions

> **Context Reference**: This document provides AI agents with discoverable architecture patterns and critical knowledge not obvious from individual file inspection. For detailed tech stack guidelines, see [tech-stack.instructions.md](tech-stack.instructions.md).

## Project Overview

SkillSnap is a full-stack portfolio tracker with Blazor WebAssembly frontend and ASP.NET Core Web API backend using JWT authentication with ASP.NET Identity.

## Architecture & Structure

### Three-Tier Architecture
- **SkillSnap.Api**: ASP.NET Core 8.0 Web API (Backend)
  - SQLite database with Entity Framework Core
  - JWT authentication with ASP.NET Identity
  - RESTful controllers with role-based authorization
  
- **SkillSnap.Client**: Blazor WebAssembly 8.0 (Frontend)
  - Component-based UI with Razor pages
  - Services for API communication
  - Client-side authentication state management
  
- **SkillSnap.Shared**: .NET 8.0 Class Library
  - Shared data models (PortfolioUser, Project, Skill, ApplicationUser)
  - DTOs for API communication (AuthResponse, LoginRequest, RegisterRequest, etc.)
  - Common interfaces and validation attributes

### Core Entities
- **ApplicationUser**: Identity user with roles (Admin, User)
- **PortfolioUser**: Portfolio profile (Name, Bio, ProfileImageUrl)
- **Project**: Portfolio project (Title, Description, ImageUrl, ProjectUrl, PortfolioUserId)
- **Skill**: Skill with proficiency (Name, Category, ProficiencyLevel, PortfolioUserId)

## Authentication & Authorization

### JWT Authentication Flow
1. User logs in via [AuthController.cs](../SkillSnap.Api/Controllers/AuthController.cs) (Login/Register endpoints)
2. API returns JWT token with expiration in `AuthResponse` DTO
3. Client stores token in browser LocalStorage via `Blazored.LocalStorage`
4. `HttpInterceptorService` injects token into Authorization header for all API requests
5. `CustomAuthStateProvider` maintains authentication state and parses JWT claims

### Authorization Patterns (API Controllers)
- **Public Access**: `[AllowAnonymous]` for GET endpoints
- **Authenticated Users**: `[Authorize]` for POST/PUT endpoints
- **Admin Only**: `[Authorize(Roles = "Admin")]` for DELETE endpoints

### Key Authentication Services (Client)
- **AuthService**: Login, Register, Logout, token management
- **CustomAuthStateProvider**: Extends `AuthenticationStateProvider`, parses JWT claims
- **HttpInterceptorService**: Attaches Bearer token to HttpClient requests

## Service Registration & Dependency Injection

### API (Program.cs)
```csharp
// DbContext with SQLite
builder.Services.AddDbContext<SkillSnapContext>(options =>
    options.UseSqlite("Data Source=skillsnap.db"));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<SkillSnapContext>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* token validation config */);

// CORS for Blazor Client
builder.Services.AddCors(options => 
    options.AddPolicy("AllowBlazorClient", /* origins */));
```

### Client (Program.cs)
```csharp
// HttpClient with API base address
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri("http://localhost:5149/") 
});

// Authentication
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthStateProvider>());

// HTTP & Entity Services
builder.Services.AddScoped<HttpInterceptorService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<PortfolioUserService>();
```

## Blazor Component Patterns

### Page Components (Routes)
- Use `@page "/route"` directive for routing
- Route parameters: `@page "/portfolio/{id:int}"`
- Inject services: `@inject NavigationManager NavManager`
- Lifecycle: `OnInitializedAsync()` for data loading

### Component Lifecycle
```csharp
@inject PortfolioUserService PortfolioUserService

@code {
    [Parameter] public int Id { get; set; }
    private PortfolioUser? user;

    protected override async Task OnInitializedAsync()
    {
        // Call HttpInterceptorService to attach token
        await HttpInterceptor.EnsureAuthHeaderAsync();
        
        // Fetch data from API
        user = await PortfolioUserService.GetPortfolioUserAsync(Id);
        
        // Trigger UI refresh if needed
        StateHasChanged();
    }
}
```

### Authorization in Components
- Use `<AuthorizeView>` for conditional rendering based on authentication
- Wrap admin actions in `<AuthorizeView Roles="Admin">`
- Redirect unauthenticated users with [RedirectToLogin.razor](../SkillSnap.Client/Shared/RedirectToLogin.razor)

### Form Patterns
- Use `<EditForm Model="@model" OnValidSubmit="HandleSubmit">`
- Add `<DataAnnotationsValidator />` for validation
- Use `<ValidationSummary />` to display errors
- Display loading state during submission: `<button disabled="@isLoading">`

## Navigation & Routing

### Navigation Manager Usage
```csharp
@inject NavigationManager NavManager

// Navigate to different page
NavManager.NavigateTo("/portfolio-users");

// Navigate with parameter
NavManager.NavigateTo($"/portfolio/{userId}");

// Navigate to delete confirmation
NavManager.NavigateTo($"/delete-project/{projectId}");
```

### Route Patterns
- List pages: `/portfolio-users`, `/projects`, `/skills`
- Detail view: `/portfolio/{id:int}`
- Add forms: `/add-portfolio-user`, `/add-project`, `/add-skill`
- Edit forms: `/edit-portfolio-user/{id:int}`, `/edit-project/{id:int}`
- Delete confirmations: `/delete-portfolio-user/{id:int}`, `/delete-project/{id:int}`

## Data Access Patterns

### Entity Service Pattern (Client)
Each entity has a dedicated service (e.g., `PortfolioUserService`, `ProjectService`, `SkillService`):
```csharp
public class PortfolioUserService
{
    private readonly HttpClient _http;
    private readonly HttpInterceptorService _httpInterceptor;

    // GET all
    public async Task<List<PortfolioUser>> GetPortfolioUsersAsync()
    {
        await _httpInterceptor.EnsureAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<PortfolioUser>>("api/portfoliousers") 
            ?? new List<PortfolioUser>();
    }

    // GET by ID
    public async Task<PortfolioUser?> GetPortfolioUserAsync(int id)
    {
        await _httpInterceptor.EnsureAuthHeaderAsync();
        return await _http.GetFromJsonAsync<PortfolioUser>($"api/portfoliousers/{id}");
    }

    // POST (Create)
    public async Task<PortfolioUser?> CreatePortfolioUserAsync(PortfolioUser user)
    {
        await _httpInterceptor.EnsureAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/portfoliousers", user);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PortfolioUser>();
    }

    // PUT (Update)
    public async Task UpdatePortfolioUserAsync(int id, PortfolioUser user)
    {
        await _httpInterceptor.EnsureAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/portfoliousers/{id}", user);
        response.EnsureSuccessStatusCode();
    }

    // DELETE
    public async Task DeletePortfolioUserAsync(int id)
    {
        await _httpInterceptor.EnsureAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/portfoliousers/{id}");
        response.EnsureSuccessStatusCode();
    }
}
```

**Critical**: Always call `await _httpInterceptor.EnsureAuthHeaderAsync()` before API requests to attach JWT token.

### API Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext _context;

    // Public read access
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        return await _context.Projects
            .Include(p => p.PortfolioUser)
            .ToListAsync();
    }

    // Authenticated write access
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Project>> PostProject(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    // Admin-only delete access
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();
        
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
```

## Development Workflow

### Running the Application
1. **Start API** (Terminal 1):
   ```powershell
   cd SkillSnap.Api
   dotnet run
   # Runs on http://localhost:5149 and https://localhost:7149
   ```

2. **Start Client** (Terminal 2):
   ```powershell
   cd SkillSnap.Client
   dotnet run
   # Runs on http://localhost:5105 and https://localhost:7105
   ```

3. **Access Application**: Navigate to http://localhost:5105

### Default Admin Account
- **Username**: admin@skillsnap.com
- **Password**: Admin123!
- Seeded automatically via [DbSeeder.cs](../SkillSnap.Api/Data/DbSeeder.cs)

### Database Migrations
```powershell
# Create migration (from SkillSnap.Api directory)
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## UI/UX Conventions

### Layout Structure
- **[MainLayout.razor](../SkillSnap.Client/Layout/MainLayout.razor)**: Main layout with top authentication bar and sidebar
- **[NavMenu.razor](../SkillSnap.Client/Layout/NavMenu.razor)**: Sidebar navigation with links to Home and All Users
- Top bar: Login/Logout buttons with `<AuthorizeView>` conditional rendering

### Bootstrap Integration
- **Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons 1.11.3 (via CDN in [index.html](../SkillSnap.Client/wwwroot/index.html))
- **Icon usage**: `<span class="bi bi-house"></span>`
- **Custom icons**: Define in component CSS with SVG data URIs (see [NavMenu.razor.css](../SkillSnap.Client/Layout/NavMenu.razor.css))

### Card Layouts
- Projects and Skills displayed as responsive card grids
- Cards include action buttons (View, Edit, Delete)
- Delete buttons navigate to confirmation pages
- Consistent card styling with Bootstrap classes: `card`, `card-body`, `card-title`

### Component Best Practices
- **ProfileList**: Displays grid of portfolio user cards
- **ProjectList**: Shows projects for a portfolio user with Edit/Delete buttons
- **SkillTags**: Displays skills as cards (not badges) with Edit/Delete actions
- **ProfileCard**: Individual portfolio user card with View/Edit/Delete buttons

## Common Patterns & Gotchas

### Error Handling
- Display error messages with `<div class="alert alert-danger">@errorMessage</div>`
- Use try-catch in service methods, return empty lists on failure
- Log errors to console: `Console.WriteLine($"Error: {ex.Message}")`

### Loading States
```csharp
private bool isLoading = true;

protected override async Task OnInitializedAsync()
{
    isLoading = true;
    try { /* fetch data */ }
    finally { isLoading = false; }
}

// In markup
@if (isLoading)
{
    <p>Loading...</p>
}
else
{
    <!-- Display data -->
}
```

### JSON Serialization (API)
Configure in [Program.cs](../SkillSnap.Api/Program.cs):
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

### CORS Configuration
API allows client origins in [Program.cs](../SkillSnap.Api/Program.cs):
```csharp
policy.WithOrigins(
    "http://localhost:5105",   // Blazor Client HTTP
    "https://localhost:5105")  // Blazor Client HTTPS
```

## Future Enhancements (Phase 4+)

- **Caching**: IMemoryCache for API responses
- **AppStateService**: Client-side state management for frequently accessed data
- **Search & Filtering**: Enhanced query capabilities
- **Pagination**: API and UI support for large datasets
- **Image Upload**: File upload for profile and project images

## Quick Reference

| Concept | Location | Key Details |
|---------|----------|-------------|
| API Startup | [SkillSnap.Api/Program.cs](../SkillSnap.Api/Program.cs) | DbContext, Identity, JWT, CORS |
| Client Startup | [SkillSnap.Client/Program.cs](../SkillSnap.Client/Program.cs) | HttpClient, Auth services, Entity services |
| Authentication | [Controllers/AuthController.cs](../SkillSnap.Api/Controllers/AuthController.cs) | Login, Register, JWT generation |
| Auth State | [Services/CustomAuthStateProvider.cs](../SkillSnap.Client/Services/CustomAuthStateProvider.cs) | JWT parsing, claims management |
| Token Injection | [Services/HttpInterceptorService.cs](../SkillSnap.Client/Services/HttpInterceptorService.cs) | Attach Bearer token to requests |
| Main Layout | [Layout/MainLayout.razor](../SkillSnap.Client/Layout/MainLayout.razor) | Top bar with Login/Logout |
| Navigation | [Layout/NavMenu.razor](../SkillSnap.Client/Layout/NavMenu.razor) | Sidebar with links |
| Database Context | [Data/SkillSnapContext.cs](../SkillSnap.Api/Data/SkillSnapContext.cs) | DbSets, relationships |
| Seed Data | [Data/DbSeeder.cs](../SkillSnap.Api/Data/DbSeeder.cs) | Admin user, roles |

## Development Checklist

When adding new features:
- [ ] Create model in `SkillSnap.Shared/Models/`
- [ ] Add DbSet to `SkillSnapContext`
- [ ] Create migration: `dotnet ef migrations add <Name>`
- [ ] Create API controller with `[Authorize]` attributes
- [ ] Create client service with `HttpInterceptorService` usage
- [ ] Register service in `SkillSnap.Client/Program.cs`
- [ ] Create Razor pages/components with `@page` directive
- [ ] Add navigation links to `NavMenu.razor`
- [ ] Test authentication/authorization for all endpoints
- [ ] Verify CORS configuration if adding new origins

---

**Last Updated**: 2024-12
