namespace FURPMS.Application.DTOs.ReviewScoring;

// Thư ký soạn/sửa biên bản (nháp). Kết quả đề xuất + nhận xét — CHƯA chốt status đề tài.
// Chỉ khi Chủ tịch duyệt (minutes/approve) mới khóa + cập nhật status proposal.
public class SaveMinutesRequest
{
    public Guid? ProjectId { get; set; }         // Phase B: council nhiều đề tài → chỉ rõ (1 đề tài thì tự suy)
    public string Result { get; set; } = null!;  // APPROVED / REJECTED / REVISION_REQUIRED (đề xuất)
    public string? CouncilComments { get; set; }   // nội dung biên bản
    public string? Recommendations { get; set; }
}
