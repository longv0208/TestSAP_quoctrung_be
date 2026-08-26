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

    /// <summary>Đồng ý gán người không khai đúng lĩnh vực (QĐ543 Điều 8.2) — xem <see cref="ExpertiseNote"/>.</summary>
    public bool AcceptWithoutExpertise { get; set; }

    /// <summary>Lý do gán người ngoài lĩnh vực — bắt buộc khi <see cref="AcceptWithoutExpertise"/> = true.</summary>
    public string? ExpertiseNote { get; set; }
}
