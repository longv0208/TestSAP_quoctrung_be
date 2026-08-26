using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Phase B (Review 2 điểm c): round thuộc CYCLE_TRACK, project tham gia qua ProjectRound
// (nhiều-nhiều). Route vẫn theo proposalId (FE giữ nguyên) — service resolve
// proposal → project → cycle_track. Nhiều project cùng track DÙNG CHUNG round.
public class ReviewRoundService : IReviewRoundService
{
    private readonly IReviewRepository _review;
    private readonly IProposalRepository _proposals;
    private readonly INotificationRepository _notifications;
    private readonly INotifier _notifier;
    private readonly IClock _clock;
    private readonly ISystemSettingService _settings;
    private readonly IDeadlineResolver _deadlines;
    private readonly ICycleRepository _cycles;

    public ReviewRoundService(
        IReviewRepository review,
        IProposalRepository proposals,
        INotificationRepository notifications,
        INotifier notifier,
        IClock clock,
        ISystemSettingService settings,
        IDeadlineResolver deadlines,
        ICycleRepository cycles)
    {
        _cycles = cycles;
        _review = review;
        _proposals = proposals;
        _notifications = notifications;
        _notifier = notifier;
        _clock = clock;
        _settings = settings;
        _deadlines = deadlines;
    }

    private async Task<(Guid projectId, int cycleTrackId)> ResolveProjectAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");
        return (proposal.ProjectId, proposal.Project.CycleTrackId);
    }

    public async Task<IEnumerable<ReviewRoundResponse>> GetProposalRoundsAsync(Guid proposalId)
    {
        var (projectId, _) = await ResolveProjectAsync(proposalId);

        var projectRounds = await _review.ProjectRounds
            .Include(pr => pr.Round)
            .Where(pr => pr.ProjectId == projectId)
            .OrderBy(pr => pr.Round.Sequence)
            .ToListAsync();

        var roundIds = projectRounds.Select(pr => pr.RoundId).ToList();

        // Council của từng round mà có gán project này (nhiều council song song / round).
        var councils = await _review.Query()
            .Where(c => c.RoundId != null && roundIds.Contains(c.RoundId.Value)
                        && c.ProjectAssignments.Any(a => a.ProjectId == projectId))
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .ToListAsync();

        var councilsByRound = councils
            .GroupBy(c => c.RoundId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return projectRounds.Select(pr =>
        {
            var council = councilsByRound.GetValueOrDefault(pr.RoundId);
            var response = MapToResponse(pr.Round, council?.Id);
            // Kết quả CỦA ĐỀ TÀI NÀY trong round (project_round) đè lên trạng thái chung.
            if (pr.Status != ReviewRoundStatus.Pending || pr.Result != null)
            {
                response.Status = pr.Status == ReviewRoundStatus.Pending ? response.Status : pr.Status;
                response.Result = pr.Result ?? response.Result;
            }
            if (council != null)
                response.Members = council.Members.Select(MapMember).ToList();
            return response;
        });
    }

    public async Task<ReviewRoundResponse> CreateRoundAsync(Guid proposalId, CreateReviewRoundRequest request, Guid createdBy)
    {
        // Rule #16: bỏ hẳn phương diện TÀI CHÍNH, chỉ còn 2 hội đồng.
        if (string.IsNullOrWhiteSpace(request.Dimension) || request.Dimension != ReviewRoundDimension.Science)
            throw new ArgumentException(
                "Phương diện chấm chỉ còn SCIENCE (khoa học). Phương diện TÀI CHÍNH đã bỏ theo " +
                "kết luận tuần 10 — báo cáo tiến độ giữa kỳ do Phòng QLKH duyệt trực tiếp, không lập hội đồng.");

        if (string.IsNullOrWhiteSpace(request.RoundType) ||
            !new[] { "REVIEW", "ACCEPTANCE" }.Contains(request.RoundType))
            throw new ArgumentException(
                "Loại vòng chỉ nhận REVIEW (Xét duyệt đề cương) hoặc ACCEPTANCE (Nghiệm thu) — " +
                "quy định chỉ có hai hội đồng này.");

        var (projectId, cycleTrackId) = await ResolveProjectAsync(proposalId);

        if (ReviewShared.IsAcceptance(request.RoundType))
        {
            await ReviewShared.AssertAcceptanceTrackReadyAsync(_review, _proposals, cycleTrackId);
            await ReviewShared.AssertProjectEligibleForAcceptanceAsync(
                _review, _proposals, cycleTrackId, projectId);
        }

        if (request.PrerequisiteRoundId.HasValue)
        {
            var prereq = await _review.GetRoundByIdAsync(request.PrerequisiteRoundId.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy vòng tiên quyết.");
            if (prereq.CycleTrackId != cycleTrackId)
                throw new ArgumentException("Vòng tiên quyết không thuộc lĩnh vực trong đợt này.");
        }

        // Vòng nào có vòng tiên quyết thì đề tài phải ĐẠT vòng đó mới được vào. Điều kiện cũ là
        // `Dimension == FINANCE` — FINANCE bỏ rồi nên nhánh đó thành code chết, không chặn được gì.
        if (request.PrerequisiteRoundId.HasValue)
        {
            var prereqProjectRound = await _review.ProjectRounds
                .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.RoundId == request.PrerequisiteRoundId.Value);
            // Chưa TỪNG tham gia vòng tiên quyết cũng là chưa đạt. Trước đây `!= null &&` khiến
            // đề tài chưa hề qua vòng trước lại lọt thẳng vào vòng sau, trong khi đường
            // `ReviewBoardService` cùng làm việc này thì chặn — hai lối vào, hai luật khác nhau.
            if (prereqProjectRound == null || prereqProjectRound.Status != ReviewRoundStatus.Passed)
                throw new InvalidOperationException(
                    $"Đề tài chưa ĐẠT vòng tiên quyết (hiện: {prereqProjectRound?.Status ?? "chưa tham gia"}) " +
                    "— chưa thể đưa vào vòng này.");
        }

        // DÙNG CHUNG round theo track (helper chung với ReviewBoardService).
        var round = await ReviewShared.GetOrCreateOpenRoundAsync(
            _review, cycleTrackId, request.Dimension, request.RoundType, request.RubricTemplateId, request.PrerequisiteRoundId);

        var existingLink = await _review.ProjectRounds
            .AnyAsync(pr => pr.ProjectId == projectId && pr.RoundId == round.Id);
        if (existingLink)
            throw new InvalidOperationException("Đề tài đã tham gia vòng chấm này rồi.");

        await _review.AddProjectRoundAsync(new ProjectRound
        {
            ProjectId = projectId,
            RoundId = round.Id,
            Status = ReviewRoundStatus.Pending
        });
        await _review.SaveChangesAsync();

        return MapToResponse(round, null);
    }

    public async Task<ReviewRoundResponse> OpenRoundAsync(Guid roundId)
    {
        var round = await _review.GetRoundByIdAsync(roundId)
            ?? throw new KeyNotFoundException("Không tìm thấy vòng chấm.");

        if (round.Status != ReviewRoundStatus.Pending)
            throw new InvalidOperationException($"Vòng đang ở trạng thái {StatusText.Vi(round.Status)} — chỉ mở được vòng chưa bắt đầu.");

        if (ReviewShared.IsAcceptance(round.RoundType))
        {
            await ReviewShared.AssertAcceptanceTrackReadyAsync(_review, _proposals, round.CycleTrackId);
            var projectIds = await _review.ProjectRounds
                .Where(pr => pr.RoundId == roundId)
                .Select(pr => pr.ProjectId)
                .ToListAsync();
            if (projectIds.Count == 0)
                throw new InvalidOperationException(
                    "Vòng NGHIỆM THU chưa có đề tài nào — hãy thêm đề tài đủ hồ sơ trước khi mở vòng.");
            foreach (var projectId in projectIds)
                await ReviewShared.AssertProjectEligibleForAcceptanceAsync(
                    _review, _proposals, round.CycleTrackId, projectId);
        }

        if (round.PrerequisiteRoundId.HasValue)
        {
            var prereq = await _review.GetRoundByIdAsync(round.PrerequisiteRoundId.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy vòng tiên quyết.");

            if (prereq.Status != ReviewRoundStatus.Passed)
                throw new InvalidOperationException(
                    $"Vòng tiên quyết (vòng #{prereq.RoundNumber}) chưa ĐẠT — đang ở trạng thái {StatusText.Vi(prereq.Status)}. Phải qua vòng đó trước.");
        }

        round.Status = ReviewRoundStatus.Open;
        round.OpenedAt = _clock.UtcNow;

        // Đặt hạn chấm ngay lúc mở vòng — trước 25/08 giai đoạn chấm là chặng DUY NHẤT không có hạn
        // nào: vòng mở ra rồi để đấy, không ai biết bao giờ phải xong. Dùng `??=` để mở lại một vòng
        // đã có hạn (hoặc đã được dời hạn) thì KHÔNG ghi đè mất hạn cũ.
        var windowDays = await _settings.GetIntAsync(
            SystemSettingKeys.ScoringWindowDays, SystemSettingKeys.DefaultScoringWindowDays);
        round.ScoringDeadline ??= DateOnly.FromDateTime(_clock.UtcNow).AddDays(windowDays);

        await _review.SaveChangesAsync();

        return await MapWithDeadlineAsync(round, null);
    }

    public async Task<ReviewRoundResponse> CloseRoundAsync(Guid roundId, CloseRoundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Result) ||
            !new[] { ReviewResult.Approved, ReviewResult.Rejected, ReviewResult.RevisionRequired }.Contains(request.Result))
            throw new ArgumentException("Kết quả chỉ nhận: Đạt, Không đạt hoặc Yêu cầu chỉnh sửa.");

        var round = await _review.ReviewRounds
            .Include(r => r.Councils)
            .Include(r => r.ProjectRounds)
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException("Không tìm thấy vòng chấm.");

        if (round.Status != ReviewRoundStatus.Open)
            throw new InvalidOperationException($"Vòng đang ở trạng thái {StatusText.Vi(round.Status)} — chỉ đóng được vòng đang mở.");

        // Kết quả áp cho TỪNG đề tài (project_round). Round nhiều đề tài → phải chỉ rõ.
        var targetLink = ResolveTargetProjectRound(round, request.ProposalProjectId);

        // Dùng chung logic với ApproveMinutesAsync: chốt project_round + tự đóng round nhất quán.
        ReviewRoundFinalizer.ApplyProjectResult(round, targetLink, request.Result, _clock.UtcNow);

        if (request.Result == ReviewResult.Rejected)
        {
            // Rule #1: REJECTED → kết thúc luôn — set cả bản đề cương hiện hành lẫn project.
            var proposal = await _proposals.Query().IgnoreQueryFilters()
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.ProjectId == targetLink.ProjectId && p.IsCurrent)
                ?? throw new KeyNotFoundException($"Current proposal of project {targetLink.ProjectId} not found.");
            proposal.Status = ProposalStatus.Rejected;
            proposal.Project.Status = ProjectStatus.Cancelled;
        }

        await _review.SaveChangesAsync();

        await NotifyProjectPiAsync(targetLink.ProjectId, round, request.Result);

        return MapToResponse(round, round.Councils.FirstOrDefault()?.Id);
    }

    // Round nhiều đề tài: request phải kèm ProposalProjectId; round 1 đề tài: tự suy.
    private static ProjectRound ResolveTargetProjectRound(ReviewRound round, Guid? explicitProjectId)
    {
        if (explicitProjectId.HasValue)
            return round.ProjectRounds.FirstOrDefault(pr => pr.ProjectId == explicitProjectId.Value)
                ?? throw new KeyNotFoundException("Đề tài không thuộc vòng chấm này.");

        var pending = round.ProjectRounds.Where(pr => pr.Status == ReviewRoundStatus.Pending).ToList();
        if (pending.Count == 1) return pending[0];
        if (round.ProjectRounds.Count == 1) return round.ProjectRounds.First();

        throw new InvalidOperationException(
            "Round này có nhiều đề tài — cần chỉ rõ projectId khi chốt kết quả (trường proposalProjectId).");
    }

    private async Task NotifyProjectPiAsync(Guid projectId, ReviewRound round, string result)
    {
        var project = await _proposals.Projects.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return;

        var message = result == ReviewResult.Approved
            ? $"Vòng xét duyệt #{round.RoundNumber} ({round.Dimension}) đã được PHÊ DUYỆT."
            : result == ReviewResult.Rejected
                ? $"Vòng xét duyệt #{round.RoundNumber} ({round.Dimension}) bị TỪ CHỐI. Đề tài bị từ chối."
                : $"Vòng xét duyệt #{round.RoundNumber} ({round.Dimension}) yêu cầu CHỈNH SỬA.";

        // Gửi kèm EMAIL: đây là tin PI cần biết ngay, không thể chờ họ tự mở app.
        await _notifier.NotifyAsync(
            project.PiUserId,
            "REVIEW_ROUND_CLOSED",
            $"Kết quả xét duyệt vòng {round.RoundNumber}",
            message,
            actionUrl: "/proposals/my",
            entityType: "ReviewRound",
            entityId: round.Id.ToString(),
            priority: result == ReviewResult.Rejected ? "HIGH" : "NORMAL");
        await _notifications.SaveChangesAsync();
    }

    public async Task<IEnumerable<CouncilMemberResponse>> GetRoundMembersAsync(Guid roundId)
    {
        var council = await _review.Query()
            .Where(c => c.RoundId == roundId)
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync();

        if (council == null) return Enumerable.Empty<CouncilMemberResponse>();

        return council.Members.Select(MapMember);
    }

    public async Task<CouncilMemberResponse> AddRoundMemberAsync(Guid roundId, AddRoundMemberRequest request, Guid addedBy)
    {
        if (!new[] { "Member", "Chair", "Secretary", "Opponent" }.Contains(request.MemberRole))
            throw new ArgumentException("Vai trong hội đồng chỉ nhận: Thành viên, Chủ tịch, Thư ký hoặc Phản biện.");

        var round = await _review.ReviewRounds
            .Include(r => r.ProjectRounds)
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException("Không tìm thấy vòng chấm.");

        // Find or auto-create council for this round
        var council = await _review.Query()
            .Include(c => c.Members)
            .Include(c => c.ProjectAssignments)
            .FirstOrDefaultAsync(c => c.RoundId == roundId);

        if (council == null)
        {
            council = new ReviewCouncil
            {
                Id = Guid.NewGuid(),
                RoundId = roundId,
                CouncilType = round.RoundType,
                MinMembersRequired = 3,
                MaxMembersAllowed = 5,
                Status = CouncilStatus.Forming,
                CreatedBy = addedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _review.AddAsync(council);

            // Council chấm NHÓM đề tài của round — gán toàn bộ project đang tham gia round.
            foreach (var pr in round.ProjectRounds)
                await _review.AddProjectAssignmentAsync(new CouncilProjectAssignment
                {
                    CouncilId = council.Id,
                    ProjectId = pr.ProjectId
                });

            await _review.SaveChangesAsync();
            council.Members = new List<CouncilMember>();
        }

        // COI (rule #5) — check với TẤT CẢ đề tài council này chấm.
        var assignedProjectIds = round.ProjectRounds.Select(pr => pr.ProjectId).ToList();
        await ReviewShared.AssertNoCoiAsync(_proposals, assignedProjectIds, new[] { request.ReviewerId });

        if (council.Members.Any(m => m.UserId == request.ReviewerId))
            throw new InvalidOperationException("Người này đã có tên trong hội đồng.");

        // Chỉ GÁN, chưa gửi thư mời — Staff bấm "Gửi thư mời" sau (tránh spam, rule #13).
        var member = new CouncilMember
        {
            Id = Guid.NewGuid(),
            CouncilId = council.Id,
            UserId = request.ReviewerId,
            MemberRole = request.MemberRole,
            IsExternal = request.IsExternal,
            Status = CouncilMemberStatus.Assigned,
            InvitationSentAt = null
        };

        await _review.AddMemberAsync(member);
        await _review.SaveChangesAsync();

        var memberWithUser = await _review.CouncilMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == member.Id);

        return MapMember(memberWithUser ?? member);
    }

    // Rule #1: nộp lại bản REVISION → mở lại hội đồng đã chốt "cần chỉnh sửa" (GIỮ điểm cũ).
    public async Task ReopenAfterResubmitAsync(Guid projectId)
    {
        // Biên bản đã KHÓA với kết quả REVISION_REQUIRED của đề tài này.
        var revisionDecisions = await _review.Decisions
            .Where(d => d.ProjectId == projectId && d.FinalizedAt != null
                        && d.Result == ReviewResult.RevisionRequired)
            .ToListAsync();
        if (revisionDecisions.Count == 0) return;

        foreach (var decision in revisionDecisions)
        {
            // Mở khoá biên bản → về NHÁP (Thư ký/Chủ tịch chấm & chốt lại). KHÔNG đụng ProposalReviewScore
            // ⇒ điểm cũ được giữ; reviewer chỉ chỉnh nếu muốn.
            decision.FinalizedAt = null;

            var council = await _review.Query()
                .FirstOrDefaultAsync(c => c.Id == decision.CouncilId);
            if (council != null && council.Status == CouncilStatus.Decided)
            {
                council.Status = CouncilStatus.Forming;
                council.UpdatedAt = DateTime.UtcNow;
            }

            if (council?.RoundId is Guid roundId)
            {
                var pr = await _review.ProjectRounds
                    .FirstOrDefaultAsync(x => x.RoundId == roundId && x.ProjectId == projectId);
                if (pr != null)
                {
                    pr.Status = ReviewRoundStatus.Open;
                    pr.Result = null;
                    pr.FinalizedAt = null;
                }

                // Nếu round đã đóng (đề tài này từng làm round terminal) → mở lại.
                var round = await _review.GetRoundByIdAsync(roundId);
                if (round != null &&
                    (round.Status == ReviewRoundStatus.Passed || round.Status == ReviewRoundStatus.Failed))
                {
                    round.Status = ReviewRoundStatus.Open;
                    round.Result = null;
                    round.ClosedAt = null;
                }
            }
        }

        await _review.SaveChangesAsync();
    }

    public async Task RemoveRoundMemberAsync(Guid roundId, Guid memberId)
    {
        var council = await _review.Query()
            .FirstOrDefaultAsync(c => c.RoundId == roundId)
            ?? throw new KeyNotFoundException($"No council found for round {roundId}.");

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.CouncilId == council.Id)
            ?? throw new KeyNotFoundException($"Member {memberId} not found in round's council.");

        _review.RemoveMember(member);
        await _review.SaveChangesAsync();
    }

    private static CouncilMemberResponse MapMember(CouncilMember m) => ReviewShared.MapMember(m);

    private static ReviewRoundResponse MapToResponse(ReviewRound r, Guid? councilId) => new()
    {
        Id = r.Id,
        RoundNumber = r.RoundNumber,
        Dimension = r.Dimension,
        RoundType = r.RoundType,
        RubricTemplateId = r.RubricTemplateId,
        Sequence = r.Sequence,
        PrerequisiteRoundId = r.PrerequisiteRoundId,
        Status = r.Status,
        OpenedAt = r.OpenedAt,
        ClosedAt = r.ClosedAt,
        Result = r.Result,
        ScoringDeadline = r.ScoringDeadline?.ToString("yyyy-MM-dd"),
        CouncilId = councilId
    };

    public async Task<ReviewRoundResponse> SetRoundDeadlineAsync(
        Guid roundId, SetRoundDeadlineRequest request, Guid actorId)
    {
        if (!DateOnly.TryParse(request.ScoringDeadline, out var newDeadline))
            throw new ArgumentException("Hạn chấm không hợp lệ — cần dạng ngày yyyy-MM-dd.");

        var round = await _review.GetRoundByIdAsync(roundId)
            ?? throw new KeyNotFoundException("Không tìm thấy vòng chấm.");

        if (round.Status is ReviewRoundStatus.Passed or ReviewRoundStatus.Failed)
            throw new InvalidOperationException(
                $"Vòng đã chốt kết quả ({StatusText.Vi(round.Status)}) — đặt hạn chấm không còn ý nghĩa.");

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (newDeadline < today)
            throw new ArgumentException(
                $"Hạn chấm không được đặt vào quá khứ (hôm nay {today:dd/MM/yyyy}).");

        // Hạn ĐANG hiệu lực — có thể đã khác ngày gốc nếu trước đó đã dời.
        var current = round.ScoringDeadline is { } original
            ? await _deadlines.EffectiveAsync(
                IDeadlineResolver.TargetTypeReviewRound, roundId.ToString(), original)
            : (DateOnly?)null;

        if (current is { } effective)
        {
            if (effective == newDeadline)
                return await MapWithDeadlineAsync(round, null);

            // Rule #19: DỜI hạn là ghi LOG, không ghi đè. `ScoringDeadline` giữ nguyên ngày GỐC để
            // sau này còn đối chiếu "ban đầu hẹn bao giờ, đã lùi mấy lần, vì sao".
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException(
                    "Vòng này đã có hạn chấm — dời hạn thì phải ghi rõ lý do.");

            await _cycles.AddDeadlineExtensionAsync(new DeadlineExtension
            {
                TargetType = IDeadlineResolver.TargetTypeReviewRound,
                TargetId = roundId.ToString(),
                OldDeadline = effective,
                NewDeadline = newDeadline,
                Reason = request.Reason.Trim(),
                CreatedBy = actorId,
                CreatedAt = _clock.UtcNow
            });
            await _cycles.SaveChangesAsync();
        }
        else
        {
            // Lần đầu đặt hạn: ghi thẳng vào vòng, chưa có gì để "dời".
            round.ScoringDeadline = newDeadline;
            await _review.SaveChangesAsync();
        }

        return await MapWithDeadlineAsync(round, null);
    }

    /// <summary>
    /// Như <see cref="MapToResponse"/> nhưng trả hạn <b>HIỆU LỰC</b> (đã tính các lần dời hạn ghi ở
    /// <c>deadline_extension</c> — rule #19: dời hạn là LOG, không ghi đè) và cờ quá hạn.
    /// </summary>
    private async Task<ReviewRoundResponse> MapWithDeadlineAsync(ReviewRound r, Guid? councilId)
    {
        var dto = MapToResponse(r, councilId);
        if (r.ScoringDeadline is not { } original) return dto;

        var effective = await _deadlines.EffectiveAsync(
            IDeadlineResolver.TargetTypeReviewRound, r.Id.ToString(), original);

        dto.ScoringDeadline = effective.ToString("yyyy-MM-dd");
        // Chỉ vòng CÒN MỞ mới tính là quá hạn — vòng đã chốt kết quả thì hạn không còn ý nghĩa.
        dto.IsScoringOverdue = r.Status == ReviewRoundStatus.Open
                               && DateOnly.FromDateTime(_clock.UtcNow) > effective;
        return dto;
    }
}
