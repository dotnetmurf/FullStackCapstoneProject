namespace SkillSnap.Shared.DTOs;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? Email { get; set; }
    public DateTime? Expiration { get; set; }
}
