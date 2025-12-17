# SkillSnap Architecture Documentation

## Table of Contents
- [System Overview](#system-overview)
- [Architectural Patterns](#architectural-patterns)
- [Component Diagrams](#component-diagrams)
- [Data Flow](#data-flow)
- [Authentication Architecture](#authentication-architecture)
- [Caching Strategy](#caching-strategy)
- [Performance Architecture](#performance-architecture)
- [Database Design](#database-design)
- [Security Architecture](#security-architecture)
- [Deployment Architecture](#deployment-architecture)

---

## System Overview

SkillSnap is a three-tier web application implementing a clean separation of concerns with distinct presentation, business logic, and data access layers.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │        Blazor WebAssembly Application (SPA)              │  │
│  │  • Razor Components (.razor)                             │  │
│  │  • Client Services (HTTP communication)                  │  │
│  │  • State Management (AppStateService)                    │  │
│  │  • LocalStorage (JWT token persistence)                  │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          │ HTTPS/JSON (JWT Bearer Token)
                          │
┌─────────────────────────┴───────────────────────────────────────┐
│                    Web Server (Backend)                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         ASP.NET Core Web API (REST)                      │  │
│  │  • Controllers (HTTP endpoints)                          │  │
│  │  • Middleware (Performance, Auth, CORS)                  │  │
│  │  • IMemoryCache (Response caching)                       │  │
│  │  • Identity (User management)                            │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          │ Entity Framework Core (ORM)
                          │
┌─────────────────────────┴───────────────────────────────────────┐
│                     Database Layer                               │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │               SQLite Database (File-based)               │  │
│  │  • PortfolioUsers, Projects, Skills                      │  │
│  │  • AspNetUsers, AspNetRoles (Identity)                   │  │
│  │  • Foreign Key Relationships with Indexes                │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Technology Stack Layers

| Layer | Technology | Purpose |
|-------|-----------|----------|
| **Presentation** | Blazor WebAssembly | Interactive SPA UI with C# |
| **Application** | ASP.NET Core Web API | RESTful API services |
| **Business Logic** | Services + Controllers | Domain logic and validation |
| **Data Access** | Entity Framework Core | ORM and database abstraction |
| **Data Storage** | SQLite | Relational database |
| **Cross-Cutting** | ASP.NET Identity, JWT | Authentication & Authorization |

---

## Architectural Patterns

### 1. Three-Tier Architecture

**Presentation Tier (SkillSnap.Client)**
- Blazor WebAssembly components
- Responsible for UI rendering and user interaction
- Communicates with API via HTTP

**Application Tier (SkillSnap.Api)**
- RESTful API controllers
- Business logic and validation
- Authentication and authorization
- Caching and performance optimization

**Data Tier (SQLite + EF Core)**
- Database schema and relationships
- Data persistence and retrieval
- Transaction management

### 2. Repository Pattern (via EF Core DbContext)

Entity Framework Core's `DbContext` acts as a Unit of Work and Repository:

```csharp
public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<PortfolioUser> PortfolioUsers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Skill> Skills { get; set; }
}
```

**Benefits:**
- Abstraction over database operations
- Change tracking for updates
- Transaction support
- Migration-based schema evolution

### 3. Dependency Injection

Both API and Client use constructor-based dependency injection:

**API Services (Program.cs):**
```csharp
builder.Services.AddDbContext<SkillSnapContext>();
builder.Services.AddMemoryCache();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>();
```

**Client Services (Program.cs):**
```csharp
builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<AppStateService>();
```

### 4. Service Layer Pattern

Client services encapsulate HTTP communication logic:

```csharp
public class ProjectService
{
    private readonly HttpClient _http;
    private readonly AppStateService _appState;
    
    public async Task<List<Project>> GetProjectsAsync()
    {
        // Check cache first
        var cached = _appState.GetCachedProjects();
        if (cached != null) return cached;
        
        // Fetch from API
        var projects = await _http.GetFromJsonAsync<List<Project>>("api/projects");
        
        // Cache result
        _appState.SetCachedProjects(projects);
        return projects;
    }
}
```

### 5. DTO Pattern (Data Transfer Objects)

Separates API contracts from domain models:

```csharp
// Domain Model (database entity)
public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }
    public PortfolioUser PortfolioUser { get; set; }  // Navigation
}

// DTO (API response)
public class ProjectSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string PortfolioUserName { get; set; }  // Flattened
}
```

**Benefits:**
- Prevents over-fetching
- Reduces payload size
- API versioning flexibility

---

## Component Diagrams

### Frontend Component Hierarchy

```
App.razor (Root)
│
├── MainLayout.razor
│   ├── NavMenu.razor (Sidebar Navigation)
│   └── [Body] (Page Content)
│       │
│       ├── Home.razor (Landing Page)
│       │
│       ├── Login.razor (Authentication)
│       │
│       ├── PortfolioUserList.razor (Portfolio Listing)
│       │   ├── Pagination.razor (Page Controls)
│       │   └── ProfileCard.razor (Individual Cards)
│       │
│       ├── ViewPortfolioUser.razor (Portfolio Detail)
│       │   ├── ProjectList.razor (Projects Display)
│       │   └── SkillTags.razor (Skills Display)
│       │
│       └── [CRUD Pages]
│           ├── AddPortfolioUser.razor
│           ├── EditPortfolioUser.razor
│           ├── DeletePortfolioUser.razor
│           └── ... (similar for Projects, Skills)
│
└── Services (Injected Dependencies)
    ├── AuthService
    ├── ProjectService
    ├── SkillService
    ├── PortfolioUserService
    ├── AppStateService
    ├── HttpInterceptorService
    └── CustomAuthStateProvider
```

### Backend Controller Structure

```
API Endpoints
│
├── /api/auth
│   ├── POST /register (User registration)
│   └── POST /login (User authentication)
│
├── /api/portfoliousers
│   ├── GET / (List all - cached)
│   ├── GET /{id} (Get one - cached)
│   ├── POST / (Create - [Authorize])
│   ├── PUT /{id} (Update - [Authorize])
│   └── DELETE /{id} (Delete - [Authorize(Roles="Admin")])
│
├── /api/projects
│   ├── GET / (List all - cached)
│   ├── GET /paged?page={page}&pageSize={pageSize} (Paginated - cached)
│   ├── GET /{id} (Get one - cached)
│   ├── GET /summary (Lightweight list - cached)
│   ├── POST / (Create - [Authorize])
│   ├── PUT /{id} (Update - [Authorize])
│   └── DELETE /{id} (Delete - [Authorize(Roles="Admin")])
│
└── /api/skills
    ├── GET / (List all - cached)
    ├── GET /{id} (Get one - cached)
    ├── POST / (Create - [Authorize])
    ├── PUT /{id} (Update - [Authorize])
    └── DELETE /{id} (Delete - [Authorize(Roles="Admin")])
```

---

## Data Flow

### Request/Response Flow (Authenticated Request)

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ 1. User Action (e.g., "Add Project")
       ▼
┌─────────────────────────────────────┐
│   Blazor Component (AddProject.razor)   │
│   - Validates form input            │
│   - Calls ProjectService.AddProjectAsync()│
└──────┬──────────────────────────────┘
       │ 2. Service Call
       ▼
┌─────────────────────────────────────┐
│   ProjectService (Client)           │
│   - Calls HttpInterceptorService    │
│   - Adds JWT token to header        │
└──────┬──────────────────────────────┘
       │ 3. HTTP POST with Bearer token
       ▼
┌─────────────────────────────────────┐
│   API Middleware Pipeline           │
│   1. PerformanceMonitoringMiddleware│
│   2. CORS Middleware                │
│   3. Authentication Middleware      │
│   4. Authorization Middleware       │
└──────┬──────────────────────────────┘
       │ 4. Authorized request
       ▼
┌─────────────────────────────────────┐
│   ProjectsController.PostProject()  │
│   - Validates model                 │
│   - Verifies PortfolioUser exists   │
│   - Adds to DbContext               │
│   - Saves changes                   │
│   - Invalidates caches              │
└──────┬──────────────────────────────┘
       │ 5. Database operation
       ▼
┌─────────────────────────────────────┐
│   Entity Framework Core             │
│   - Generates SQL INSERT            │
│   - Executes against SQLite         │
│   - Returns new entity with ID      │
└──────┬──────────────────────────────┘
       │ 6. HTTP 201 Created response
       ▼
┌─────────────────────────────────────┐
│   ProjectService (Client)           │
│   - Receives new project            │
│   - Calls AppStateService.NotifyProjectsChanged()│
│   - Invalidates client cache        │
└──────┬──────────────────────────────┘
       │ 7. Navigation/UI update
       ▼
┌─────────────────────────────────────┐
│   Blazor Component                  │
│   - Navigates to project list       │
│   - Triggers StateHasChanged()      │
│   - UI reflects new project         │
└─────────────────────────────────────┘
```

### Cache Hit Flow

```
Client Request
     │
     ▼
AppStateService.GetCachedProjects()
     │
     ├─► Cache Hit (data exists, not expired)
     │        │
     │        └─► Return cached data (0-5ms)
     │
     └─► Cache Miss (data missing or expired)
              │
              ▼
         HTTP GET /api/projects
              │
              ▼
         IMemoryCache.TryGetValue("AllProjects")
              │
              ├─► Cache Hit
              │        │
              │        └─► Return cached data (5-20ms)
              │
              └─► Cache Miss
                       │
                       ▼
                  Database Query (150-200ms)
                       │
                       ▼
                  Cache result (5 min)
                       │
                       ▼
                  Return data to client
```

---

## Authentication Architecture

### JWT Token Flow

```
┌──────────────────────────────────────────────────────────┐
│  Step 1: User Registration/Login                         │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  AuthController.Register/Login                           │
│  1. Validate credentials with UserManager                │
│  2. Generate JWT token with claims:                      │
│     - Sub (user ID)                                      │
│     - Email                                              │
│     - Roles (Admin, User)                                │
│     - Expiration (60 minutes)                            │
│  3. Sign token with HMAC SHA256                          │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Step 2: Client Stores Token                             │
│  AuthService.LoginAsync()                                │
│  - Store token in LocalStorage (key: "authToken")        │
│  - Store expiration timestamp                            │
│  - Notify CustomAuthStateProvider                        │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Step 3: Token Parsing and Claims Extraction             │
│  CustomAuthStateProvider.ParseClaimsFromJwt()            │
│  1. Split JWT: header.payload.signature                  │
│  2. Base64 decode payload                                │
│  3. Deserialize JSON to claims                           │
│  4. Create ClaimsIdentity with "jwt" auth type           │
│  5. Create ClaimsPrincipal for authorization checks      │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Step 4: Authenticated Requests                          │
│  HttpInterceptorService.EnsureAuthHeaderAsync()          │
│  - Retrieve token from LocalStorage                      │
│  - Add to HttpClient.DefaultRequestHeaders:              │
│    Authorization: Bearer {token}                         │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Step 5: API Token Validation                            │
│  JwtBearerMiddleware                                     │
│  1. Extract token from Authorization header              │
│  2. Validate signature with symmetric key                │
│  3. Check issuer, audience, expiration                   │
│  4. Populate HttpContext.User with claims                │
└──────────────────┬───────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Step 6: Authorization                                   │
│  [Authorize] or [Authorize(Roles="Admin")]               │
│  - Check if user is authenticated                        │
│  - Verify role claims if specified                       │
│  - Allow/Deny request                                    │
└──────────────────────────────────────────────────────────┘
```

### Identity Schema

```
AspNetUsers (ApplicationUser extends IdentityUser)
├── Id (string, PK)
├── UserName (string, unique)
├── Email (string, unique)
├── PasswordHash (string, hashed with PBKDF2)
└── [Other Identity fields]

AspNetRoles
├── Id (string, PK)
├── Name (string, unique) - "Admin", "User"
└── NormalizedName (string)

AspNetUserRoles (Many-to-Many)
├── UserId (string, FK -> AspNetUsers.Id)
└── RoleId (string, FK -> AspNetRoles.Id)
```

---

## Caching Strategy

### Two-Level Cache Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Level 1: Client-Side Cache                 │
│                  (AppStateService)                      │
│  • In-memory storage in browser                         │
│  • 5-minute expiration                                  │
│  • Event-driven invalidation                            │
│  • Survives within single session                       │
└───────────────────┬─────────────────────────────────────┘
                    │
                    │ Cache miss or expired
                    │
┌───────────────────┴─────────────────────────────────────┐
│              Level 2: Server-Side Cache                 │
│                  (IMemoryCache)                         │
│  • In-memory storage on server                          │
│  • 5-10 minute expiration (sliding + absolute)          │
│  • Invalidated on POST/PUT/DELETE                       │
│  • Shared across all clients                            │
└───────────────────┬─────────────────────────────────────┘
                    │
                    │ Cache miss
                    │
┌───────────────────┴─────────────────────────────────────┐
│                   Database (SQLite)                     │
│  • Single source of truth                               │
│  • Indexed foreign keys                                 │
│  • AsNoTracking() for read-only queries                │
└─────────────────────────────────────────────────────────┘
```

### Cache Invalidation Strategy

**Client-Side (AppStateService):**
- Invalidate on data mutation (POST, PUT, DELETE)
- Event notification to update UI components
- Time-based expiration (5 minutes)

**Server-Side (IMemoryCache):**
- Invalidate all related caches on mutation
- Pattern: Remove main cache, summary cache, paginated caches
- Track paginated keys with HashSet for bulk removal

**Example - Project Cache Invalidation:**
```csharp
private void InvalidateProjectCaches()
{
    _cache.Remove("AllProjects");              // Main list
    _cache.Remove("AllProjectsSummary");       // DTO projection
    _cache.Remove("ProjectsTotalCount");       // Pagination count
    
    // Invalidate all paginated pages
    if (_cache.TryGetValue("ProjectsPagedCacheKeys", out HashSet<string> keys))
    {
        foreach (var key in keys)
            _cache.Remove(key);  // "ProjectsPaged_Page1_Size20"
    }
}
```

---

## Performance Architecture

### Request Performance Pipeline

```
HTTP Request
     │
     ▼
┌─────────────────────────────────────────┐
│  PerformanceMonitoringMiddleware        │
│  • Start Stopwatch                      │
│  • Log request details (method, path)   │
│  • Track correlation ID                 │
└──────┬──────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  Controller Action                      │
│  • Check IMemoryCache first             │
│  • If cache hit: Return (5-20ms)        │
│  • If cache miss: Query database        │
└──────┬──────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  Entity Framework Core                  │
│  • AsNoTracking() for reads             │
│  • Include() for eager loading          │
│  • Indexed foreign key queries          │
│  • Connection pooling                   │
└──────┬──────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  PerformanceMonitoringMiddleware        │
│  • Stop Stopwatch                       │
│  • Calculate elapsed time               │
│  • Log if > threshold (1000ms prod)     │
│  • Write to file log with correlation   │
└─────────────────────────────────────────┘
```

### Database Query Optimization

**1. AsNoTracking() for Read Operations**
```csharp
var projects = await _context.Projects
    .AsNoTracking()  // No change tracking overhead
    .Include(p => p.PortfolioUser)
    .ToListAsync();
```

**2. Foreign Key Indexing**
```csharp
modelBuilder.Entity<Project>()
    .HasIndex(p => p.PortfolioUserId)
    .HasDatabaseName("IX_Projects_PortfolioUserId");
```

**3. Projection for Summary Views**
```csharp
var summaries = await _context.Projects
    .AsNoTracking()
    .Select(p => new ProjectSummaryDto {  // Only select needed fields
        Id = p.Id,
        Title = p.Title,
        PortfolioUserName = p.PortfolioUser.Name
    })
    .ToListAsync();
```

---

## Database Design

### Entity Relationship Diagram

```
┌──────────────────────────┐
│   PortfolioUser          │
├──────────────────────────┤
│ Id (PK, int)             │
│ Name (string, 100)       │
│ Bio (string, 500)        │
│ ProfileImageUrl (string) │
└───────┬──────────────────┘
        │ 1
        │
        │ *
┌───────┴──────────────────┐       ┌──────────────────────────┐
│   Project                │       │   Skill                  │
├──────────────────────────┤       ├──────────────────────────┤
│ Id (PK, int)             │       │ Id (PK, int)             │
│ Title (string, 100)      │       │ Name (string, 50)        │
│ Description (string)     │       │ Category (string)        │
│ ImageUrl (string)        │       │ ProficiencyLevel (string)│
│ PortfolioUserId (FK, int)│       │ PortfolioUserId (FK, int)│
└──────────────────────────┘       └──────────────────────────┘
         │                                  │
         └──────────────┬───────────────────┘
                        │
                   [CASCADE DELETE]
```

### Cascade Delete Behavior

When a `PortfolioUser` is deleted:
- All associated `Projects` are automatically deleted
- All associated `Skills` are automatically deleted
- Configured via `OnDelete(DeleteBehavior.Cascade)`

### Index Strategy

| Table | Index | Purpose | Performance Gain |
|-------|-------|---------|------------------|
| Projects | IX_Projects_PortfolioUserId | Filter by user | 80-98% faster |
| Skills | IX_Skills_PortfolioUserId | Filter by user | 80-98% faster |

---

## Security Architecture

### Security Layers

**1. Authentication (JWT Bearer Tokens)**
- HMAC SHA256 signature validation
- Token expiration enforcement (60 minutes)
- Secure token storage (HTTPS only)

**2. Authorization Levels**
- Public: GET endpoints (read-only)
- User: POST/PUT (create/update own resources)
- Admin: DELETE (remove any resource)

**3. Input Validation**
- Data annotations on models
- ModelState validation in controllers
- SQL injection prevention (EF Core parameterized queries)

**4. CORS Configuration**
- Whitelist specific origins
- Allow credentials for authentication
- Restrict allowed methods and headers

**5. HTTPS Enforcement**
- Redirect HTTP to HTTPS
- HSTS headers in production
- Secure cookie flags

### Security Best Practices Implemented

✅ Passwords hashed with PBKDF2 (Identity default)  
✅ JWT secrets stored in configuration (not hardcoded)  
✅ CORS restricted to known origins  
✅ SQL injection prevented (parameterized queries)  
✅ XSS prevented (Blazor auto-escaping)  
✅ CSRF tokens for state-changing operations  
✅ Role-based authorization on controllers  
✅ Token expiration and validation  

---

## Deployment Architecture

### Development Environment

```
Developer Machine
├── SkillSnap.Api (http://localhost:5149)
│   └── skillsnap.db (SQLite file in project root)
├── SkillSnap.Client (http://localhost:5105)
│   └── Connected to API via HttpClient
└── Logs/api-{date}.log (Daily rotating logs)
```

### Production Considerations

**Database:**
- Migrate from SQLite to SQL Server or PostgreSQL
- Connection pooling configuration
- Backup and recovery strategy

**API Hosting:**
- Deploy to Azure App Service, AWS Elastic Beanstalk, or IIS
- Configure environment-specific `appsettings.json`
- Enable Application Insights for monitoring

**Client Hosting:**
- Deploy static files to CDN (Azure Storage, Cloudflare)
- Configure API base URL via environment variable
- Enable gzip/brotli compression

**Caching:**
- Consider Redis for distributed caching
- Enable Response Caching middleware
- Configure CDN caching rules

**Security:**
- Store JWT secrets in Azure Key Vault or AWS Secrets Manager
- Enable HTTPS with valid SSL certificate
- Configure firewall rules
- Implement rate limiting

---

## Scalability Considerations

### Current Limitations (SQLite + In-Memory Cache)
- Single-server deployment (no horizontal scaling)
- Cache not shared across instances
- File-based database limits concurrent writes

### Scalability Path

**Phase 1: Vertical Scaling**
- Increase server resources (CPU, RAM)
- Optimize database queries
- Implement connection pooling

**Phase 2: Distributed Caching**
- Replace IMemoryCache with Redis
- Share cache across multiple API instances
- Implement cache clustering

**Phase 3: Database Migration**
- Move to SQL Server or PostgreSQL
- Configure read replicas for queries
- Implement database sharding if needed

**Phase 4: Horizontal Scaling**
- Deploy multiple API instances behind load balancer
- Use Azure Front Door or AWS CloudFront
- Implement stateless API design

**Phase 5: Microservices (Future)**
- Split into Auth, Portfolio, Projects, Skills services
- Implement API Gateway (Azure API Management, Kong)
- Use message queues for async communication

---

## Monitoring and Observability

### Current Implementation

**Performance Monitoring:**
- Request timing with Stopwatch
- Slow query detection (>1000ms threshold)
- File logging with correlation IDs

**Log Files:**
```
Logs/api-20241215.log
```

**Log Format:**
```
[INFO] Request [abc123]: GET /api/projects completed in 15ms with status 200
[WARN] SLOW REQUEST [def456]: GET /api/portfoliousers/3 completed in 1250ms with status 200
```

### Production Monitoring Recommendations

**Application Performance Monitoring (APM):**
- Azure Application Insights
- New Relic
- Datadog

**Metrics to Track:**
- Request throughput (req/sec)
- Average response time
- Error rate (5xx responses)
- Cache hit ratio
- Database query time
- Memory usage

**Alerting:**
- Slow query threshold exceeded
- Error rate spike
- Memory exhaustion
- Disk space low (SQLite)

---

## Conclusion

SkillSnap implements a modern, scalable architecture with:
- Clear separation of concerns (three-tier)
- Performance optimization (two-level caching)
- Security best practices (JWT, authorization)
- Monitoring and observability
- Maintainable codebase with documentation

The architecture supports future enhancements like microservices, distributed caching, and cloud-native deployment while maintaining development simplicity for the current phase.

---

**Last Updated:** December 15, 2024  
**Architecture Version:** 1.0 (Phase 5 Complete)
