namespace FURPMS.Application.DTOs.Councils;

public class MyMembershipDto
{
    public Guid MemberId { get; set; }
    public Guid CouncilId { get; set; }
    public Guid? RoundId { get; set; }
    public string RoundType { get; set; } = null!;
    public string RoundStatus { get; set; } = null!;
    public string MemberRole { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid ProposalId { get; set; }
    /// <summary>id của PROJECT — dùng cho SaveMinutes (biên bản neo theo project). Khác ProposalId (bản đề cương).</summary>
    public Guid ProjectId { get; set; }
    public string ProposalTitleVI { get; set; } = null!;
    public string ProposalStatus { get; set; } = null!;
    // Enrich (rule tuần 10) — cho reviewer xem nhiều thông tin + sắp xếp.
    public string? PiName { get; set; }
    public string? TrackName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? NextMeetingAt { get; set; }
}
