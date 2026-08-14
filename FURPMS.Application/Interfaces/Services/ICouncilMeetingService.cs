using FURPMS.Application.DTOs.Meetings;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilMeetingService
{
    Task<IEnumerable<MeetingListDto>> GetAllAsync();
    /// <summary>Lịch họp hội đồng chấm đề tài của PI này (PI phải trình bày trước hội đồng).</summary>
    Task<IEnumerable<MeetingListDto>> GetForPiAsync(Guid piUserId);

    /// <summary>
    /// Lịch họp của các hội đồng mà người này là <b>thành viên</b> (Chủ tịch/Thư ký/Phản biện/Uỷ viên).
    /// <para>
    /// Trước 14/08 không có đường này: menu "Lịch họp" hiện cho cả vai Hội đồng, nhưng nó gọi
    /// <c>GET /api/meetings</c> — endpoint chỉ dành cho Admin/Staff ⇒ người chấm bấm vào là ăn
    /// <b>403</b>. Mà họ mới chính là người cần biết họp lúc nào, ở đâu.
    /// </para>
    /// </summary>
    Task<IEnumerable<MeetingListDto>> GetForCouncilMemberAsync(Guid userId);
    Task<IEnumerable<MeetingDto>> GetByCouncilAsync(Guid councilId);
    Task<MeetingDto> ScheduleAsync(Guid councilId, ScheduleMeetingRequest request);
    Task<MeetingDto> UpdateAsync(Guid meetingId, UpdateMeetingRequest request);
    Task DeleteAsync(Guid meetingId);
    Task<MeetingDto> StartAsync(Guid meetingId);
    /// <summary>Hoàn tác "Bắt đầu" — về lại trạng thái đã lên lịch (chỉ khi chưa ai điểm danh).</summary>
    Task<MeetingDto> UndoStartAsync(Guid meetingId);

    Task<MeetingDto> EndAsync(Guid meetingId);

    // Điểm danh (rule tuần 10): list theo DS hội đồng; Thư ký/Admin/Staff lưu.
    Task<IEnumerable<AttendanceEntryDto>> GetAttendanceAsync(Guid meetingId);
    Task SaveAttendanceAsync(Guid meetingId, Guid callerId, bool isAdminStaff, SaveAttendanceRequest request);
}
