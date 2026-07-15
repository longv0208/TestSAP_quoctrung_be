namespace FURPMS.Application.DTOs.Auth;

public class UserInfoResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public DateTime? LastLoginAt { get; set; }
}
