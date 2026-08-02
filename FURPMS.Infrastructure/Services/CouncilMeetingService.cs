using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Meetings;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class CouncilMeetingService : ICouncilMeetingService
{
    private readonly IReviewRepository _review;

    public CouncilMeetingService(IReviewRepository review)
    {
        _review = review;
    }

    public async Task<IEnumerable<MeetingListDto>> GetAllAsync()
    {
        var meetings = await _review.Meetings
            .Include(m => m.Council)
                .ThenInclude(c => c.Round)
            .Include(m => m.Council)
                .ThenInclude(c => c.ProjectAssignments)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .OrderByDescending(m => m.ScheduledAt)
            .ToListAsync();

        return meetings.Select(MapList);
    }

    private static MeetingListDto MapList(Domain.Entities.Review.CouncilMeeting m) => new()
    {
        Id = m.Id,
        CouncilId = m.CouncilId,
        Title = m.Title,
        Platform = m.Platform,
        MeetingLink = m.MeetingLink,
        Location = m.Location,
        ScheduledAt = m.ScheduledAt,
        DurationMinutes = m.DurationMinutes,
        Agenda = m.Agenda,
        ActualStartAt = m.ActualStartAt,
        ActualEndAt = m.ActualEndAt,
        Status = m.Status,
        CancellationReason = m.CancellationReason,
        CreatedAt = m.CreatedAt,
        ProposalId = m.Council?.ProjectAssignments.FirstOrDefault()?.Project?.Proposals.FirstOrDefault()?.Id,   // id bản đề cương hiện hành (FE điều hướng)
        ProposalTitle = m.Council?.ProjectAssignments.FirstOrDefault()?.Project?.TitleVi,
        RoundType = m.Council?.Round?.RoundType,
        RoundNumber = m.Council?.Round?.RoundNumber,
    };

    // Lịch họp của hội đồng đang chấm ĐỀ TÀI CỦA PI này. PI phải trình bày trước hội đồng
    // (Process_Spec) nên cần biết ngày/giờ + địa điểm hoặc link.
    public async Task<IEnumerable<MeetingListDto>> GetForPiAsync(Guid piUserId)
    {
        var meetings = await _review.Meetings
            .Include(m => m.Council)
                .ThenInclude(c => c.Round)
            .Include(m => m.Council)
                .ThenInclude(c => c.ProjectAssignments)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .Where(m => m.Council!.ProjectAssignments.Any(a => a.Project.PiUserId == piUserId))
            .OrderByDescending(m => m.ScheduledAt)
            .ToListAsync();

        return meetings.Select(MapList);
    }

    public async Task<IEnumerable<MeetingDto>> GetByCouncilAsync(Guid councilId)
    {
        _ = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var meetings = await _review.Meetings
            .Where(m => m.CouncilId == councilId)
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();

        return meetings.Select(Map);
    }

    public async Task<MeetingDto> ScheduleAsync(Guid councilId, ScheduleMeetingRequest request)
    {
        _ = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        if (request.ScheduledAt <= DateTime.UtcNow)
            throw new ArgumentException("Meeting must be scheduled in the future.");
        if (request.DurationMinutes <= 0)
            throw new ArgumentException("DurationMinutes must be positive.");

        var validPlatforms = new[] { "IN_PERSON", "GOOGLE_MEET", "TEAMS", "ZOOM" };
        var platform = request.Platform.ToUpperInvariant().Replace(" ", "_");
        if (!validPlatforms.Contains(platform))
            platform = "IN_PERSON";

        // Họp trực tiếp bắt buộc có địa điểm; họp online thì bỏ địa điểm, giữ link.
        if (platform == "IN_PERSON" && string.IsNullOrWhiteSpace(request.Location))
            throw new ArgumentException("Họp trực tiếp phải nhập địa điểm.");

        var meeting = new CouncilMeeting
        {
            CouncilId = councilId,
            Title = request.Title,
            Platform = platform,
            MeetingLink = platform == "IN_PERSON" ? null : request.MeetingLink,
            Location = platform == "IN_PERSON" ? request.Location : null,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Agenda = request.Agenda,
            Status = MeetingStatus.Scheduled
        };

        await _review.AddMeetingAsync(meeting);
        await _review.SaveChangesAsync();
        return Map(meeting);
    }

    public async Task<MeetingDto> StartAsync(Guid meetingId)
    {
        var meeting = await _review.Meetings
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        if (meeting.Status != MeetingStatus.Scheduled)
            throw new InvalidOperationException($"Meeting is '{meeting.Status}'; only SCHEDULED meetings can be started.");

        meeting.ActualStartAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.InProgress;
        await _review.SaveChangesAsync();
        return Map(meeting);
    }

    public async Task<MeetingDto> EndAsync(Guid meetingId)
    {
        var meeting = await _review.Meetings
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        if (meeting.Status != MeetingStatus.InProgress)
            throw new InvalidOperationException($"Meeting is '{meeting.Status}'; only IN_PROGRESS meetings can be ended.");

        meeting.ActualEndAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.Completed;
        await _review.SaveChangesAsync();
        return Map(meeting);
    }

    // ── Điểm danh (rule tuần 10) ─────────────────────────────────────────────
    public async Task<IEnumerable<AttendanceEntryDto>> GetAttendanceAsync(Guid meetingId)
    {
        var meeting = await _review.Meetings
            .Include(m => m.Council).ThenInclude(c => c.Members).ThenInclude(mm => mm.User)
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        var existing = await _review.MeetingAttendances
            .Where(a => a.MeetingId == meetingId)
            .ToListAsync();
        var byMember = existing.ToDictionary(a => a.MemberId);

        return meeting.Council.Members
            .OrderBy(m => m.MemberRole)
            .Select(mm => new AttendanceEntryDto
            {
                MemberId = mm.Id,
                MemberName = mm.User.FullName,
                MemberRole = mm.MemberRole,
                Attended = byMember.TryGetValue(mm.Id, out var a) ? a.ActuallyAttended : null,
                AbsenceReason = byMember.TryGetValue(mm.Id, out var a2) ? a2.AbsenceReason : null
            })
            .ToList();
    }

    public async Task SaveAttendanceAsync(Guid meetingId, Guid callerId, bool isAdminStaff, SaveAttendanceRequest request)
    {
        var meeting = await _review.Meetings
            .Include(m => m.Council).ThenInclude(c => c.Members)
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        // Quyền: Admin/Staff, hoặc Thư ký của hội đồng này.
        var isSecretary = meeting.Council.Members.Any(mm =>
            mm.UserId == callerId && mm.MemberRole != null &&
            mm.MemberRole.Trim().Equals("Secretary", StringComparison.OrdinalIgnoreCase));
        if (!isAdminStaff && !isSecretary)
            throw new ForbiddenException("Chỉ Thư ký hội đồng (hoặc Admin/Staff) được điểm danh.");

        var memberIds = meeting.Council.Members.Select(m => m.Id).ToHashSet();
        var existing = await _review.MeetingAttendances
            .Where(a => a.MeetingId == meetingId)
            .ToListAsync();
        var byMember = existing.ToDictionary(a => a.MemberId);

        foreach (var e in request.Entries.Where(x => memberIds.Contains(x.MemberId)))
        {
            var reason = e.Attended == false ? e.AbsenceReason : null; // chỉ giữ lý do khi VẮNG
            if (byMember.TryGetValue(e.MemberId, out var a))
            {
                a.ActuallyAttended = e.Attended;
                a.AbsenceReason = reason;
            }
            else
            {
                await _review.AddMeetingAttendanceAsync(new MeetingAttendance
                {
                    MeetingId = meetingId,
                    MemberId = e.MemberId,
                    ActuallyAttended = e.Attended,
                    AbsenceReason = reason
                });
            }
        }
        await _review.SaveChangesAsync();
    }

    private static MeetingDto Map(CouncilMeeting m) => new()
    {
        Id = m.Id,
        CouncilId = m.CouncilId,
        Title = m.Title,
        Platform = m.Platform,
        MeetingLink = m.MeetingLink,
        Location = m.Location,
        ScheduledAt = m.ScheduledAt,
        DurationMinutes = m.DurationMinutes,
        Agenda = m.Agenda,
        ActualStartAt = m.ActualStartAt,
        ActualEndAt = m.ActualEndAt,
        Status = m.Status,
        CancellationReason = m.CancellationReason,
        CreatedAt = m.CreatedAt
    };
}
