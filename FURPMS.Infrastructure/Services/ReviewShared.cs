using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Logic dùng chung cho 3 service review (ReviewRound / Council / ReviewBoard) — gom về 1 chỗ để
// rule COI (#5) và mapping thành viên KHÔNG lệch giữa các đường (trước đây bị copy 3 bản).
internal static class ReviewShared
{
    public static bool IsAcceptance(string? roundType) =>
        string.Equals(roundType, "ACCEPTANCE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Nghiệm thu là giai đoạn sau thực hiện đề tài, không phải một tab chấm song song với xét duyệt.
    /// Toàn bộ đề tài đã đưa vào vòng REVIEW của lĩnh vực phải có kết quả cuối cùng; đề tài còn chờ/chỉnh sửa
    /// hoặc đề cương đã nộp nhưng chưa được đưa vào xử lý đều làm luồng trước chưa khép lại.
    /// </summary>
    public static async Task AssertAcceptanceTrackReadyAsync(
        IReviewRepository review, IProposalRepository proposals, int cycleTrackId)
    {
        var reviewRounds = await review.ReviewRounds
            .Where(r => r.CycleTrackId == cycleTrackId && r.RoundType == "REVIEW")
            .Select(r => new
            {
                r.Status,
                ProjectStatuses = r.ProjectRounds.Select(pr => pr.Status).ToList()
            })
            .ToListAsync();

        if (reviewRounds.Count == 0)
            throw new InvalidOperationException(
                "Lĩnh vực này chưa có vòng XÉT DUYỆT ĐỀ CƯƠNG — chưa thể tạo hoặc mở vòng NGHIỆM THU.");

        var hasUnfinishedReview = reviewRounds.Any(r =>
            r.Status == ReviewRoundStatus.Pending
            || r.Status == ReviewRoundStatus.Open
            || r.ProjectStatuses.Any(s => s != ReviewRoundStatus.Passed && s != ReviewRoundStatus.Failed));
        var hasUnprocessedProposal = await proposals.Query().IgnoreQueryFilters()
            .AnyAsync(p => p.IsCurrent
                           && !p.Project.IsDeleted
                           && p.Project.CycleTrackId == cycleTrackId
                           && (p.Status == ProposalStatus.Submitted
                               || p.Status == ProposalStatus.RevisionRequired));

        if (hasUnfinishedReview || hasUnprocessedProposal)
            throw new InvalidOperationException(
                "Vòng XÉT DUYỆT ĐỀ CƯƠNG chưa hoàn tất cho toàn bộ đề tài trong lĩnh vực — hãy chốt kết quả " +
                "Đạt/Không đạt của tất cả đề tài trước khi tạo hoặc mở vòng NGHIỆM THU.");
    }

    /// <summary>Chỉ hồ sơ đã qua REVIEW và đã được Staff duyệt báo cáo tổng kết mới đủ căn cứ đưa ra nghiệm thu.</summary>
    public static async Task AssertProjectEligibleForAcceptanceAsync(
        IReviewRepository review, IProposalRepository proposals, int cycleTrackId, Guid projectId)
    {
        var passedReview = await review.ProjectRounds.AnyAsync(pr =>
            pr.ProjectId == projectId
            && pr.Round.CycleTrackId == cycleTrackId
            && pr.Round.RoundType == "REVIEW"
            && pr.Status == ReviewRoundStatus.Passed);
        if (!passedReview)
            throw new InvalidOperationException(
                "Đề tài chưa Đạt vòng XÉT DUYỆT ĐỀ CƯƠNG — chưa thể đưa vào vòng NGHIỆM THU.");

        var dossierReady = await proposals.Projects.IgnoreQueryFilters().AnyAsync(p =>
            p.Id == projectId
            && p.CycleTrackId == cycleTrackId
            && !p.IsDeleted
            && p.Status == ProjectStatus.Acceptance
            && p.FinalReport != null
            && (p.FinalReport.Status == FinalReportStatus.Accepted
                || p.FinalReport.Status == FinalReportStatus.Archived));
        if (!dossierReady)
            throw new InvalidOperationException(
                "Hồ sơ nghiệm thu của đề tài chưa sẵn sàng — cần nộp báo cáo tổng kết và được Phòng QLKH duyệt trước khi đưa vào vòng NGHIỆM THU.");
    }

    public static async Task<List<Guid>> GetAcceptanceEligibleProjectIdsAsync(
        IReviewRepository review, IProposalRepository proposals, int cycleTrackId)
    {
        var passedIds = review.ProjectRounds
            .Where(pr => pr.Round.CycleTrackId == cycleTrackId
                         && pr.Round.RoundType == "REVIEW"
                         && pr.Status == ReviewRoundStatus.Passed)
            .Select(pr => pr.ProjectId);

        return await proposals.Projects.IgnoreQueryFilters()
            .Where(p => passedIds.Contains(p.Id)
                        && p.CycleTrackId == cycleTrackId
                        && !p.IsDeleted
                        && p.Status == ProjectStatus.Acceptance
                        && p.FinalReport != null
                        && (p.FinalReport.Status == FinalReportStatus.Accepted
                            || p.FinalReport.Status == FinalReportStatus.Archived))
            .Select(p => p.Id)
            .Distinct()
            .ToListAsync();
    }

    public static CouncilMemberResponse MapMember(CouncilMember m) => new()
    {
        Id = m.Id,
        CouncilId = m.CouncilId,
        UserId = m.UserId,
        ReviewerName = m.User?.FullName ?? string.Empty,
        ReviewerEmail = m.User?.Email ?? string.Empty,
        MemberRole = m.MemberRole,
        IsExternal = m.IsExternal,
        Status = m.Status,
        InvitationSentAt = m.InvitationSentAt,
        ConfirmedAt = m.ConfirmedAt,
        DeclinedAt = m.DeclinedAt
    };

    // Rule #5 (COI): PI/thành viên của BẤT KỲ đề tài trong nhóm KHÔNG được là ủy viên chấm nhóm đó.
    // 2 query cho cả tập (bất kể số user) → throw ArgumentException (=400) nếu vi phạm.
    public static async Task AssertNoCoiAsync(
        IProposalRepository proposals, IReadOnlyCollection<Guid> projectIds, IReadOnlyCollection<Guid> userIds)
    {
        if (projectIds.Count == 0 || userIds.Count == 0) return;
        const string msg = "Conflict of interest: PI/thành viên đề tài không được là ủy viên hội đồng chấm chính đề tài đó.";

        var isPi = await proposals.Projects.IgnoreQueryFilters()
            .AnyAsync(p => projectIds.Contains(p.Id) && userIds.Contains(p.PiUserId));
        if (isPi) throw new ArgumentException(msg);

        var isTeam = await proposals.ProjectMembers
            .AnyAsync(tm => projectIds.Contains(tm.ProjectId) && tm.UserId != null && userIds.Contains(tm.UserId.Value));
        if (isTeam) throw new ArgumentException(msg);
    }

    // DÙNG CHUNG round theo track: tái dùng round cùng dimension+type còn mở (PENDING/OPEN), hoặc tạo mới
    // (đánh số sequence/roundNumber tiếp theo trong track). Chỉ AddRoundAsync (KHÔNG SaveChanges) — caller lo lưu.
    public static async Task<ReviewRound> GetOrCreateOpenRoundAsync(
        IReviewRepository review, int cycleTrackId, string dimension, string roundType,
        int? rubricTemplateId, Guid? prerequisiteRoundId)
    {
        var round = await review.ReviewRounds.FirstOrDefaultAsync(r =>
            r.CycleTrackId == cycleTrackId && r.Dimension == dimension && r.RoundType == roundType
            && (r.Status == ReviewRoundStatus.Pending || r.Status == ReviewRoundStatus.Open));
        if (round != null) return round;

        var maxSeq = await review.ReviewRounds
            .Where(r => r.CycleTrackId == cycleTrackId)
            .MaxAsync(r => (int?)r.Sequence) ?? 0;
        var maxNum = await review.ReviewRounds
            .Where(r => r.CycleTrackId == cycleTrackId && r.Dimension == dimension)
            .MaxAsync(r => (int?)r.RoundNumber) ?? 0;

        round = new ReviewRound
        {
            Id = Guid.NewGuid(),
            CycleTrackId = cycleTrackId,
            RoundNumber = maxNum + 1,
            Sequence = maxSeq + 1,
            Dimension = dimension,
            RoundType = roundType,
            RubricTemplateId = rubricTemplateId,
            PrerequisiteRoundId = prerequisiteRoundId,
            Status = ReviewRoundStatus.Pending
        };
        await review.AddRoundAsync(round);
        return round;
    }
}
