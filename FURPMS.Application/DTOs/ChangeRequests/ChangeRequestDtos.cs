namespace FURPMS.Application.DTOs.ChangeRequests;

// DTO khớp CHÍNH XÁC FE (Fefurpmsv0/src/types/changeRequest.ts):
// type trả về dạng TÊN ('ExtendTime'…), status PascalCase ('Pending'…).
public class ChangeRequestDto
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }          // id bản đề cương hiện hành (FE điều hướng)
    public string ProposalTitleVI { get; set; } = null!;
    public string Type { get; set; } = null!;     // ExtendTime / ContentChange / PersonnelChange / BudgetChange / Suspend
    public string Description { get; set; } = null!;
    public string? NewValue { get; set; }
    public string Status { get; set; } = null!;   // Pending / Approved / Rejected
    public string? AdminNote { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateChangeRequestRequest
{
    public int Type { get; set; }                 // 1..5 (CHANGE_TYPE bên FE)
    public string Description { get; set; } = null!;
    public string? NewValue { get; set; }
}

public class ReviewChangeRequestRequest
{
    public bool Approved { get; set; }
    public string? AdminNote { get; set; }
}
