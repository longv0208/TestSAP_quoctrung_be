using FURPMS.Application.DTOs.ReviewScoring;

namespace FURPMS.Application.Interfaces.Services;

public interface IAcceptanceEvaluationService
{
    /// <summary>Tổng hợp phiếu của hội đồng. Chỉ Staff/Admin hoặc THÀNH VIÊN hội đồng (Thư ký lập biên bản cần xem).</summary>
    Task<IReadOnlyList<AcceptanceEvaluationDto>> GetByCouncilAsync(Guid councilId, Guid requesterId, bool isStaffOrAdmin);
    /// <summary>Phiếu nghiệm thu của CHÍNH người gọi (null nếu chưa chấm) — form của reviewer dùng cái này.</summary>
    Task<AcceptanceEvaluationDto?> GetMyAsync(Guid councilId, Guid projectId, Guid userId);
    Task<AcceptanceEvaluationDto> SubmitAsync(Guid councilId, Guid userId, SubmitAcceptanceRequest request);
}
