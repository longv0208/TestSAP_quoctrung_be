namespace FURPMS.Application.DTOs.Auth;

/// <summary>Bước 1 — xin mã đặt lại mật khẩu, gửi vào hộp thư.</summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}

/// <summary>Bước 2 — đổi mật khẩu bằng mã nhận được trong thư.</summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
