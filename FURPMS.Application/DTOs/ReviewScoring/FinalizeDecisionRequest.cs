namespace FURPMS.Application.DTOs.ReviewScoring;

public class FinalizeDecisionRequest
{
    public Guid? ProjectId { get; set; }         // Phase B: council nhiều đề tài → chỉ rõ (1 đề tài thì tự suy)
    public string Result { get; set; } = null!;  // APPROVED / REJECTED / REVISION_REQUIRED
    public string? CouncilComments { get; set; }
    public string? Recommendations { get; set; }
    public Guid? ChairUserId { get; set; }
    public Guid? SecretaryUserId { get; set; }
}
