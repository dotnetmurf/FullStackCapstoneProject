using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Models;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Data;

public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public SkillSnapContext(DbContextOptions<SkillSnapContext> options) : base(options)
    {
    }

    public DbSet<PortfolioUser> PortfolioUsers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Skill> Skills { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<PortfolioUser>()
            .HasMany(u => u.Projects)
            .WithOne(p => p.PortfolioUser)
            .HasForeignKey(p => p.PortfolioUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PortfolioUser>()
            .HasMany(u => u.Skills)
            .WithOne(s => s.PortfolioUser)
            .HasForeignKey(s => s.PortfolioUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        ConfigureIndexes(modelBuilder);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Projects foreign key index - improves queries filtering by PortfolioUserId
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.PortfolioUserId)
            .HasDatabaseName("IX_Projects_PortfolioUserId");

        // Skills foreign key index - improves queries filtering by PortfolioUserId
        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.PortfolioUserId)
            .HasDatabaseName("IX_Skills_PortfolioUserId");
    }
}
