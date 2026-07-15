using FURPMS.Application.DTOs.AI;

namespace FURPMS.Application.Interfaces.Services;

public interface IAiSummaryService
{
    Task<AiSummaryDto?> GetAsync(Guid proposalId);
    Task<AiSummaryDto> GenerateAsync(Guid proposalId, Guid userId);
    Task<AiSummaryDto> UpdateAsync(Guid proposalId, string editedText, Guid userId);
}
