namespace FURPMS.Application.DTOs.Proposals;

// Kết quả AI trích xuất từ file đề cương (Đường B). Thiếu field nào để null —
// PI review/sửa rồi nộp. AI lỗi/không cấu hình → Warning set, FE fallback nhập tay.
public class ExtractedProposalDto
{
    public string? TitleVi { get; set; }
    public string? TitleEn { get; set; }
    public string? AbstractVi { get; set; }
    public string? ResearchObjectives { get; set; }
    public string? Methodology { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? Urgency { get; set; }
    public string? Novelty { get; set; }
    public string? ApplicationPotential { get; set; }
    public string? TransferPotential { get; set; }
    public string? Facilities { get; set; }
    public int? DurationMonths { get; set; }
    public decimal? TotalBudget { get; set; }
    public string? Warning { get; set; }
}
