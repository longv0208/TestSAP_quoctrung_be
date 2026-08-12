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
        // Bản ghi cũ còn GOOGLE_MEET/TEAMS/ZOOM — quy về ONLINE khi đọc, khỏi phải migration.
        Platform = m.Platform == MeetingPlatform.InPerson ? MeetingPlatform.InPerson : MeetingPlatform.Online,
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

        var platform = ValidateAndNormalize(request, requireFuture: true);

        var meeting = new CouncilMeeting
        {
            CouncilId = councilId,
            Title = request.Title,
            Platform = platform,
            MeetingLink = platform == MeetingPlatform.InPerson ? null : request.MeetingLink,
            Location = platform == MeetingPlatform.InPerson ? request.Location : null,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Agenda = request.Agenda,
            Status = MeetingStatus.Scheduled
        };

        await _review.AddMeetingAsync(meeting);
        await _review.SaveChangesAsync();
        return Map(meeting);
    }

    /// <summary>
    /// Ràng buộc dùng chung cho TẠO và SỬA buổi họp. Tách ra để hai đường không lệch nhau —
    /// sửa mà lỏng hơn tạo thì người dùng lách được bằng cách tạo bừa rồi sửa lại.
    /// </summary>
    private static string ValidateAndNormalize(ScheduleMeetingRequest request, bool requireFuture)
    {
        // Buổi họp đã bắt đầu/đã qua thì không đòi "phải ở tương lai" nữa — Staff vẫn cần sửa
        // địa điểm hay link ghi nhầm sau khi họp xong.
        if (requireFuture && request.ScheduledAt <= DateTime.UtcNow)
            throw new ArgumentException("Buổi họp phải được đặt ở thời điểm trong tương lai.");
        if (request.DurationMinutes <= 0)
            throw new ArgumentException("Thời lượng họp phải lớn hơn 0 phút.");

        /*
         * Chỉ còn 2 hình thức: ONLINE / IN_PERSON (thầy 05/08 — cả 2 bản note đều nêu:
         * "chỉ cần ghi onl hay offline thay vì ghi onl meet hay onl microsoft team").
         * Nền tảng cụ thể không phải việc của hệ thống — Staff dán link nào cũng được.
         *
         * Dữ liệu CŨ vẫn còn GOOGLE_MEET/TEAMS/ZOOM nên map về ONLINE thay vì migration,
         * và vẫn nhận các giá trị đó ở request để FE/bản cũ không gãy.
         */
        var platform = request.Platform.ToUpperInvariant().Replace(" ", "_");
        platform = platform switch
        {
            "IN_PERSON" or "OFFLINE" => MeetingPlatform.InPerson,
            _ => MeetingPlatform.Online,   // ONLINE, GOOGLE_MEET, TEAMS, ZOOM, giá trị lạ…
        };

        // Họp trực tiếp bắt buộc có địa điểm; họp online thì bỏ địa điểm, giữ link (rule #17).
        if (platform == MeetingPlatform.InPerson && string.IsNullOrWhiteSpace(request.Location))
            throw new ArgumentException("Họp trực tiếp phải nhập địa điểm.");

        return platform;
    }

    /// <summary>
    /// Sửa buổi họp. Trước đây chỉ có tạo: Staff đặt nhầm giờ hay dán sai link Meet là **kẹt**,
    /// chỉ còn cách tạo buổi họp thứ hai — hội đồng nhìn vào thấy hai lịch, không biết theo cái nào.
    ///
    /// Rule #17 cho đổi lịch **bất kỳ lúc nào**, nên không khoá theo trạng thái. Riêng buổi họp đã
    /// diễn ra thì bỏ ràng buộc "phải ở tương lai" — sửa lại địa điểm/link ghi nhầm vẫn phải được.
    /// </summary>
    public async Task<MeetingDto> UpdateAsync(Guid meetingId, UpdateMeetingRequest request)
    {
        var meeting = await _review.Meetings
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        var isPast = meeting.Status != MeetingStatus.Scheduled;
        var platform = ValidateAndNormalize(request, requireFuture: !isPast);

        meeting.Title = request.Title;
        meeting.Platform = platform;
        meeting.MeetingLink = platform == MeetingPlatform.InPerson ? null : request.MeetingLink;
        meeting.Location = platform == MeetingPlatform.InPerson ? request.Location : null;
        meeting.ScheduledAt = request.ScheduledAt;
        meeting.DurationMinutes = request.DurationMinutes;
        meeting.Agenda = request.Agenda;

        await _review.SaveChangesAsync();
        return Map(meeting);
    }

    /// <summary>
    /// Xoá buổi họp đặt nhầm. **Chỉ khi chưa diễn ra** — họp rồi mà xoá là mất luôn điểm danh và
    /// mốc thời gian mà biên bản đang dựa vào.
    ///
    /// Xoá thì phải dọn: dòng điểm danh của buổi đó, và **gỡ slot đề tài** đang trỏ tới buổi họp
    /// (khoá ngoại không cascade — để nguyên là `SaveChanges` nổ, hoặc tệ hơn: slot trỏ vào buổi
    /// họp không còn tồn tại).
    /// </summary>
    public async Task DeleteAsync(Guid meetingId)
    {
        var meeting = await _review.Meetings
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        if (meeting.Status != MeetingStatus.Scheduled)
            throw new InvalidOperationException(
                $"Buổi họp đang ở trạng thái \"{StatusText.Vi(meeting.Status)}\" — chỉ xoá được buổi họp CHƯA DIỄN RA.");

        var attendances = await _review.MeetingAttendances
            .Where(a => a.MeetingId == meetingId).ToListAsync();
        // Đã có người điểm danh nghĩa là buổi họp đó có thật, không phải đặt nhầm.
        if (attendances.Any(a => a.ActuallyAttended != null))
            throw new InvalidOperationException(
                "Buổi họp đã có điểm danh — không xoá được. Nếu buổi họp không diễn ra thì sửa lại lịch.");
        _review.RemoveAttendancesRange(attendances);

        var slots = await _review.ProjectAssignments
            .Where(a => a.MeetingId == meetingId).ToListAsync();
        foreach (var s in slots)
        {
            s.MeetingId = null;
            s.SlotStartAt = null;
        }

        _review.RemoveMeetingsRange(new[] { meeting });
        await _review.SaveChangesAsync();
    }

    public async Task<MeetingDto> StartAsync(Guid meetingId)
    {
        var meeting = await _review.Meetings
            .FirstOrDefaultAsync(m => m.Id == meetingId)
            ?? throw new KeyNotFoundException($"Meeting {meetingId} not found.");

        if (meeting.Status != MeetingStatus.Scheduled)
            throw new InvalidOperationException($"Buổi họp đang ở trạng thái {StatusText.Vi(meeting.Status)} — chỉ buổi đã lên lịch mới bắt đầu được.");

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
            throw new InvalidOperationException($"Buổi họp đang ở trạng thái {StatusText.Vi(meeting.Status)} — chỉ buổi đang diễn ra mới kết thúc được.");

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
        // Bản ghi cũ còn GOOGLE_MEET/TEAMS/ZOOM — quy về ONLINE khi đọc, khỏi phải migration.
        Platform = m.Platform == MeetingPlatform.InPerson ? MeetingPlatform.InPerson : MeetingPlatform.Online,
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
