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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public OrganizationalUnit? Unit { get; set; }
    public User? DeletedByUser { get; set; }
    public AcademicProfile? AcademicProfile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
