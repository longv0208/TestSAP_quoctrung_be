namespace FURPMS.Application.DTOs.Councils;

public class AssignProjectToCouncilRequest
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Đồng ý gán đề tài này cho hội đồng dù có ủy viên <b>không khai đúng lĩnh vực</b> của đề tài
    /// (QĐ543 Điều 8.2) — xem <see cref="ExpertiseNote"/>.
    /// </summary>
    public bool AcceptWithoutExpertise { get; set; }

    /// <summary>Lý do — bắt buộc khi <see cref="AcceptWithoutExpertise"/> = true.</summary>
    public string? ExpertiseNote { get; set; }
}
