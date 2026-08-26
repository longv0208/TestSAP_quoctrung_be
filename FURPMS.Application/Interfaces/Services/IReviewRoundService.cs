using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.DTOs.ReviewRounds;

namespace FURPMS.Application.Interfaces.Services;

public interface IReviewRoundService
{
    Task<IEnumerable<ReviewRoundResponse>> GetProposalRoundsAsync(Guid proposalId);
    Task<ReviewRoundResponse> CreateRoundAsync(Guid proposalId, CreateReviewRoundRequest request, Guid createdBy);
    Task<ReviewRoundResponse> OpenRoundAsync(Guid roundId);
    Task<ReviewRoundResponse> CloseRoundAsync(Guid roundId, CloseRoundRequest request);
    Task<IEnumerable<CouncilMemberResponse>> GetRoundMembersAsync(Guid roundId);
    Task<CouncilMemberResponse> AddRoundMemberAsync(Guid roundId, AddRoundMemberRequest request, Guid addedBy);
    Task RemoveRoundMemberAsync(Guid roundId, Guid memberId);

    /// <summary>
    /// Đặt/dời hạn chấm của vòng. Dời hạn đã có ⇒ ghi thêm một dòng <c>deadline_extension</c>
    /// (rule #19), KHÔNG ghi đè ngày gốc.
    /// </summary>
    Task<ReviewRoundResponse> SetRoundDeadlineAsync(Guid roundId, SetRoundDeadlineRequest request, Guid actorId);

    // Rule #1: PI nộp lại bản REVISION → mở lại hội đồng đã chốt "yêu cầu chỉnh sửa" để chấm lại,
    // GIỮ điểm cũ. No-op nếu đề tài không có biên bản REVISION nào đã khoá.
    Task ReopenAfterResubmitAsync(Guid projectId);
}
