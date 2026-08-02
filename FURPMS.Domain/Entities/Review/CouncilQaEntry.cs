namespace FURPMS.Domain.Entities.Review;

// BM04/BM12 mục II.1 — cách ghi biên bản dạng Hỏi–Đáp (Q&A): Thư ký ghi từng lượt
// "hội đồng hỏi → chủ nhiệm trả lời". Thuộc 1 biên bản (CouncilDecision), khóa cùng biên bản.
public class CouncilQaEntry
{
    public int Id { get; set; }
    public int DecisionId { get; set; }
    public string? AskedBy { get; set; }        // tên/vai người hỏi (Thư ký gõ tự do)
    public string Question { get; set; } = null!;
    public string? Answer { get; set; }
    public int Order { get; set; }

    public CouncilDecision Decision { get; set; } = null!;
}
