using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Api.Data;
using SkillSnap.Api.Middleware;
using SkillSnap.Api.Models;
using SkillSnap.Api.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure file logging with daily rolling files
// Use ContentRootPath to store logs in project root (SkillSnap.Api/Logs/)
// instead of bin folder, making them easier to find and persistent across rebuilds
builder.Logging.AddFile(options =>
{
    options.RootPath = builder.Environment.ContentRootPath;
    options.BasePath = "Logs";
    options.Files = new[]
    {
        new Karambolo.Extensions.Logging.File.LogFileOptions
        {
            Path = "api-<date:yyyyMMdd>.log"
        }
    };
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext with SQLite
builder.Services.AddDbContext<SkillSnapContext>(options =>
    options.UseSqlite("Data Source=skillsnap.db"));

// Add Memory Cache
builder.Services.AddMemoryCache();

// Register custom services
builder.Services.AddScoped<CacheHelper>();

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<SkillSnapContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SkillSnapApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SkillSnapClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5105",   // Blazor Client HTTP
                "https://localhost:5105",  // Blazor Client HTTPS
                "https://localhost:5001",
                "https://localhost:5002",
                "https://localhost:7001",
                "https://localhost:7002")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbSeeder.SeedRolesAndAdminUser(roleManager, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles and admin user");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable performance monitoring middleware (before CORS and auth)
app.UseMiddleware<PerformanceMonitoringMiddleware>();

// Enable CORS
app.UseCors("AllowBlazorClient");

// Authentication & Authorization middleware (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
