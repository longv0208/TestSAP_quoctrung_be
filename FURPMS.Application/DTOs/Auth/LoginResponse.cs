namespace FURPMS.Application.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserInfoResponse User { get; set; } = null!;
}
