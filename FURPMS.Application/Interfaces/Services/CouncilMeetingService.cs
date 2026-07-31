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

        return meetings.Select(m => new MeetingListDto
        {
            Id = m.Id,
            CouncilId = m.CouncilId,
            Title = m.Title,
            Platform = m.Platform,
            MeetingLink = m.MeetingLink,
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
        });
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

        var meeting = new CouncilMeeting
        {
            CouncilId = councilId,
            Title = request.Title,
            Platform = platform,
            MeetingLink = request.MeetingLink,
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

    private static MeetingDto Map(CouncilMeeting m) => new()
    {
        Id = m.Id,
        CouncilId = m.CouncilId,
        Title = m.Title,
        Platform = m.Platform,
        MeetingLink = m.MeetingLink,
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
