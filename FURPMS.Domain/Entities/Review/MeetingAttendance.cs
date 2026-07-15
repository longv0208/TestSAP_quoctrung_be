namespace FURPMS.Domain.Entities.Review;

public class MeetingAttendance
{
    public int Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid MemberId { get; set; }
    public string RsvpStatus { get; set; } = "PENDING";
    public DateTime? RsvpAt { get; set; }
    public bool? ActuallyAttended { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public string? AbsenceReason { get; set; }

    public CouncilMeeting Meeting { get; set; } = null!;
    public CouncilMember Member { get; set; } = null!;
}
