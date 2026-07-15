namespace FURPMS.Application.DTOs.ReviewRounds;

public class CloseRoundRequest
{
    public string Result { get; set; } = null!;  // APPROVED | REJECTED | REVISION_REQUIRED
    public Guid? ProposalProjectId { get; set; }  // Phase B: round nhiều đề tài → chỉ rõ project khi chốt
}
