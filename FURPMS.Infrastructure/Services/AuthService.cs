using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Auth;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IJwtService _jwt;

    private readonly INotifier _notifier;

    public AuthService(IUserRepository users, IJwtService jwt, INotifier notifier)
    {
        _users = users;
        _jwt = jwt;
        _notifier = notifier;
    }

    /// <summary>Mã sống 30 phút — đủ để mở thư và đổi, ngắn đủ để mã lọt ra ngoài cũng nhanh vô dụng.</summary>
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc/>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Phải nhập email.");

        var user = await _users.Query()
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim() && !u.IsDeleted);

        // Email không tồn tại (hoặc tài khoản bị khoá) thì IM LẶNG kết thúc — không ném lỗi.
        // Trả lời khác nhau giữa "có" và "không có" là biến màn này thành công cụ dò tài khoản.
        if (user == null || user.Status != "ACTIVE") return;

        // Mã gốc chỉ tồn tại trong lá thư; DB giữ bản băm.
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = HashToken(token);
        user.PasswordResetExpiresAt = DateTime.UtcNow.Add(ResetTokenLifetime);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();

        await _notifier.NotifyAsync(
            user.Id,
            "PASSWORD_RESET",
            "Đặt lại mật khẩu FURPMS",
            $"Chào {user.FullName}, có yêu cầu đặt lại mật khẩu cho tài khoản {user.Email}. " +
            $"Mã đặt lại: {token} — mã có hiệu lực trong 30 phút và chỉ dùng được MỘT LẦN. " +
            "Nếu không phải bạn yêu cầu, hãy bỏ qua thư này; mật khẩu hiện tại vẫn giữ nguyên.",
            actionUrl: $"/reset-password?token={token}",
            entityType: "User",
            entityId: user.Id.ToString(),
            priority: "HIGH");
    }

    /// <inheritdoc/>
    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException("Thiếu mã đặt lại mật khẩu.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        var hash = HashToken(request.Token.Trim());
        var user = await _users.Query().FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hash && !u.IsDeleted);

        // Gộp chung một câu cho cả "sai mã" lẫn "hết hạn": nói rõ mã nào sai thì người dò biết
        // mình đang đi đúng hướng.
        if (user == null || user.PasswordResetExpiresAt == null || user.PasswordResetExpiresAt < DateTime.UtcNow)
            throw new ArgumentException(
                "Mã đặt lại không đúng hoặc đã hết hạn (mã chỉ sống 30 phút và dùng được một lần). " +
                "Hãy yêu cầu gửi lại mã mới.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        // Xoá mã ngay — dùng một lần, không để lại đường quay lại.
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("Tài khoản đang bị khoá. Liên hệ quản trị viên.");

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = _jwt.GenerateToken(user, roles);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = _jwt.ExpiryMinutes * 60,
            User = MapUserInfo(user, roles)
        };
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return MapUserInfo(user, roles);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
    }

    private static UserInfoResponse MapUserInfo(Domain.Entities.Users.User user, List<string> roles) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            Roles = roles,
            LastLoginAt = user.LastLoginAt
        };
}
