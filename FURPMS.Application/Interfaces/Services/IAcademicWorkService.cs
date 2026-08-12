using FURPMS.Application.DTOs.Users;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Công trình khoa học &amp; đề tài trong lý lịch (QĐ543 — Biểu mẫu 02, mục 13 · 14.6 · 15 ·
/// 16.3 · 17 · 18 · 19.4).
/// <para>
/// Mọi thao tác ghi đều <b>tính lại các ô đếm</b> của hồ sơ (mục 14.1–14.5, 15, 19.1/19.3) —
/// biểu mẫu đòi cả số lẫn danh sách, khai tay hai chỗ riêng thì sớm muộn cũng lệch.
/// </para>
/// </summary>
public interface IAcademicWorkService
{
    /// <param name="requesterId">Người đang gọi — dùng để kiểm quyền xem.</param>
    /// <param name="requesterIsAdminOrStaff">Admin/Staff xem được hồ sơ người khác để thẩm định.</param>
    Task<List<AcademicWorkResponse>> ListAsync(Guid userId, Guid requesterId, bool requesterIsAdminOrStaff);

    /// <summary>Chỉ chính chủ khai được — kể cả Admin cũng không khai hộ.</summary>
    Task<AcademicWorkResponse> CreateAsync(Guid userId, Guid requesterId, AcademicWorkRequest request);

    Task<AcademicWorkResponse> UpdateAsync(Guid userId, Guid requesterId, Guid workId, AcademicWorkRequest request);

    Task DeleteAsync(Guid userId, Guid requesterId, Guid workId);
}
