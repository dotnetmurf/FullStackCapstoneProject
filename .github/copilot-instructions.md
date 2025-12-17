# SkillSnap Copilot Instructions

## Project Overview
SkillSnap is a three-tier portfolio tracker with:
- **SkillSnap.Api**: ASP.NET Core 8 Web API (SQLite, EF Core, Identity, JWT)
- **SkillSnap.Client**: Blazor WebAssembly 8 (Razor, HttpClient, JWT auth)
- **SkillSnap.Shared**: .NET 8 class library (models, DTOs)

## Architecture & Patterns
- **API**: RESTful controllers, role-based auth, async/await, error handling, XML docs
- **Client**: Razor components, service-based API access, local storage for JWT, `<AuthorizeView>` for UI auth
- **Shared**: All models/DTOs in `SkillSnap.Shared/Models` and `SkillSnap.Shared/DTOs`
- **DbContext**: `SkillSnapContext` (IdentityDbContext), cascade delete, foreign key indexes
- **Caching**: IMemoryCache in API, AppStateService planned for client

## Authentication & Authorization
- JWT tokens issued by `/api/auth/login` and `/api/auth/register` (see `AuthController.cs`)
- Client stores token in LocalStorage (`authToken`), injects via `HttpInterceptorService`
- API endpoints: `[AllowAnonymous]` for GET, `[Authorize]` for POST/PUT, `[Authorize(Roles = "Admin")]` for DELETE
- Use `<AuthorizeView>` in Blazor for conditional UI

## Developer Workflows
- **Run API**: `cd SkillSnap.Api && dotnet run` (http://localhost:5149)
- **Run Client**: `cd SkillSnap.Client && dotnet run` (http://localhost:5105)
- **Migrations**: `dotnet ef migrations add <Name>` and `dotnet ef database update` (from Api dir)
- **Seed Data**: POST to `/api/seed` or use `DbSeeder.cs` logic
- **Default Admin**: `admin@skillsnap.com` / `Admin123!` (see `DbSeeder.cs`)

## Key Conventions & Patterns
- **Service Pattern**: Each entity has a service (e.g., `ProjectService`) in both API and Client
- **HttpClient Usage**: Always call `EnsureAuthHeaderAsync()` before API requests
- **Component Loading**: Use `isLoading` flag and `OnInitializedAsync()` for async data
- **Error Handling**: Return empty lists/null on error, log to console, show Bootstrap alerts
- **Form Validation**: Use `<EditForm>`, `<DataAnnotationsValidator>`, and `<ValidationSummary>`
- **Navigation**: Use `NavigationManager.NavigateTo()` for page transitions
- **CORS**: API allows client origins (see `Program.cs`)
- **JSON Serialization**: `ReferenceHandler.IgnoreCycles` and `WhenWritingNull` in API

## File References
- API startup/config: `SkillSnap.Api/Program.cs`
- Auth logic: `SkillSnap.Api/Controllers/AuthController.cs`, `SkillSnap.Client/Services/AuthService.cs`
- Db seeding: `SkillSnap.Api/Data/DbSeeder.cs`
- Main layout: `SkillSnap.Client/Layout/MainLayout.razor`
- Navigation: `SkillSnap.Client/Layout/NavMenu.razor`
- Shared models: `SkillSnap.Shared/Models/`
- Client services: `SkillSnap.Client/Services/`

## Examples
- **API Controller**: See `ProjectsController.cs` for async CRUD, `[Authorize]`, and error handling
- **Blazor Service**: See `ProjectService.cs` for HttpClient usage and error patterns
- **Component Auth**: `<AuthorizeView Roles="Admin">` for admin-only UI

---
**Update this file if you add new architectural patterns, workflows, or conventions.**