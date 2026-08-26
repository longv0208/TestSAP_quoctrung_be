using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Projects;

/// <summary>
/// Sổ đăng ký các <b>quyết định</b> đã ra với một đề tài — trả lời nửa sau yêu cầu số (2) của hội
/// đồng bảo vệ lần 2: *"lưu trữ lại các quyết định liên quan đến đề tài"*.
///
/// <para><b>Vì sao không dùng <c>AuditLog</c>:</b> bảng đó là nhật ký <i>kỹ thuật</i> — có
/// <c>OldValues</c>/<c>NewValues</c>/<c>IpAddress</c>/<c>UserAgent</c> để truy "ai sửa cột nào lúc
/// mấy giờ", nhưng <b>không neo vào đề tài</b>, không có loại quyết định, không có số văn bản. Mở nó
/// ra trước hội đồng thì thấy một danh sách thao tác CRUD, không phải hồ sơ ra quyết định.</para>
///
/// <para><b>Đây là sổ MỎNG, trỏ ngược về bản gốc.</b> Nội dung đầy đủ vẫn nằm ở
/// <c>CouncilDecision</c> / <c>ProjectRound</c> / <c>AmendmentRequest</c>…; bảng này chỉ giữ đủ để
/// <i>liệt kê theo thứ tự thời gian</i> và <i>mở đúng bản gốc</i>. Chép lại nội dung là tự tạo ra
/// hai nguồn sự thật, sửa một bên thì bên kia sai.</para>
///
/// <para>⚠️ Rule #12 vẫn nguyên: <see cref="Result"/> luôn là <b>bản chép lại</b> kết luận của người
/// có thẩm quyền (Chủ tịch hội đồng, Phòng QLKH…), hệ thống không tự sinh ra kết luận nào.</para>
/// </summary>
public class ProjectDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Điểm khác cốt lõi so với <c>AuditLog</c>: mọi dòng đều neo vào một đề tài.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Loại quyết định — xem <c>DecisionTypes</c> bên Application.</summary>
    public string DecisionType { get; set; } = null!;

    /// <summary>Kết luận, <b>chép lại</b> từ bản gốc: APPROVED / REJECTED / REVISION_REQUIRED / PASSED / FAILED…</summary>
    public string? Result { get; set; }

    /// <summary>Một câu tiếng Việt đọc là hiểu, dùng làm dòng tóm tắt trong hồ sơ.</summary>
    public string Summary { get; set; } = null!;

    /// <summary>Lý do/căn cứ nếu người quyết định có nêu.</summary>
    public string? Reason { get; set; }

    /// <summary>Số hiệu biểu mẫu/văn bản kèm theo (BM04, BM12, BM13…) nếu có.</summary>
    public string? DocumentNo { get; set; }

    /// <summary>Bảng gốc chứa quyết định này — để giao diện mở đúng chỗ.</summary>
    public string SourceEntityType { get; set; } = null!;
    public string SourceEntityId { get; set; } = null!;

    public Guid? DecidedBy { get; set; }

    /// <summary>
    /// Chức danh của người quyết định <b>tại thời điểm chốt</b>, chép cứng vào đây.
    ///
    /// <para>Không suy ra từ <c>user_roles</c> lúc đọc: người ta đổi vai, nghỉ việc, bị gỡ quyền —
    /// hồ sơ vài năm trước phải giữ nguyên "Chủ tịch hội đồng" chứ không được biến thành vai hiện
    /// tại của người đó.</para>
    /// </summary>
    public string? DecidedByRole { get; set; }

    /// <summary>Thời điểm quyết định <b>thực sự</b> được ra (có thể sớm hơn <see cref="CreatedAt"/> khi backfill).</summary>
    public DateTime DecidedAt { get; set; }

    /// <summary>Thời điểm dòng này được ghi vào sổ.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User? DecidedByUser { get; set; }
}
