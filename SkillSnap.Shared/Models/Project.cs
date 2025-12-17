using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

/// <summary>
/// Represents a portfolio project with title, description, and associated user.
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
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project description.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the project image.
    /// </summary>
    [StringLength(255)]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreign key to the portfolio user who owns this project.
    /// </summary>
    [ForeignKey(nameof(PortfolioUser))]
    public int PortfolioUserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the portfolio user who owns this project.
    /// </summary>
    public PortfolioUser? PortfolioUser { get; set; }
}
