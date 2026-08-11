using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilService
{
    Task<CouncilResponse> CreateCouncilAsync(CreateCouncilRequest request, Guid createdBy);
    Task<CouncilMemberResponse> AddMemberAsync(Guid councilId, AddCouncilMemberRequest request);
    Task<int> SendInvitationsAsync(Guid councilId, DateTime? confirmDeadline);
    Task<IEnumerable<MyMembershipDto>> GetMyMembershipsAsync(Guid userId);
    Task<CouncilMemberResponse> RespondToMembershipAsync(Guid memberId, Guid userId, bool accept, string? declineReason);
    // Staff/Admin xác nhận thay thành viên (reviewer đồng ý qua điện thoại/email ngoài hệ thống,
    // hoặc tiện demo khi không có tài khoản reviewer để tự bấm nhận).
    Task<CouncilMemberResponse> ConfirmMemberOnBehalfAsync(Guid memberId);
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
