using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Users;

/// <summary>
/// Lĩnh vực chuyên môn của một người — bảng nối người ↔ <see cref="ResearchTrack"/>.
///
/// <para><b>Vì sao cần (26/08):</b> QĐ543 Điều 8.2 đòi hội đồng gồm *"nhà khoa học, giảng viên
/// <b>có chuyên môn trong lĩnh vực</b>"*. Trước đó hệ thống <b>không có bảng nào nối người với lĩnh
/// vực</b>: chuyên môn chỉ là hai ô text tự do trong hồ sơ khoa học, dùng để in ra Word. Khi gán ủy
/// viên, hệ thống chỉ kiểm xung đột lợi ích, trùng tên và số lượng — không hề biết người đó có làm
/// đúng ngành hay không.</para>
///
/// <para>Đây là <b>dữ liệu khai báo</b>, do Quản trị/Phòng QLKH nhập, không phải suy đoán. Gọi nó là
/// "gợi ý AI" thì vừa sai vừa mất điểm — nó là một phép nối bảng.</para>
/// </summary>
public class UserResearchTrack
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int TrackId { get; set; }

    /// <summary>Ghi chú tự do — ví dụ hướng hẹp trong lĩnh vực, hoặc căn cứ công nhận chuyên môn.</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ResearchTrack Track { get; set; } = null!;
}
