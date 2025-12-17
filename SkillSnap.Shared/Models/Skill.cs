using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Shared.Models;

public class Skill
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string Level { get; set; } = string.Empty;

    // Foreign key
    [ForeignKey(nameof(PortfolioUser))]
    public int PortfolioUserId { get; set; }

    // Navigation property
    public PortfolioUser? PortfolioUser { get; set; }
}
