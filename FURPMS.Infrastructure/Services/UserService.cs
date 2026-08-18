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

    // Xoá tài khoản phải soi được ràng buộc nghiệp vụ ở nơi khác: người này có đang chủ nhiệm đề
    // tài hay ngồi hội đồng nào không. Không có hai kho này thì chỉ còn cách xoá mù.
    private readonly IProposalRepository _proposals;
    private readonly IReviewRepository _review;

    public UserService(IUserRepository users, INotifier notifier, IProposalRepository proposals, IReviewRepository review)
    {
        _users = users;
        _notifier = notifier;
        _proposals = proposals;
        _review = review;
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

    /// <summary>
    /// Kiểm định dạng email. Dùng <see cref="System.Net.Mail.MailAddress"/> thay vì biểu thức chính
    /// quy tự chế: cú pháp email (RFC 5322) rắc rối hơn mọi regex ngắn viết ra được, và regex dài
    /// thì vừa khó đọc vừa dễ dính bẫy quay lui.
    /// <para>
    /// Có hai lần kiểm thêm vì <c>MailAddress</c> vẫn nhận vài dạng không dùng được ở đây:
    /// nó chấp nhận cả <c>"Tên Hiển Thị &lt;a@b.c&gt;"</c> và cả tên miền không có dấu chấm
    /// (<c>a@localhost</c>).
    /// </para>
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (email.Contains(' ')) return false;
        try
        {
            var parsed = new System.Net.Mail.MailAddress(email);
            if (parsed.Address != email) return false;            // loại dạng "Tên <a@b.c>"
            var domain = parsed.Host;
            // Tên miền phải có ít nhất một dấu chấm và đuôi từ 2 ký tự trở lên: "a@localhost" hay
            // "a@fpt." không phải địa chỉ gửi thư được.
            var lastDot = domain.LastIndexOf('.');
            return lastDot > 0 && domain.Length - lastDot - 1 >= 2 && !domain.Contains("..");
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Phải nhập email.");

        // Trước 18/08 chỉ kiểm "có nhập gì chưa": gõ "abc" hay "a@@b" vẫn tạo được tài khoản, rồi
        // thư mời hội đồng và mật khẩu tạm gửi đi đâu mất — người dùng không bao giờ đăng nhập được
        // mà Admin cũng không biết vì sao. Email là ĐỊNH DANH đăng nhập nên phải chặn ngay tại cửa.
        request.Email = request.Email.Trim();
        if (!IsValidEmail(request.Email))
            throw new ArgumentException($"Email \"{request.Email}\" không đúng định dạng — ví dụ hợp lệ: ten.nguoidung@fpt.edu.vn");

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
    /// Xoá <b>mềm</b> tài khoản (<c>IsDeleted</c> + dấu vết ai xoá, lúc nào) — bảng
    /// <c>users</c> có bộ lọc toàn cục nên người bị xoá biến mất khỏi mọi danh sách.
    ///
    /// <para>
    /// <b>Vì sao xoá mềm chứ không xoá thật:</b> tài khoản bị tham chiếu khắp nơi — đề tài, hội
    /// đồng, điểm chấm, biên bản, nhật ký. Xoá cứng là đứt khoá ngoại hàng loạt, mà những dữ liệu
    /// đó là hồ sơ pháp lý của đề tài, không được phép mất theo người.
    /// </para>
    ///
    /// <para>
    /// <b>Vì sao vẫn phải chặn:</b> xoá mềm giấu người khỏi giao diện, nên nếu họ đang chủ nhiệm đề
    /// tài hay ngồi hội đồng thì màn hình kia sẽ hiện một ô trống không ai giải thích được. Gặp
    /// những trường hợp đó thì đúng việc phải làm là <b>vô hiệu hoá</b> (khoá đăng nhập, vẫn giữ
    /// tên), nên thông báo chỉ thẳng sang đó thay vì chỉ nói "không xoá được".
    /// </para>
    /// </summary>
    public async Task DeleteUserAsync(Guid userId, Guid deletedBy)
    {
        var user = await _users.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        // Tự xoá mình thì phiên đang đăng nhập trở thành tài khoản không tồn tại — đăng xuất giữa
        // chừng, không rõ vì sao.
        if (userId == deletedBy)
            throw new InvalidOperationException("Không thể tự xoá tài khoản của chính mình.");

        var isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
        if (isAdmin)
        {
            // Xoá người quản trị cuối cùng là khoá cửa vứt chìa: không còn ai tạo lại tài khoản nào.
            var otherAdmins = await _users.UserRoles
                .CountAsync(ur => ur.Role.Name == "Admin" && ur.UserId != userId && !ur.User.IsDeleted);
            if (otherAdmins == 0)
                throw new InvalidOperationException("Đây là quản trị viên duy nhất — xoá xong sẽ không còn ai quản trị hệ thống.");
        }

        var isPi = await _proposals.Projects.IgnoreQueryFilters().AnyAsync(p => p.PiUserId == userId);
        if (isPi)
            throw new InvalidOperationException(
                "Người này đang là chủ nhiệm đề tài nên không xoá được — hãy dùng \"Vô hiệu hoá\" để khoá đăng nhập mà vẫn giữ tên trên hồ sơ đề tài.");

        var isCouncilMember = await _review.CouncilMembers.AnyAsync(m => m.UserId == userId);
        if (isCouncilMember)
            throw new InvalidOperationException(
                "Người này đang là ủy viên hội đồng nên không xoá được — hãy gỡ khỏi hội đồng trước, hoặc dùng \"Vô hiệu hoá\".");

        var isTeamMember = await _proposals.ProjectMembers.AnyAsync(m => m.UserId == userId);
        if (isTeamMember)
            throw new InvalidOperationException(
                "Người này đang là thành viên tham gia đề tài nên không xoá được — hãy dùng \"Vô hiệu hoá\".");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;
        user.Status = "INACTIVE";   // chặn đăng nhập ngay, không phụ thuộc bộ lọc toàn cục
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
