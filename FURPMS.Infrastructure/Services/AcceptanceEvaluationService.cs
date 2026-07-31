using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class AcceptanceEvaluationService : IAcceptanceEvaluationService
{
    private readonly IReviewRepository _review;

    public AcceptanceEvaluationService(IReviewRepository review) => _review = review;

    public async Task<IReadOnlyList<AcceptanceEvaluationDto>> GetByCouncilAsync(Guid councilId)
    {
        return await _review.AcceptanceEvaluations
            .Include(e => e.EvaluatorMember).ThenInclude(m => m.User)
            .Where(e => e.CouncilId == councilId)
            .Select(e => ToDto(e))
            .ToListAsync();
    }

    public async Task<AcceptanceEvaluationDto> SubmitAsync(Guid councilId, Guid userId, SubmitAcceptanceRequest request)
    {
        var validResults = new[] { EvaluationResult.Pass, EvaluationResult.Fail };
        if (!validResults.Contains(request.Result))
            throw new ArgumentException("Result must be PASS or FAIL.");

        if (request.Result == EvaluationResult.Fail && string.IsNullOrWhiteSpace(request.FailReason))
            throw new ArgumentException("FailReason is required when result is FAIL.");

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId)
            ?? throw new KeyNotFoundException("You are not a member of this council.");

        var existing = await _review.AcceptanceEvaluations
            .FirstOrDefaultAsync(e => e.CouncilId == councilId && e.EvaluatorMemberId == member.Id);

        if (existing != null)
            throw new InvalidOperationException("You have already submitted an acceptance evaluation for this council.");

        var eval = new AcceptanceEvaluation
        {
            CouncilId = councilId,
            EvaluatorMemberId = member.Id,
            Result = request.Result,
            FailReason = request.Result == EvaluationResult.Pass ? null : request.FailReason,
            SubmittedAt = DateTime.UtcNow,
            IsValidBallot = true,
        };

        await _review.AddAcceptanceEvaluationAsync(eval);
        await _review.SaveChangesAsync();

        eval = await _review.AcceptanceEvaluations
            .Include(e => e.EvaluatorMember).ThenInclude(m => m.User)
            .FirstAsync(e => e.Id == eval.Id);

        return ToDto(eval);
    }

    private static AcceptanceEvaluationDto ToDto(AcceptanceEvaluation e) => new()
    {
        Id = e.Id,
        CouncilId = e.CouncilId,
        EvaluatorMemberId = e.EvaluatorMemberId,
        EvaluatorName = e.EvaluatorMember?.User?.FullName,
        Result = e.Result,
        FailReason = e.FailReason,
        SubmittedAt = e.SubmittedAt,
        IsValidBallot = e.IsValidBallot,
    };
}
