using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Users;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _users.Query()
            .Where(u => !u.IsDeleted)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(Map);
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return Map(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Phải nhập email.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Phải nhập họ tên.");
        if (string.IsNullOrWhiteSpace(request.TemporaryPassword))
            throw new ArgumentException("Phải nhập mật khẩu tạm.");

        var exists = await _users.Query().AnyAsync(u => u.Email == request.Email && !u.IsDeleted);
        if (exists)
            throw new InvalidOperationException($"Đã có tài khoản dùng email {request.Email}.");

        foreach (var roleId in request.Roles)
        {
            var roleExists = await _users.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                throw new KeyNotFoundException($"Role {roleId} not found.");
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword, workFactor: 12),
            Status = UserStatus.Active
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        foreach (var roleId in request.Roles)
        {
            _users.AddUserRole(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedBy = createdBy
            });
        }
        await _users.SaveChangesAsync();

        return await GetUserByIdAsync(user.Id);
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Phải nhập họ tên.");

        var user = await _users.Query()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        foreach (var roleId in request.Roles)
        {
            var roleExists = await _users.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                throw new KeyNotFoundException($"Role {roleId} not found.");
        }

        user.FullName = request.FullName;
        user.Phone = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var existingRoles = user.UserRoles.ToList();
        _users.RemoveUserRoles(existingRoles);

        foreach (var roleId in request.Roles)
        {
            _users.AddUserRole(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await _users.SaveChangesAsync();
        return await GetUserByIdAsync(userId);
    }

    // Khoá/mở tài khoản (Admin) — FE UserManagement gọi PATCH /api/users/{id}/toggle-active.
    public async Task<UserDto> ToggleActiveAsync(Guid userId)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.Status = user.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
        return Map(user);
    }

    // Reset mật khẩu về mặc định "Furpms@123456" (Admin) — user tự đổi sau khi đăng nhập.
    public async Task ResetPasswordAsync(Guid userId)
    {
        var user = await _users.Query()
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Furpms@123456", workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
    }

    private static UserDto Map(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        PhoneNumber = u.Phone,
        AccountType = u.UserRoles.FirstOrDefault()?.Role.Name ?? "—",
        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
        IsActive = u.Status == UserStatus.Active,
        MustChangePassword = false,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
