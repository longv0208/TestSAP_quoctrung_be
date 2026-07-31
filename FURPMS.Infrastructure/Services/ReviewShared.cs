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
