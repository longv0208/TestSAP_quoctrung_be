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

    private readonly INotifier _notifier;

    public UserService(IUserRepository users, INotifier notifier)
    {
        _users = users;
        _notifier = notifier;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _users.Query()
            .Where(u => !u.IsDeleted)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Unit)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        // Học vị nằm ở hồ sơ khoa học — lấy một lượt cho cả danh sách, tránh N+1.
        var ids = users.Select(u => u.Id).ToList();
        var degrees = await _users.AcademicProfiles
            .Where(a => ids.Contains(a.UserId) && a.DegreeLevel != null)
            .ToDictionaryAsync(a => a.UserId, a => a.DegreeLevel!);

        return users.Select(u => Map(u, degrees.GetValueOrDefault(u.Id)));
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Unit)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var degree = await _users.AcademicProfiles
            .Where(a => a.UserId == userId)
            .Select(a => a.DegreeLevel)
            .FirstOrDefaultAsync();

        return Map(user, degree);
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
                throw new KeyNotFoundException("Không tìm thấy vai trò.");
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

        // Gửi SAU CÙNG, khi tài khoản đã đủ vai — thư báo trước khi gán vai xong thì người ta đăng
        // nhập vào là màn hình trống.
        await SendWelcomeAsync(user, request.TemporaryPassword!);

        return await GetUserByIdAsync(user.Id);
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Phải nhập họ tên.");

        var user = await _users.Query()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        foreach (var roleId in request.Roles)
        {
            var roleExists = await _users.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                throw new KeyNotFoundException("Không tìm thấy vai trò.");
        }

        user.FullName = request.FullName;
        user.Phone = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        // Đơn vị + học vị trước đây NHẬN vào rồi bỏ đi: form sửa xong bấm Lưu, đóng lại mở ra là
        // trắng. `UserDto` cũng khai hai trường này mà không ai gán, nên giao diện luôn thấy rỗng.
        await ApplyUnitAndDegreeAsync(user, request.Department, request.AcademicDegree);

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
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

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
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Furpms@123456", workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
    }

    /// <summary>
    /// Ghi <b>đơn vị</b> (khớp theo tên hoặc mã trong danh mục <c>organizational_units</c>) và
    /// <b>học vị</b> (lưu ở <c>academic_profiles.DegreeLevel</c> — hồ sơ khoa học, không phải cột
    /// của bảng người dùng).
    /// <para>
    /// Không khớp được đơn vị thì <b>bỏ qua</b> chứ không ném lỗi: đây là thông tin phụ, chặn cả
    /// thao tác sửa tên chỉ vì gõ sai tên khoa là quá tay.
    /// </para>
    /// </summary>
    /// <summary>
    /// Gửi <b>thư chào mừng kèm mật khẩu tạm</b> cho tài khoản vừa tạo.
    /// <para>
    /// Trước đây tạo xong hệ thống im lặng: người được tạo <b>không biết mình có tài khoản</b>, và
    /// Admin phải tự nhắn mật khẩu tạm qua kênh khác — vừa phiền vừa không có dấu vết.
    /// </para>
    /// <para>
    /// <b>Phải <c>await</c></b>, không được bắn kiểu "chạy ngầm rồi quên": repository dùng chung một
    /// <c>DbContext</c> mà <c>DbContext</c> KHÔNG an toàn đa luồng — thả song song là hai luồng cùng
    /// ghi, gây lỗi ngay ở chính thao tác tạo tài khoản (đã đo được: trả về 409 trong khi tài khoản
    /// vẫn được tạo).
    /// </para>
    /// <para>
    /// Lỗi gửi mail thì đã được <c>IEmailService</c> nuốt và ghi <c>email_logs</c>, nên await ở đây
    /// vẫn không làm hỏng việc tạo tài khoản. Tắt <c>EMAIL_ENABLED</c> ⇒ ghi SKIPPED, chuông vẫn có.
    /// </para>
    /// </summary>
    private async Task SendWelcomeAsync(User user, string temporaryPassword)
    {
        await _notifier.NotifyAsync(
            user.Id,
            "ACCOUNT_CREATED",
            "Tài khoản FURPMS của bạn đã được tạo",
            $"Chào {user.FullName}, Phòng Quản lý khoa học đã tạo tài khoản cho bạn trên Hệ thống " +
            $"Quản lý đề tài NCKH (FURPMS). Email đăng nhập: {user.Email} — Mật khẩu tạm: " +
            $"{temporaryPassword}. Vui lòng đăng nhập và ĐỔI MẬT KHẨU ngay trong lần đầu sử dụng.",
            actionUrl: "/change-password",
            entityType: "User",
            entityId: user.Id.ToString(),
            priority: "HIGH");
    }

    private async Task ApplyUnitAndDegreeAsync(User user, string? department, int? academicDegree)
    {
        if (!string.IsNullOrWhiteSpace(department))
        {
            var name = department.Trim();
            var unit = await _users.OrganizationalUnits
                .FirstOrDefaultAsync(x => x.Name == name || x.Code == name);
            if (unit != null) user.UnitId = unit.Id;
        }

        if (academicDegree is not { } degree) return;

        var level = degree switch
        {
            0 => "Cử nhân",
            1 => "Thạc sĩ",
            2 => "Tiến sĩ",
            3 => "Giáo sư",
            _ => null
        };
        if (level == null) return;

        var profile = await _users.AcademicProfiles.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (profile == null)
        {
            _users.AddAcademicProfile(new AcademicProfile
            {
                UserId = user.Id, DegreeLevel = level, UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            profile.DegreeLevel = level;
            profile.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static UserDto Map(User u, string? degreeLevel = null) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        PhoneNumber = u.Phone,
        Department = u.Unit?.Name,
        AcademicDegree = degreeLevel,
        AccountType = u.UserRoles.FirstOrDefault()?.Role.Name ?? "—",
        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
        IsActive = u.Status == UserStatus.Active,
        MustChangePassword = false,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
