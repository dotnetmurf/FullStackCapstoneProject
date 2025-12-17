using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Shared.DTOs;

/// <summary>
/// Lightweight DTO for project list views - excludes navigation properties for better performance.
/// </summary>
public class ProjectSummaryDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Url]
    public string? ImageUrl { get; set; }

    public int PortfolioUserId { get; set; }

    public string PortfolioUserName { get; set; } = string.Empty;
}
