# SkillSnap Project Documentation

## Overview
SkillSnap is a full-stack portfolio and project tracker application designed to help users showcase their skills and projects. It is built with a modern three-tier architecture using .NET 8.0 technologies, including ASP.NET Core Web API, Blazor WebAssembly, and Entity Framework Core with SQLite. The solution is structured for maintainability, scalability, and security, leveraging best practices in authentication, data modeling, and UI development.

---

## Architecture

### High-Level Structure
- **SkillSnap.Api**: ASP.NET Core 8 Web API (backend)
- **SkillSnap.Client**: Blazor WebAssembly 8 SPA (frontend)
- **SkillSnap.Shared**: .NET 8 class library for shared models and DTOs

### Three-Tier Design
- **Presentation Layer**: Blazor WebAssembly SPA, Razor components, client-side state, and authentication
- **Application Layer**: RESTful API controllers, business logic, authentication, caching, and validation
- **Data Layer**: Entity Framework Core with SQLite, code-first migrations, and cascade delete relationships

---

## Key Features
- **PortfolioUser, Project, Skill**: Central entities with strong data annotations and relationships
- **Full CRUD API**: For all entities, with async/await, error handling, and XML documentation
- **Authentication & Authorization**: ASP.NET Identity, JWT tokens, role-based access (User/Admin)
- **Client-Server Communication**: HttpClient with JWT injection, CORS, and error handling
- **Reusable UI Components**: ProfileCard, ProjectList, SkillTags, and more
- **Validation**: DataAnnotations on both client and server, EditForm with validation summary
- **Caching**: IMemoryCache on API, AppStateService planned for client
- **Performance Monitoring**: Middleware for request timing and logging

---

## Data Model
- **PortfolioUser**: Id, Name, Bio, ProfileImageUrl, Projects, Skills
- **Project**: Id, Title, Description, ImageUrl, PortfolioUserId (FK)
- **Skill**: Id, Name, Level, PortfolioUserId (FK)
- **Relationships**: One-to-many (PortfolioUser → Projects/Skills), cascade delete, indexed FKs

---

## API Endpoints
- **/api/portfoliousers**: GET, POST, PUT, DELETE (Admin only for DELETE)
- **/api/projects**: GET, POST, PUT, DELETE (Admin only for DELETE)
- **/api/skills**: GET, POST, PUT, DELETE (Admin only for DELETE)
- **/api/auth**: POST /register, POST /login (JWT issuance)
- **/api/seed**: POST (seed sample data)

---

## Authentication & Security
- **JWT Bearer Tokens**: Issued on login/register, stored in LocalStorage, injected into API requests
- **Role-Based Authorization**: [AllowAnonymous] for GET, [Authorize] for POST/PUT, [Authorize(Roles="Admin")] for DELETE
- **Password Policies**: Enforced via Identity (min 6 chars, digit, upper/lowercase)
- **CORS**: Configured for local development ports, credentials allowed
- **HTTPS**: Supported, with dev cert trust guidance
- **Input Validation**: DataAnnotations, ModelState checks, SQL injection prevention

---

## Developer Workflows
- **Run API**: `cd SkillSnap.Api && dotnet run` (http://localhost:5149)
- **Run Client**: `cd SkillSnap.Client && dotnet run` (http://localhost:5105)
- **Migrations**: `dotnet ef migrations add <Name>` and `dotnet ef database update` (from Api dir)
- **Seeding**: POST to `/api/seed` or use `DbSeeder.cs` logic
- **Default Admin**: `admin@skillsnap.com` / `Admin123!`
- **Testing**: Manual via UI and API endpoints; all builds must pass before commit

---

## UI/UX Patterns
- **Component Loading**: `isLoading` flag, `OnInitializedAsync()` for async data
- **Error Handling**: Bootstrap alerts, try-catch in services, null/empty returns
- **Navigation**: `NavigationManager.NavigateTo()` for page transitions
- **Form Validation**: `<EditForm>`, `<DataAnnotationsValidator>`, `<ValidationSummary>`
- **Role-Based UI**: `<AuthorizeView>` for conditional rendering (e.g., admin actions)
- **Bootstrap 5**: Used for layout and styling, with Bootstrap Icons

---

## Caching & Performance
- **API**: IMemoryCache for GET endpoints, invalidated on POST/PUT/DELETE
- **Client**: AppStateService planned for in-memory caching
- **PerformanceMonitoringMiddleware**: Logs request times, correlation IDs, and slow requests

---

## Database Design
- **SQLite**: File-based, with code-first migrations
- **Cascade Delete**: Deleting a PortfolioUser removes related Projects and Skills
- **Indexes**: On foreign keys for fast queries
- **Identity Tables**: AspNetUsers, AspNetRoles, AspNetUserRoles for authentication

---

## Security Architecture
- **JWT**: HMAC SHA256, 60-minute expiration, validated issuer/audience
- **Role Enforcement**: Only Admins can delete entities
- **CORS**: Only whitelisted origins allowed
- **HTTPS**: Enforced in production, dev cert trust required for local
- **Input Validation**: DataAnnotations, ModelState, parameterized queries

---

## Deployment & Scalability
- **Development**: API and Client run separately, logs in `SkillSnap.Api/Logs/`
- **Production**: Guidance for migration to SQL Server/PostgreSQL, CDN for static files, environment-based config
- **Scalability**: Current limits (SQLite, in-memory cache), future plans for distributed cache and DB

---

## Key Files & Directories
- **SkillSnap.Api/Program.cs**: API startup, DI, CORS, Identity, JWT
- **SkillSnap.Api/Controllers/**: API endpoints (CRUD, Auth, Seed)
- **SkillSnap.Api/Data/SkillSnapContext.cs**: EF Core context, relationships
- **SkillSnap.Api/Data/DbSeeder.cs**: Role and admin seeding
- **SkillSnap.Client/Program.cs**: Client startup, DI, HttpClient config
- **SkillSnap.Client/Services/**: API communication, auth, state
- **SkillSnap.Client/Layout/**: MainLayout, NavMenu, shared UI
- **SkillSnap.Shared/Models/**: Data models (PortfolioUser, Project, Skill)
- **SkillSnap.Shared/DTOs/**: DTOs for API contracts

---

## Commit & Versioning Practices
- **Conventional Commits**: `feat:`, `fix:`, `docs:`, etc.
- **Build Success**: All builds/tests must pass before commit
- **.gitignore**: Covers .NET, SQLite, and build artifacts

---

## Copilot & AI Agent Guidance
- See `.github/copilot-instructions.md` for AI-specific conventions, patterns, and workflows.

---

## References
- **ARCHITECTURE.md**: In-depth architecture, diagrams, and flows
- **BUILD_SUMMARY.md**: Phase-by-phase accomplishments and challenges
- **.github/copilot-instructions.md**: AI agent instructions

---

_Last updated: December 2025_
