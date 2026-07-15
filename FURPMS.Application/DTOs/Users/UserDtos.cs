namespace FURPMS.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? AcademicDegree { get; set; }
    public string AccountType { get; set; } = null!;  // primary role
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public int? AcademicDegree { get; set; }
    public List<int> Roles { get; set; } = new();  // Role.Id values
    public string TemporaryPassword { get; set; } = null!;
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public int? AcademicDegree { get; set; }
    public List<int> Roles { get; set; } = new();
}
