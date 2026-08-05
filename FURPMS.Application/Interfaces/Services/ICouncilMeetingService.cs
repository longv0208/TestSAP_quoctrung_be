using FURPMS.Application.DTOs.Meetings;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilMeetingService
{
    Task<IEnumerable<MeetingListDto>> GetAllAsync();
    /// <summary>Lịch họp hội đồng chấm đề tài của PI này (PI phải trình bày trước hội đồng).</summary>
    Task<IEnumerable<MeetingListDto>> GetForPiAsync(Guid piUserId);
    Task<IEnumerable<MeetingDto>> GetByCouncilAsync(Guid councilId);
    Task<MeetingDto> ScheduleAsync(Guid councilId, ScheduleMeetingRequest request);
    Task<MeetingDto> UpdateAsync(Guid meetingId, UpdateMeetingRequest request);
    Task DeleteAsync(Guid meetingId);
    Task<MeetingDto> StartAsync(Guid meetingId);
    Task<MeetingDto> EndAsync(Guid meetingId);

    // Điểm danh (rule tuần 10): list theo DS hội đồng; Thư ký/Admin/Staff lưu.
    Task<IEnumerable<AttendanceEntryDto>> GetAttendanceAsync(Guid meetingId);
    Task SaveAttendanceAsync(Guid meetingId, Guid callerId, bool isAdminStaff, SaveAttendanceRequest request);
}
