namespace FURPMS.Domain.Entities.Review;

public class CouncilMeeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CouncilId { get; set; }
    public string? Title { get; set; }
    public string Platform { get; set; } = "IN_PERSON";
    public string? MeetingLink { get; set; }
    public string? Location { get; set; }              // địa điểm khi họp trực tiếp (IN_PERSON)
    public string? ExternalMeetingId { get; set; }
    public string? CalendarEventId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public string? Agenda { get; set; }
    public string? AgendaDocuments { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public string Status { get; set; } = "SCHEDULED";
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReviewCouncil Council { get; set; } = null!;
    public ICollection<MeetingAttendance> Attendances { get; set; } = new List<MeetingAttendance>();
}
