using FURPMS.Application.Common;
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

    public async Task<IReadOnlyList<AcceptanceEvaluationDto>> GetByCouncilAsync(Guid councilId, Guid requesterId, bool isStaffOrAdmin)
    {
        // Không để lộ phiếu cho người ngoài: chỉ Staff/Admin hoặc thành viên chính hội đồng đó.
        if (!isStaffOrAdmin)
        {
            var isMember = await _review.CouncilMembers
                .AnyAsync(m => m.CouncilId == councilId && m.UserId == requesterId);
            if (!isMember)
                throw new ForbiddenException("Bạn không thuộc hội đồng này.");
        }

        return await _review.AcceptanceEvaluations
            .Include(e => e.EvaluatorMember).ThenInclude(m => m.User)
            .Where(e => e.CouncilId == councilId)
            .Select(e => ToDto(e))
            .ToListAsync();
    }

    // Phiếu của CHÍNH người gọi — form chấm nghiệm thu của reviewer dùng cái này (trước đây FE gọi
    // GetByCouncil trả MẢNG nhưng dùng như 1 object → seed sai; và endpoint đó chặn Admin/Staff nên
    // reviewer bị 403, không chấm được).
    public async Task<AcceptanceEvaluationDto?> GetMyAsync(Guid councilId, Guid projectId, Guid userId)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("Cần chỉ rõ đề tài cần xem phiếu nghiệm thu (projectId).");

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId);
        if (member == null) return null;

        var eval = await _review.AcceptanceEvaluations
            .Include(e => e.EvaluatorMember).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(e => e.CouncilId == councilId
                                      && e.ProjectId == projectId
                                      && e.EvaluatorMemberId == member.Id);

        return eval == null ? null : ToDto(eval);
    }

    public async Task<AcceptanceEvaluationDto> SubmitAsync(Guid councilId, Guid userId, SubmitAcceptanceRequest request)
    {
        var validResults = new[] { EvaluationResult.Pass, EvaluationResult.Fail };
        if (!validResults.Contains(request.Result))
            throw new ArgumentException("Kết quả nghiệm thu chỉ nhận Đạt (PASS) hoặc Không đạt (FAIL).");

        if (request.Result == EvaluationResult.Fail && string.IsNullOrWhiteSpace(request.FailReason))
            throw new ArgumentException("Phiếu Không đạt phải ghi rõ lý do.");

        if (request.ProjectId == Guid.Empty)
            throw new ArgumentException("Cần chỉ rõ đề tài cần chấm nghiệm thu (projectId).");

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId)
            ?? throw new KeyNotFoundException("Bạn không thuộc hội đồng này.");
        if (member.Status != CouncilMemberStatus.Confirmed)
            throw new InvalidOperationException(
                "Bạn chưa xác nhận tham gia hội đồng — hãy chấp nhận lời mời trước khi chấm nghiệm thu.");

        var isAssignedProject = await _review.ProjectAssignments
            .AnyAsync(a => a.CouncilId == councilId && a.ProjectId == request.ProjectId);
        if (!isAssignedProject)
            throw new InvalidOperationException(
                "Đề tài không thuộc phạm vi của hội đồng này — hãy mở đề tài được giao từ danh sách chấm.");

        // Biên bản đã được Chủ tịch chốt (rule #12) → khoá, không sửa phiếu nữa.
        var finalized = await _review.Decisions
            .AnyAsync(d => d.CouncilId == councilId
                           && d.ProjectId == request.ProjectId
                           && d.FinalizedAt != null);
        if (finalized)
            throw new InvalidOperationException("Biên bản đã được Chủ tịch chốt — không thể sửa phiếu nghiệm thu.");

        var existing = await _review.AcceptanceEvaluations
            .FirstOrDefaultAsync(e => e.CouncilId == councilId
                                      && e.ProjectId == request.ProjectId
                                      && e.EvaluatorMemberId == member.Id);

        // Cho SỬA phiếu của mình khi biên bản chưa chốt (UI vẫn ghi "Cập nhật đánh giá" — trước đây
        // BE ném 409 "already submitted" nên bấm là lỗi).
        if (existing != null)
        {
            existing.Result = request.Result;
            existing.FailReason = request.Result == EvaluationResult.Pass ? null : request.FailReason;
            existing.SubmittedAt = DateTime.UtcNow;
            await _review.SaveChangesAsync();

            var updated = await _review.AcceptanceEvaluations
                .Include(e => e.EvaluatorMember).ThenInclude(m => m.User)
                .FirstAsync(e => e.Id == existing.Id);
            return ToDto(updated);
        }

        var eval = new AcceptanceEvaluation
        {
            CouncilId = councilId,
            ProjectId = request.ProjectId,
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
        ProjectId = e.ProjectId,
        EvaluatorMemberId = e.EvaluatorMemberId,
        EvaluatorName = e.EvaluatorMember?.User?.FullName,
        Result = e.Result,
        FailReason = e.FailReason,
        SubmittedAt = e.SubmittedAt,
        IsValidBallot = e.IsValidBallot,
    };
}
