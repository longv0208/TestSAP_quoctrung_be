namespace FURPMS.Application.DTOs.ReviewRounds;

// Tạo vòng chấm ở CẤP TRACK (không qua proposalId) — dùng cho màn Hội đồng & Chấm.
public class CreateTrackRoundRequest
{
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public int? RubricTemplateId { get; set; }
    public Guid? PrerequisiteRoundId { get; set; }

    // Rỗng/null → tự gom mọi đề tài SUBMITTED/REVISION_REQUIRED của track chưa vào vòng này.
    public List<Guid>? ProjectIds { get; set; }
}
