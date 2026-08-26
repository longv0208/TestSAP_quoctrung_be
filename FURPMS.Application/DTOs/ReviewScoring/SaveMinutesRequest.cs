namespace FURPMS.Application.DTOs.ReviewScoring;

// Thư ký soạn/sửa biên bản (nháp). Kết quả đề xuất + nhận xét — CHƯA chốt status đề tài.
// Chỉ khi Chủ tịch duyệt (minutes/approve) mới khoá + cập nhật status proposal.
public class SaveMinutesRequest
{
    public Guid? ProjectId { get; set; }         // Phase B: council nhiều đề tài → chỉ rõ (1 đề tài thì tự suy)
    public string Result { get; set; } = null!;  // APPROVED / REJECTED / REVISION_REQUIRED (đề xuất)
    public string? CouncilComments { get; set; }   // nội dung biên bản (cách 2: viết tự do)
    public string? Recommendations { get; set; }
    public List<QaEntryDto>? QaEntries { get; set; }  // cách 1: hỏi–đáp (thay toàn bộ mỗi lần lưu)
    public List<MemberOpinionDto>? MemberOpinions { get; set; }  // II.1: ý kiến TV (thay toàn bộ)

    /// <summary>
    /// Lý do kết luận lệch với điểm trung bình. <b>Bắt buộc</b> khi điểm thấp mà kết luận Đạt,
    /// hoặc điểm cao mà kết luận Không đạt — thiếu thì 400 kèm câu giải thích.
    /// </summary>
    public string? ResultJustification { get; set; }
}
