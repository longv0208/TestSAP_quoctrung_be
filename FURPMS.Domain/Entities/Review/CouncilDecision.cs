using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Review;

public class CouncilDecision
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }    // Phase B: Chủ tịch chốt TỪNG đề tài trong phiên
    public int TotalMembers { get; set; }
    public int AttendingMembers { get; set; }
    public int ValidBallots { get; set; }
    public int InvalidBallots { get; set; }
    public decimal? AverageScore { get; set; }
    public string Result { get; set; } = null!;
    public string? CouncilComments { get; set; }
    public string? Recommendations { get; set; }
    public Guid? ChairUserId { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public DateTime? FinalizedAt { get; set; }

    // ── Chủ tịch trả biên bản cho Thư ký sửa ────────────────────────────────
    // QĐ543 Điều 8.3.c / 12.3.c: Thư ký **ghi** biên bản, các thành viên **thông qua** — quy định
    // KHÔNG cho Chủ tịch tự sửa chữ của Thư ký. Nhưng trước 14/08 hệ thống chỉ có đúng hai đường:
    // duyệt (khoá luôn) hoặc không làm gì. Chủ tịch thấy sai một chỗ thì phải nhắn tin/gọi điện
    // ngoài hệ thống — không ai biết đã yêu cầu sửa gì, và biên bản không có dấu vết.
    //
    // Ghi chú lưu ở ĐÂY chứ không chỉ gửi thông báo: Thư ký mở màn soạn ra là thấy ngay cần sửa
    // gì, không phải lục lại chuông báo.

    /// <summary>
    /// Lý do hội đồng kết luận <b>lệch</b> với điểm trung bình (thêm 25/08).
    ///
    /// <para>Trước đây <see cref="AverageScore"/> và <see cref="Result"/> là hai đại lượng độc lập
    /// tuyệt đối — gán ở hai dòng liền kề mà không một phép so sánh nào. Đề tài trung bình 35/100
    /// vẫn chốt được "Đạt", chuyển sang HOÀN THÀNH và mở khoá giải ngân đợt cuối, không một tiếng
    /// cảnh báo.</para>
    ///
    /// <para>Nay khi kết luận lệch ngưỡng, Thư ký <b>bắt buộc</b> ghi lý do vào đây. Hệ thống vẫn
    /// KHÔNG tự kết luận thay hội đồng (rule #12) — chỉ đòi giải trình.</para>
    /// </summary>
    public string? ResultJustification { get; set; }

    /// <summary>Chủ tịch yêu cầu sửa gì. Null = chưa từng bị trả lại (hoặc Thư ký đã lưu bản mới).</summary>
    public string? RevisionRequestNote { get; set; }

    public DateTime? RevisionRequestedAt { get; set; }
    public Guid? RevisionRequestedBy { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Projects.Project Project { get; set; } = null!;
    public User? ChairUser { get; set; }
    public User? SecretaryUser { get; set; }
    public ICollection<CouncilQaEntry> QaEntries { get; set; } = new List<CouncilQaEntry>();
    public ICollection<CouncilMemberOpinion> MemberOpinions { get; set; } = new List<CouncilMemberOpinion>();
}
