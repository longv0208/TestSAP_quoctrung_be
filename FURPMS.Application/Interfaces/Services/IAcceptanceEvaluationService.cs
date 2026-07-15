using FURPMS.Application.DTOs.ReviewScoring;

namespace FURPMS.Application.Interfaces.Services;

public interface IAcceptanceEvaluationService
{
    Task<IReadOnlyList<AcceptanceEvaluationDto>> GetByCouncilAsync(Guid councilId);
    Task<AcceptanceEvaluationDto> SubmitAsync(Guid councilId, Guid userId, SubmitAcceptanceRequest request);
}
