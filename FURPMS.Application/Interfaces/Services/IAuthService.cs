using FURPMS.Application.DTOs.Auth;

namespace FURPMS.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserInfoResponse> GetCurrentUserAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>
    /// Gửi liên kết đặt lại mật khẩu vào hộp thư.
    /// <para>
    /// <b>Luôn coi như thành công</b> dù email có tồn tại hay không — trả lời khác nhau là biến
    /// màn "quên mật khẩu" thành công cụ dò xem ai có tài khoản trong hệ thống.
    /// </para>
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>Đặt lại mật khẩu bằng mã trong thư. Mã dùng MỘT LẦN và hết hạn sau 30 phút.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
