namespace FURPMS.Application.DTOs.Councils;

public class AddCouncilMemberRequest
{
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = null!;
    public bool IsExternal { get; set; }

    /// <summary>
    /// Đồng ý gán người <b>không khai đúng lĩnh vực</b> của đề tài (QĐ543 Điều 8.2).
    ///
    /// <para>Không có cờ này thì bị chặn 400. Có cờ mà thiếu <see cref="ExpertiseNote"/> cũng chặn:
    /// mục đích là buộc giải trình, không phải dựng thêm một ô tick cho người ta bấm qua.</para>
    /// </summary>
    public bool AcceptWithoutExpertise { get; set; }

    /// <summary>Lý do gán người ngoài lĩnh vực — ghi vào sổ quyết định của đề tài.</summary>
    public string? ExpertiseNote { get; set; }
}
