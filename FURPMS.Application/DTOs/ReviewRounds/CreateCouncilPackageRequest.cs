namespace FURPMS.Application.DTOs.ReviewRounds;

// Tạo hội đồng TRỌN GÓI cho 1 vòng: chọn nhóm đề tài + gán đủ thành viên trong 1 lần
// (thay vì phải add-member từng người như cũ) — theo yêu cầu redesign.
public class CreateCouncilPackageRequest
{
    public string? CouncilType { get; set; }
    public List<Guid> ProjectIds { get; set; } = new();
    public List<CouncilPackageMemberRequest> Members { get; set; } = new();
}

public class CouncilPackageMemberRequest
{
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = "Member";
    public bool IsExternal { get; set; }
}
