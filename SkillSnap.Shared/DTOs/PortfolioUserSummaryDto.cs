using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Shared.DTOs;

/// <summary>
/// Lightweight DTO for portfolio user list views - excludes related projects and skills for better performance.
/// Includes counts only to avoid loading large collections.
/// </summary>
public class PortfolioUserSummaryDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;

    [Url]
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Total number of projects for this user.
    /// </summary>
    public int ProjectCount { get; set; }

    /// <summary>
    /// Total number of skills for this user.
    /// </summary>
    public int SkillCount { get; set; }
}
