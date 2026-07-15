using FURPMS.Application.DTOs.Meetings;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilMeetingService
{
    Task<IEnumerable<MeetingListDto>> GetAllAsync();
    Task<IEnumerable<MeetingDto>> GetByCouncilAsync(Guid councilId);
    Task<MeetingDto> ScheduleAsync(Guid councilId, ScheduleMeetingRequest request);
    Task<MeetingDto> StartAsync(Guid meetingId);
    Task<MeetingDto> EndAsync(Guid meetingId);
}
