using FURPMS.Application.DTOs.ReviewScoring;

namespace FURPMS.Application.Interfaces.Services;

public interface IReviewScoringService
{
    Task<IEnumerable<RubricTemplateDto>> GetRubricTemplatesAsync();
    Task<RubricTemplateDto> GetRubricTemplateByIdAsync(int templateId);
    Task<ReviewScoreDto> SubmitScoreAsync(Guid councilId, Guid userId, SubmitScoreRequest request);
    Task<ReviewScoreDto?> GetMyScoreAsync(Guid councilId, Guid userId);
    Task<IEnumerable<ReviewScoreDto>> GetCouncilScoresAsync(Guid councilId);
    Task<CouncilDecisionDto> FinalizeDecisionAsync(Guid councilId, FinalizeDecisionRequest request);
    // Workflow biên bản: Thư ký soạn nháp → Chủ tịch duyệt = khóa + cập nhật status đề tài.
    Task<CouncilDecisionDto> SaveMinutesAsync(Guid councilId, Guid secretaryUserId, SaveMinutesRequest request);
    Task<CouncilDecisionDto> ApproveMinutesAsync(Guid councilId, Guid chairUserId);
    Task<CouncilDecisionDto?> GetDecisionAsync(Guid councilId);
    /// <summary>BM12 mục 10.1 — kết quả bỏ phiếu chi tiết từng thành viên.</summary>
    Task<BallotTallyDto> GetBallotTallyAsync(Guid councilId, Guid? projectId);
}
