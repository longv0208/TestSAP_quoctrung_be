using FURPMS.Application.DTOs.ReviewRounds;

namespace FURPMS.Application.Interfaces.Services;

// Màn "Hội đồng & Chấm" cấp Lĩnh vực (Track) — thay cho phân công rải rác theo từng đề xuất.
public interface IReviewBoardService
{
    Task<ReviewBoardDto> GetReviewBoardAsync(int cycleId, int trackId);
    Task<ReviewRoundResponse> CreateRoundForTrackAsync(int cycleId, int trackId, CreateTrackRoundRequest request);
    Task DeleteRoundAsync(Guid roundId);
    Task AddProjectToRoundAsync(Guid roundId, Guid projectId);
    Task RemoveProjectFromRoundAsync(Guid roundId, Guid projectId);
    Task<ReviewBoardCouncilDto> CreateCouncilPackageAsync(Guid roundId, CreateCouncilPackageRequest request, Guid createdBy);
}
