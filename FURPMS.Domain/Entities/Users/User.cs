namespace FURPMS.Domain.Entities.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = null!;
    public string? FptEmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FptSsoSub { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime? LastLoginAt { get; set; }
    public bool IsExternal { get; set; }
    public int? UnitId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    /// <summary>
    /// <b>Băm SHA-256</b> của mã đặt lại mật khẩu — không lưu mã gốc.
    /// <para>
    /// Ai đọc được DB cũng không dùng được để chiếm tài khoản; mã gốc chỉ tồn tại trong đúng lá
    /// thư gửi đi. Dùng một lần: đặt lại xong thì xoá.
    /// </para>
    /// </summary>
    public string? PasswordResetTokenHash { get; set; }

    /// <summary>Hạn của mã đặt lại (30 phút). Quá hạn coi như không có mã.</summary>
    public DateTime? PasswordResetExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public OrganizationalUnit? Unit { get; set; }
    public User? DeletedByUser { get; set; }
    public AcademicProfile? AcademicProfile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
