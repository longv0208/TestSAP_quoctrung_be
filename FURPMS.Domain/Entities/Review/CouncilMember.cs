using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Review;

public class CouncilMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CouncilId { get; set; }
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = null!;
    public bool IsExternal { get; set; }
    public DateTime? InvitationSentAt { get; set; }
    public string? InvitationToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }
    public string Status { get; set; } = "INVITED";

    /// <summary>
    /// Chuyên viên nào đã trả lời <b>THAY</b> thành viên này (xác nhận hộ / đánh dấu từ chối hộ).
    /// <c>null</c> = chính thành viên tự bấm.
    /// <para>
    /// Trước 14/08 không lưu gì cả: <c>ConfirmedAt</c> được đặt y như người đó tự xác nhận, nên
    /// về sau <b>không ai phân biệt được</b> thành viên thật sự đồng ý hay chuyên viên bấm hộ.
    /// Với hệ thống mà cả mục đích là giữ hồ sơ đối chiếu được thì đó là lỗ hổng thật.
    /// </para>
    /// </summary>
    public Guid? RespondedOnBehalfBy { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public User User { get; set; } = null!;
}
