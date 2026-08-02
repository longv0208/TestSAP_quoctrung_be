namespace FURPMS.Domain.Entities.Review;

// BM04/BM12 mục II.1 — "Ý kiến của các thành viên Hội đồng": mỗi thành viên 1 dòng,
// tách 2 cột Về chuyên môn / Về kinh phí. Thư ký ghi (prefill tên từ danh sách hội đồng).
// Thuộc 1 biên bản (CouncilDecision), khóa cùng biên bản.
public class CouncilMemberOpinion
{
    public int Id { get; set; }
    public int DecisionId { get; set; }
    public string MemberName { get; set; } = null!;   // tên (prefill từ roster) hoặc vai
    public string? AcademicComment { get; set; }       // Về chuyên môn
    public string? BudgetComment { get; set; }         // Về kinh phí
    public int Order { get; set; }

    public CouncilDecision Decision { get; set; } = null!;
}
