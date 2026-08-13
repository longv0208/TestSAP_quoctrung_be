namespace FURPMS.Application.DTOs.Councils;

/// <summary>
/// Một hội đồng nhìn từ màn quản lý của Phòng QLKH.
///
/// <para>
/// <b>Vì sao gom nhiều thứ vào một DTO.</b> Việc chuyên viên thực sự làm với danh sách này là
/// <i>"hội đồng nào còn thiếu gì để gửi được thư mời"</i>. Nếu chỉ trả tên + trạng thái thì họ phải
/// mở từng hội đồng ra đếm thành viên, xem có lịch chưa, xem ai đã xác nhận — mười hội đồng là mười
/// lần mở ra đóng vào. Trả sẵn số liệu tóm tắt thì nhìn bảng là biết phải làm gì.
/// </para>
/// </summary>
public class CouncilListItemDto
{
    public Guid Id { get; set; }
    public string CouncilType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? EstablishmentDecisionNo { get; set; }
    public DateOnly? EstablishedAt { get; set; }
    public DateOnly? MeetingDeadline { get; set; }

    // ── Ngữ cảnh: hội đồng này thuộc vòng nào, đợt nào, lĩnh vực nào ────────
    public Guid? RoundId { get; set; }
    public string? RoundType { get; set; }
    public string? RoundName { get; set; }
    public string? CycleName { get; set; }
    public string? TrackName { get; set; }

    // ── Nhân sự ─────────────────────────────────────────────────────────────
    public int MemberCount { get; set; }
    public int MinMembersRequired { get; set; }
    public bool HasChair { get; set; }
    public bool HasSecretary { get; set; }
    public string? ChairName { get; set; }
    public string? SecretaryName { get; set; }

    /// <summary>Đã bấm "Gửi thư mời" cho bao nhiêu người.</summary>
    public int InvitedCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int DeclinedCount { get; set; }

    // ── Công việc ───────────────────────────────────────────────────────────
    public int ProjectCount { get; set; }
    public int MeetingCount { get; set; }
    public DateTime? NextMeetingAt { get; set; }
    public string? NextMeetingLocation { get; set; }

    /// <summary>Số đề tài đã có biên bản CHỐT — để biết hội đồng chạy tới đâu.</summary>
    public int FinalizedProjectCount { get; set; }

    /// <summary>
    /// Còn thiếu gì để gửi được thư mời (rule #17: đủ thành viên + ngày giờ + địa điểm/link).
    /// Rỗng = sẵn sàng.
    /// <para>
    /// Trả <b>danh sách việc còn thiếu</b> chứ không phải một cờ true/false: chuyên viên cần biết
    /// <i>thiếu cái gì</i> để đi làm, chứ "chưa sẵn sàng" thì họ vẫn phải tự đi dò.
    /// </para>
    /// </summary>
    public List<string> MissingForInvitation { get; set; } = [];

    /// <summary>Đã gửi thư mời chưa — gửi rồi thì nút đổi thành "Gửi lại cho người chưa trả lời".</summary>
    public bool InvitationsSent { get; set; }
}

/// <summary>Bộ lọc cho danh sách hội đồng.</summary>
public class CouncilQueryParams
{
    /// <summary>Tìm theo số quyết định, tên chủ tịch/thư ký, tên đợt, tên lĩnh vực.</summary>
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? CouncilType { get; set; }
    public int? CycleId { get; set; }

    /// <summary>Chỉ lấy hội đồng <b>chưa gửi được thư mời</b> — đây là việc tồn đọng của chuyên viên.</summary>
    public bool? NotReadyOnly { get; set; }
}

/// <summary>Sửa thông tin hành chính của hội đồng. Không đụng tới thành viên hay đề tài.</summary>
public class UpdateCouncilRequest
{
    public string? EstablishmentDecisionNo { get; set; }
    public DateOnly? EstablishedAt { get; set; }
    public DateOnly? MeetingDeadline { get; set; }
    public int? MinMembersRequired { get; set; }
    public int? MaxMembersAllowed { get; set; }
    public string? Status { get; set; }
}

/// <summary>Đổi vai trò một thành viên trong hội đồng (Chủ tịch / Thư ký / Phản biện / Uỷ viên).</summary>
public class UpdateCouncilMemberRequest
{
    public string MemberRole { get; set; } = null!;
}
