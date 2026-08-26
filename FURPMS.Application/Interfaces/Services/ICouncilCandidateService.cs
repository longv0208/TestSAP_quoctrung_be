using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Danh sách ứng viên ủy viên hội đồng, <b>đã xếp hạng theo chuyên môn</b>.
///
/// <para>Đây chính là thứ tài liệu cũ gọi là <c>/ai/suggest-reviewers</c>. Nó <b>không phải AI</b> —
/// là một phép nối bảng người ↔ lĩnh vực rồi sắp xếp. Gọi một truy vấn SQL là "AI" đúng vào cái bẫy
/// mà cẩm nang chống trượt phạt nặng nhất: *"gọi là AI nhưng thực chất if-else"*.</para>
/// </summary>
public interface ICouncilCandidateService
{
    /// <param name="projectId">Đề tài đang lập hội đồng — dùng để suy ra lĩnh vực và kiểm xung đột lợi ích.</param>
    /// <param name="trackId">Chỉ định lĩnh vực trực tiếp, khi chưa chọn đề tài nào.</param>
    /// <param name="councilId">Hội đồng đang thao tác — để đánh dấu ai đã có tên trong đó.</param>
    Task<CouncilCandidatesResponse> GetCandidatesAsync(
        Guid? projectId, int? trackId, Guid? councilId);
}
