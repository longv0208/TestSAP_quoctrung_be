using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilService
{
    /// <summary>
    /// Danh sách hội đồng cho màn quản lý của Phòng QLKH — kèm số liệu tóm tắt và
    /// <b>danh sách việc còn thiếu để gửi được thư mời</b>.
    /// <para>
    /// Trước 14/08 <b>không có endpoint nào liệt kê hội đồng</b>: chỉ có tạo, xoá, và xem hội đồng
    /// của chính mình (dành cho người chấm). Nên màn "Hội đồng" của chuyên viên buộc phải hiện tạm
    /// bảng ĐỀ TÀI — trùng y hệt màn "Xét duyệt đề cương", và không có chỗ nào xem được hội đồng.
    /// </para>
    /// </summary>
    Task<IEnumerable<CouncilListItemDto>> GetCouncilsAsync(CouncilQueryParams query);

    /// <summary>Chi tiết một hội đồng (cùng shape với một dòng trong danh sách).</summary>
    Task<CouncilListItemDto> GetCouncilByIdAsync(Guid councilId);

    /// <summary>Sửa thông tin hành chính: số quyết định, ngày thành lập, hạn họp, số thành viên.</summary>
    Task<CouncilListItemDto> UpdateCouncilAsync(Guid councilId, UpdateCouncilRequest request);

    /// <summary>
    /// Đổi vai trò một thành viên. Chặn hai Chủ tịch / hai Thư ký trong cùng hội đồng, và chặn
    /// hạ vai người đã chấm hoặc đã ký biên bản.
    /// </summary>
    Task<CouncilMemberResponse> UpdateMemberRoleAsync(Guid memberId, string memberRole);

    Task<CouncilResponse> CreateCouncilAsync(CreateCouncilRequest request, Guid createdBy);
    Task<CouncilMemberResponse> AddMemberAsync(Guid councilId, AddCouncilMemberRequest request);
    Task<int> SendInvitationsAsync(Guid councilId, DateTime? confirmDeadline);
    Task<IEnumerable<MyMembershipDto>> GetMyMembershipsAsync(Guid userId);
    Task<CouncilMemberResponse> RespondToMembershipAsync(Guid memberId, Guid userId, bool accept, string? declineReason);
    // Staff/Admin xác nhận thay thành viên (reviewer đồng ý qua điện thoại/email ngoài hệ thống,
    // hoặc tiện demo khi không có tài khoản reviewer để tự bấm nhận).
    /// <summary>
    /// Chuyên viên trả lời thư mời <b>thay</b> thành viên (họ đã đồng ý/từ chối ngoài hệ thống).
    /// <para>
    /// Chịu công tắc <c>COUNCIL_ALLOW_RESPOND_ON_BEHALF</c>, và luôn ghi lại ai đã bấm hộ.
    /// </para>
    /// </summary>
    /// <param name="accept">true = xác nhận thay · false = đánh dấu đã từ chối.</param>
    Task<CouncilMemberResponse> RespondOnBehalfAsync(
        Guid memberId, Guid staffUserId, bool accept, string? declineReason);
    Task<IEnumerable<CouncilMemberResponse>> GetMembersAsync(Guid councilId);
    Task RemoveMemberAsync(Guid memberId);
    // Xoá cả hội đồng (chỉ khi chưa có phiếu chấm / biên bản / nghiệm thu).
    Task DeleteCouncilAsync(Guid councilId);

    // Gán / gỡ 1 đề tài vào hội đồng có sẵn (dropdown ở màn Hội đồng & Chấm).
    Task AssignProjectToCouncilAsync(Guid councilId, Guid projectId);
    Task RemoveProjectFromCouncilAsync(Guid councilId, Guid projectId);

    // Cảnh báo trùng lịch: thành viên hội đồng này có mặt ở hội đồng khác họp giao giờ.
    Task<IEnumerable<ScheduleConflictDto>> GetScheduleConflictsAsync(Guid councilId);

    // Slot theo đề tài (rule tuần 10): khung giờ con từng đề tài trong buổi họp.
    Task<IEnumerable<CouncilSlotDto>> GetCouncilSlotsAsync(Guid councilId);
    /// <summary>Slot + khung giờ buổi họp + tổng phút đã xếp.</summary>
    Task<CouncilSlotBoardDto> GetCouncilSlotBoardAsync(Guid councilId);
    Task SaveCouncilSlotsAsync(Guid councilId, SaveSlotsRequest request);
}
