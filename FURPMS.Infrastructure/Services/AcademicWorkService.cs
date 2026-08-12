using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Users;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IAcademicWorkService"/>
public class AcademicWorkService : IAcademicWorkService
{
    private readonly IMasterDataRepository _repo;

    public AcademicWorkService(IMasterDataRepository repo) => _repo = repo;

    public async Task<List<AcademicWorkResponse>> ListAsync(Guid userId, Guid requesterId, bool requesterIsAdminOrStaff)
    {
        if (userId != requesterId && !requesterIsAdminOrStaff)
            throw new ForbiddenException("Chỉ xem được lý lịch khoa học của chính mình.");

        return await _repo.AcademicWorks
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.WorkType)
            .ThenBy(w => w.SortOrder)
            .ThenByDescending(w => w.Year)
            .Select(w => AcademicWorkResponse.From(w))
            .ToListAsync();
    }

    public async Task<AcademicWorkResponse> CreateAsync(Guid userId, Guid requesterId, AcademicWorkRequest request)
    {
        AssertIsOwner(userId, requesterId);
        Validate(request);

        var work = new AcademicWork { UserId = userId };
        Apply(work, request);
        _repo.Add(work);
        await _repo.SaveChangesAsync();

        await RecomputeCountsAsync(userId);
        return AcademicWorkResponse.From(work);
    }

    public async Task<AcademicWorkResponse> UpdateAsync(
        Guid userId, Guid requesterId, Guid workId, AcademicWorkRequest request)
    {
        AssertIsOwner(userId, requesterId);
        Validate(request);

        var work = await _repo.AcademicWorks.FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy công trình này trong lý lịch khoa học.");

        Apply(work, request);
        work.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        await RecomputeCountsAsync(userId);
        return AcademicWorkResponse.From(work);
    }

    public async Task DeleteAsync(Guid userId, Guid requesterId, Guid workId)
    {
        AssertIsOwner(userId, requesterId);

        var work = await _repo.AcademicWorks.FirstOrDefaultAsync(w => w.Id == workId && w.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy công trình này trong lý lịch khoa học.");

        _repo.Remove(work);
        await _repo.SaveChangesAsync();

        await RecomputeCountsAsync(userId);
    }

    // ── Quyền ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi thì <b>chỉ chính chủ</b> — kể cả Admin. Lý lịch khoa học là lời khai có trách nhiệm
    /// của người đứng tên; để người khác khai hộ là làm hỏng chính giá trị pháp lý của nó.
    /// </summary>
    private static void AssertIsOwner(Guid userId, Guid requesterId)
    {
        if (userId == requesterId) return;
        throw new ForbiddenException(
            "Chỉ chính chủ mới khai được lý lịch khoa học của mình — kể cả Quản trị viên cũng không khai hộ.");
    }

    // ── Kiểm tra dữ liệu ────────────────────────────────────────────────────

    private static void Validate(AcademicWorkRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Title))
            throw new ArgumentException("Phải nhập tên công trình.");

        if (!WorkTypes.All.Contains(r.WorkType))
            throw new ArgumentException($"Loại công trình không hợp lệ: '{r.WorkType}'.");

        var allowed = WorkCategories.For(r.WorkType);
        if (!allowed.Contains(r.Category))
            throw new ArgumentException(
                $"Phân loại '{r.Category}' không dùng được cho loại '{r.WorkType}'. " +
                $"Giá trị hợp lệ: {string.Join(", ", allowed)}.");

        if (!string.IsNullOrWhiteSpace(r.Role) && !WorkRoles.All.Contains(r.Role))
            throw new ArgumentException($"Vai trò không hợp lệ: '{r.Role}'.");

        if (!string.IsNullOrWhiteSpace(r.Status) && !WorkStatuses.All.Contains(r.Status))
            throw new ArgumentException($"Tình trạng không hợp lệ: '{r.Status}'.");

        // Năm sai kéo theo cả thống kê sai, mà gõ nhầm năm là chuyện rất hay xảy ra.
        if (r.Year is < 1900 or > 2100)
            throw new ArgumentException("Năm công bố phải nằm trong khoảng 1900–2100.");
        if (r.Year > DateTime.UtcNow.Year + 1)
            throw new ArgumentException($"Năm công bố ({r.Year}) vượt quá năm hiện tại — kiểm tra lại.");
        if (r.StartYear is < 1900 or > 2100)
            throw new ArgumentException("Năm bắt đầu phải nằm trong khoảng 1900–2100.");
        if (r.StartYear.HasValue && r.Year.HasValue && r.StartYear > r.Year)
            throw new ArgumentException("Năm bắt đầu không được sau năm kết thúc.");
    }

    private static void Apply(AcademicWork w, AcademicWorkRequest r)
    {
        w.WorkType = r.WorkType;
        w.Category = r.Category;
        w.Title = r.Title.Trim();
        w.Venue = Clean(r.Venue);
        w.Authors = Clean(r.Authors);
        w.Role = Clean(r.Role);
        w.Year = r.Year;
        w.StartYear = r.StartYear;
        w.Identifier = Clean(r.Identifier);
        w.Volume = Clean(r.Volume);
        w.Pages = Clean(r.Pages);
        w.Status = Clean(r.Status);
        w.Url = Clean(r.Url);
        w.Note = Clean(r.Note);
        w.SortOrder = r.SortOrder;
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ── Số suy ra ───────────────────────────────────────────────────────────

    /// <summary>
    /// Tính lại các ô đếm của <see cref="AcademicProfile"/> từ danh sách công trình.
    /// <para>
    /// Ô đếm <b>không còn nhập tay</b> nhưng vẫn giữ trong bảng vì chỗ khác đọc chúng. Nguồn sự
    /// thật nay là danh sách; số chỉ là bản tóm tắt dựng lại sau mỗi thay đổi — nên mục
    /// 14.1–14.5 và 14.6 của BM02 không bao giờ mâu thuẫn nhau.
    /// </para>
    /// </summary>
    private async Task RecomputeCountsAsync(Guid userId)
    {
        var profile = await _repo.AcademicProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            // Chưa có hồ sơ mà đã khai công trình — tạo vỏ hồ sơ để chỗ chứa số có tồn tại.
            profile = new AcademicProfile { UserId = userId };
            _repo.Add(profile);
        }

        var byCategory = await _repo.AcademicWorks
            .Where(w => w.UserId == userId)
            .GroupBy(w => w.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);

        int N(string category) => byCategory.TryGetValue(category, out var n) ? n : 0;

        // Ánh xạ đúng theo số mục của BM02 — mỗi ô đếm ứng với đúng một phân loại.
        profile.IsiScopusCount = N(WorkCategories.IsiScopus);                    // 14.1
        profile.IntlJournalCount = N(WorkCategories.JournalIntl);                // 14.2
        profile.DomesticJournalCount = N(WorkCategories.JournalDomestic);        // 14.3
        profile.IntlConferenceCount = N(WorkCategories.ConferenceIntl);          // 14.4
        profile.DomesticConferenceCount = N(WorkCategories.ConferenceDomestic);  // 14.5
        profile.PatentsCount = N(WorkCategories.PatentGranted);                  // 15
        profile.PhdSupervisedCount = N(WorkCategories.PhdSupervision);           // 19.1
        profile.MasterSupervisedCount = N(WorkCategories.MasterSupervision);     // 19.3
        profile.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
    }
}
