using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Shared.Models;

/// <summary>
/// Represents a portfolio user with projects and skills.
/// </summary>
public class PortfolioUser
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio user.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's biography or personal description.
    /// </summary>
    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the user's profile image.
    /// </summary>
    [StringLength(255)]
    public string ProfileImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of projects associated with this user.
    /// </summary>
    public List<Project> Projects { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the collection of skills associated with this user.
    /// </summary>
    public List<Skill> Skills { get; set; } = new();
}
