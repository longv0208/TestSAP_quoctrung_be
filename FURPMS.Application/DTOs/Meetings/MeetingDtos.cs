namespace FURPMS.Application.DTOs.Meetings;

public class MeetingDto
{
    public Guid Id { get; set; }
    public Guid CouncilId { get; set; }
    public string? Title { get; set; }
    public string Platform { get; set; } = "IN_PERSON";
    public string? MeetingLink { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? Agenda { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public string Status { get; set; } = "SCHEDULED";
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeetingListDto : MeetingDto
{
    public Guid? ProposalId { get; set; }
    public string? ProposalTitle { get; set; }
    public string? RoundType { get; set; }
    public int? RoundNumber { get; set; }
}

public class ScheduleMeetingRequest
{
    public string? Title { get; set; }
    public string Platform { get; set; } = "IN_PERSON";
    public string? MeetingLink { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public string? Agenda { get; set; }
}
