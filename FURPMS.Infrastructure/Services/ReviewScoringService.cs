using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ReviewScoringService : IReviewScoringService
{
    private readonly IReviewRepository _review;
    private readonly IMasterDataRepository _masterData;
    private readonly IProposalRepository _proposals;
    private readonly IClock _clock;
    // Bước nhảy điểm chấm là tham số nghiệp vụ Admin chỉnh được, không hardcode.
    private readonly ISystemSettingService _settings;

    private readonly INotifier _notifier;

    public ReviewScoringService(
        IReviewRepository review,
        IMasterDataRepository masterData,
        IProposalRepository proposals,
        IClock clock,
        ISystemSettingService settings,
        INotifier notifier)
    {
        _review = review;
        _masterData = masterData;
        _proposals = proposals;
        _clock = clock;
        _settings = settings;
        _notifier = notifier;
    }

    // Phase B: council chấm NHÓM đề tài — suy ra project từ assignment
    // (1 đề tài → tự suy; nhiều đề tài → request phải chỉ rõ projectId).
    private async Task<Guid> ResolveProjectIdAsync(Guid councilId, Guid? explicitProjectId)
    {
        var assignments = await _review.ProjectAssignments
            .Where(a => a.CouncilId == councilId)
            .Select(a => a.ProjectId)
            .ToListAsync();

        if (assignments.Count == 0)
            throw new InvalidOperationException("Hội đồng chưa được gán đề tài nào để chấm.");

        if (explicitProjectId.HasValue)
        {
            if (!assignments.Contains(explicitProjectId.Value))
                throw new KeyNotFoundException("Đề tài không thuộc phạm vi chấm của hội đồng này.");
            return explicitProjectId.Value;
        }

        if (assignments.Count == 1) return assignments[0];
        throw new InvalidOperationException(
            "Hội đồng này chấm nhiều đề tài — cần chỉ rõ projectId trong request.");
    }

    // ── Rubric templates ──────────────────────────────────────────────────────

    public async Task<IEnumerable<RubricTemplateDto>> GetRubricTemplatesAsync()
    {
        var templates = await _masterData.RubricTemplates
            .Where(t => t.IsActive)
            .Include(t => t.Criteria)
            .OrderBy(t => t.TemplateType)
            .ToListAsync();

        return templates.Select(MapTemplate);
    }

    public async Task<RubricTemplateDto> GetRubricTemplateByIdAsync(int templateId)
    {
        var template = await _masterData.RubricTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new KeyNotFoundException($"Rubric template {templateId} not found.");

        return MapTemplate(template);
    }

    // ── Submit score ──────────────────────────────────────────────────────────

    public async Task<ReviewScoreDto> SubmitScoreAsync(Guid councilId, Guid userId, SubmitScoreRequest request)
    {
        var council = await _review.Query()
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var projectId = await ResolveProjectIdAsync(councilId, request.ProjectId);

        // Đã có biên bản KHÓA cho đề tài này → không sửa điểm nữa.
        var lockedDecision = await _review.Decisions
            .AnyAsync(d => d.CouncilId == councilId && d.ProjectId == projectId && d.FinalizedAt != null);
        if (council.Status == CouncilStatus.Decided || lockedDecision)
            throw new InvalidOperationException("Biên bản đã được Chủ tịch chốt — không sửa được điểm nữa.");

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId)
            ?? throw new ForbiddenException("Bạn không thuộc hội đồng này.");
        AssertConfirmed(member);

        var template = await _masterData.RubricTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId)
            ?? throw new KeyNotFoundException($"Rubric template {request.TemplateId} not found.");

        /*
         * Bộ tiêu chí phải CỘNG ĐÚNG MaxTotalScore mới được đem chấm.
         * QĐ543 BM03 ghi rõ dòng "Cộng 100" — bộ 60 điểm hay 125 điểm thì mọi con số suy ra sau
         * đó (điểm trung bình hội đồng, tỷ lệ %) đều vô nghĩa. Chặn ở ĐÂY chứ không chặn lúc
         * thêm tiêu chí, vì bộ phải xây dần từng mục mới đủ 100.
         */
        var totalCriteria = template.Criteria.Where(c => c.IsActive).Sum(c => c.MaxScore);
        if (totalCriteria != template.MaxTotalScore)
            throw new InvalidOperationException(
                $"Bộ tiêu chí \"{template.Name}\" đang cộng được {totalCriteria:0.##}/{template.MaxTotalScore:0.##} điểm " +
                "nên chưa dùng để chấm được (QĐ543 BM03: phiếu chấm phải cộng đúng tổng điểm). " +
                "Nhờ phòng QLKH chỉnh lại bộ tiêu chí trước.");

        // Validate all criteria provided
        var criterionIds = template.Criteria.Select(c => c.Id).ToHashSet();
        var providedIds = request.ScoreDetails.Select(d => d.CriterionId).ToHashSet();
        var missing = criterionIds.Except(providedIds).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"Missing scores for criterion IDs: {string.Join(", ", missing)}.");

        /*
         * Bước nhảy điểm do Admin quy định (mặc định: số nguyên).
         * QĐ543 không quy định nguyên hay thập phân — BM03 để điểm tối đa toàn số nguyên. Thầy
         * 08/08: cho Admin đặt từ đầu, PI/hội đồng làm việc khi luật đã rõ. Đổi cấu hình KHÔNG hồi
         * tố: phiếu đã chấm giữ nguyên, chỉ phiếu mới bị soi — giống rule #13.
         */
        var decimals = await _settings.GetIntAsync(
            SystemSettingKeys.ScoreDecimalPlaces, SystemSettingKeys.DefaultScoreDecimalPlaces);

        // Validate score ranges
        foreach (var detail in request.ScoreDetails)
        {
            if (decimal.Round(detail.GivenScore, decimals) != detail.GivenScore)
                throw new ArgumentException(decimals == 0
                    ? $"Điểm phải là SỐ NGUYÊN (đang nhập {detail.GivenScore}). Phòng QLKH đổi được ở Cấu hình hệ thống."
                    : $"Điểm chỉ được có tối đa {decimals} chữ số thập phân (đang nhập {detail.GivenScore}).");

            var criterion = template.Criteria.FirstOrDefault(c => c.Id == detail.CriterionId);
            if (criterion == null)
                throw new ArgumentException($"Tiêu chí {detail.CriterionId} không thuộc bộ tiêu chí đang dùng.");
            if (detail.GivenScore < 0 || detail.GivenScore > criterion.MaxScore)
                throw new ArgumentException(
                    $"Score {detail.GivenScore} for '{criterion.CriterionName}' is out of range [0, {criterion.MaxScore}].");
        }

        // Upsert: if score already exists for this member+council, replace details
        var existing = await _review.ReviewScores
            .Include(s => s.ScoreDetails)
            .FirstOrDefaultAsync(s => s.CouncilId == councilId && s.ProjectId == projectId && s.EvaluatorMemberId == member.Id);

        ProposalReviewScore score;
        if (existing != null)
        {
            _review.RemoveScoreDetailsRange(existing.ScoreDetails);
            existing.TemplateId = request.TemplateId;
            existing.GeneralComments = request.GeneralComments;
            existing.OtherRecommendations = request.OtherRecommendations;
            existing.SubmittedAt = _clock.UtcNow;
            existing.IsValidBallot = true;
            score = existing;
        }
        else
        {
            score = new ProposalReviewScore
            {
                CouncilId = councilId,
                ProjectId = projectId,
                EvaluatorMemberId = member.Id,
                TemplateId = request.TemplateId,
                GeneralComments = request.GeneralComments,
                OtherRecommendations = request.OtherRecommendations,
                SubmittedAt = _clock.UtcNow,
                IsValidBallot = true
            };
            await _review.AddScoreAsync(score);
        }

        await _review.SaveChangesAsync();

        _review.AddScoreDetailsRange(request.ScoreDetails.Select(d => new ReviewScoreDetail
        {
            ScoreId = score.Id,
            CriterionId = d.CriterionId,
            GivenScore = d.GivenScore,
            Comments = d.Comments
        }));
        await _review.SaveChangesAsync();

        return await BuildScoreDto(score, template, member.UserId);
    }

    // ── Get scores ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phieu cua CHINH TOI cho MOT de tai. Thieu projectId thi hoi dong cham nhieu de tai se tra
    /// nham phieu cua de tai khac -- nguoi cham mo de tai B lai thay diem da nhap cho de tai A.
    /// </summary>
    public async Task<ReviewScoreDto?> GetMyScoreAsync(Guid councilId, Guid userId, Guid? projectId = null)
    {
        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId);
        if (member == null) return null;

        var pid = await ResolveProjectIdAsync(councilId, projectId);

        var score = await _review.ReviewScores
            .Include(s => s.ScoreDetails).ThenInclude(d => d.Criterion)
            .Include(s => s.Template).ThenInclude(t => t.Criteria)
            .FirstOrDefaultAsync(s => s.CouncilId == councilId && s.ProjectId == pid && s.EvaluatorMemberId == member.Id);

        if (score == null) return null;
        return MapScore(score, userId);
    }

    /// <summary>
    /// Toan bo phieu cua MOT de tai trong hoi dong. Khong loc theo de tai thi hoi dong cham 3 de
    /// tai tra ve 15 phieu tron lan, bien ban in ra sai hoan toan.
    /// </summary>
    public async Task<IEnumerable<ReviewScoreDto>> GetCouncilScoresAsync(Guid councilId, Guid? projectId = null)
    {
        _ = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var pid = await ResolveProjectIdAsync(councilId, projectId);

        var scores = await _review.ReviewScores
            .Include(s => s.ScoreDetails).ThenInclude(d => d.Criterion)
            .Include(s => s.Template).ThenInclude(t => t.Criteria)
            .Include(s => s.EvaluatorMember).ThenInclude(m => m.User)
            .Where(s => s.CouncilId == councilId && s.ProjectId == pid)
            .ToListAsync();

        return scores.Select(s => MapScore(s, s.EvaluatorMember.UserId));
    }

    // ── Finalize decision (NGỪNG DÙNG) ────────────────────────────────────────

    // Đường chốt trực tiếp này BỎ QUA biên bản Thư ký→Chủ tịch (vi phạm rule #12) và trước đây
    // KHÔNG đồng bộ project_round → gây lệch dữ liệu round/proposal. Đã khoá lại: hãy chốt qua
    // luồng biên bản — POST .../minutes (Thư ký soạn) rồi POST .../minutes/approve (Chủ tịch duyệt).
    public Task<CouncilDecisionDto> FinalizeDecisionAsync(Guid councilId, FinalizeDecisionRequest request)
        => throw new InvalidOperationException(
            "Đã ngừng dùng: hãy chốt kết quả qua biên bản (POST .../minutes rồi POST .../minutes/approve).");

    // ── Biên bản: Thư ký soạn (nháp) → Chủ tịch duyệt = khoá ───────────────────

    public async Task<CouncilDecisionDto> SaveMinutesAsync(Guid councilId, Guid secretaryUserId, SaveMinutesRequest request)
    {
        var validResults = new[] { ReviewResult.Approved, ReviewResult.Rejected, ReviewResult.RevisionRequired };
        if (!validResults.Contains(request.Result))
            throw new ArgumentException($"Kết quả chỉ nhận: {string.Join(", ", validResults)}.");

        var council = await _review.Query().Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        if (council.Status == CouncilStatus.Decided)
            throw new InvalidOperationException("Biên bản đã được Chủ tịch khoá, không thể sửa.");

        var me = council.Members.FirstOrDefault(m => m.UserId == secretaryUserId)
            ?? throw new ForbiddenException("Bạn không thuộc hội đồng này.");
        AssertConfirmed(me);
        if (!IsSecretary(me.MemberRole))
            throw new ForbiddenException("Chỉ Thư ký hội đồng được soạn biên bản.");

        var projectIdM = await ResolveProjectIdAsync(councilId, request.ProjectId);
        await AssertQuorumAsync(council, projectIdM, "lưu biên bản");
        var (total, attending, valid, invalid, avg) = await ComputeTallyAsync(council, councilId, projectIdM);

        var decision = await _review.Decisions
            .Include(d => d.QaEntries)
            .Include(d => d.MemberOpinions)
            .FirstOrDefaultAsync(d => d.CouncilId == councilId && d.ProjectId == projectIdM);
        if (decision != null && decision.FinalizedAt != null)
            throw new InvalidOperationException("Biên bản đã được khoá.");

        if (decision == null)
        {
            decision = new CouncilDecision { CouncilId = councilId, ProjectId = projectIdM };
            await _review.AddDecisionAsync(decision);
        }

        decision.TotalMembers = total;
        decision.AttendingMembers = attending;
        decision.ValidBallots = valid;
        decision.InvalidBallots = invalid;
        decision.AverageScore = avg;                 // điểm/phiếu chỉ để tham khảo
        decision.Result = request.Result;            // Thư ký ghi nhận kết quả họp kín
        decision.CouncilComments = request.CouncilComments;
        decision.Recommendations = request.Recommendations;
        decision.SecretaryUserId = secretaryUserId;
        decision.FinalizedAt = null;                 // vẫn là nháp, CHƯA khoá

        // Thư ký đã lưu bản mới ⇒ xoá yêu cầu sửa của Chủ tịch. Không xoá thì cảnh báo "cần
        // sửa" treo mãi trên màn dù đã sửa xong, và Chủ tịch không phân biệt được bản này đã
        // xử lý yêu cầu hay chưa.
        decision.RevisionRequestNote = null;
        decision.RevisionRequestedAt = null;
        decision.RevisionRequestedBy = null;

        // Cách 1 (Q&A): thay TOÀN BỘ danh sách hỏi–đáp mỗi lần lưu nháp (xoá cũ tường minh
        // để EF không báo "severed" khi FK bắt buộc, rồi thêm mới).
        if (decision.QaEntries.Count > 0)
            _review.RemoveQaEntriesRange(decision.QaEntries.ToList());
        decision.QaEntries.Clear();
        if (request.QaEntries != null)
        {
            var order = 0;
            foreach (var qa in request.QaEntries.Where(q => !string.IsNullOrWhiteSpace(q.Question)))
                decision.QaEntries.Add(new CouncilQaEntry
                {
                    AskedBy = qa.AskedBy,
                    Question = qa.Question,
                    Answer = qa.Answer,
                    Order = qa.Order != 0 ? qa.Order : order++
                });
        }

        // II.1 — ý kiến từng thành viên (chuyên môn / kinh phí): cũng thay TOÀN BỘ.
        if (decision.MemberOpinions.Count > 0)
            _review.RemoveMemberOpinionsRange(decision.MemberOpinions.ToList());
        decision.MemberOpinions.Clear();
        if (request.MemberOpinions != null)
        {
            var oOrder = 0;
            foreach (var op in request.MemberOpinions.Where(o => !string.IsNullOrWhiteSpace(o.MemberName)))
                decision.MemberOpinions.Add(new CouncilMemberOpinion
                {
                    MemberName = op.MemberName,
                    AcademicComment = op.AcademicComment,
                    BudgetComment = op.BudgetComment,
                    Order = op.Order != 0 ? op.Order : oOrder++
                });
        }

        await _review.SaveChangesAsync();
        return MapDecision(decision);
    }

    /// <inheritdoc/>
    public async Task<CouncilDecisionDto> RequestMinutesRevisionAsync(
        Guid councilId, Guid chairUserId, string note, Guid? projectId = null)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException(
                "Phải nêu rõ cần sửa gì. Trả biên bản mà không nói lý do thì Thư ký chỉ biết là bị " +
                "trả lại, không biết sửa chỗ nào.");

        var council = await _review.Query().Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var me = council.Members.FirstOrDefault(m => m.UserId == chairUserId)
            ?? throw new ForbiddenException("Bạn không thuộc hội đồng này.");
        AssertConfirmed(me);
        if (!IsChair(me.MemberRole))
            throw new ForbiddenException("Chỉ Chủ tịch hội đồng được trả biên bản cho Thư ký sửa.");

        var query = _review.Decisions.Where(d => d.CouncilId == councilId);
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId.Value);
        var drafts = await query.ToListAsync();

        if (drafts.Count == 0)
            throw new InvalidOperationException("Chưa có biên bản nào để trả lại.");
        if (drafts.Count > 1)
            throw new InvalidOperationException(
                "Hội đồng có nhiều biên bản (nhiều đề tài) — nêu rõ đề tài cần trả lại.");

        var decision = drafts[0];

        // Đã chốt thì thôi: khoá là khoá (rule #12). Muốn sửa bản đã chốt phải là một quy trình
        // khác hẳn, không thể lách qua nút "trả lại".
        if (decision.FinalizedAt != null)
            throw new InvalidOperationException(
                "Biên bản đã được chốt và khoá — không trả lại được. " +
                "Khoá xong là kết quả đã có hiệu lực với đề tài.");

        decision.RevisionRequestNote = note.Trim();
        decision.RevisionRequestedAt = _clock.UtcNow;
        decision.RevisionRequestedBy = chairUserId;
        await _review.SaveChangesAsync();

        // Báo đích danh Thư ký. Không có Thư ký ghi trên biên bản thì báo người giữ vai Thư ký
        // trong hội đồng — biên bản mới soạn dở có thể chưa gán SecretaryUserId.
        var secretaryId = decision.SecretaryUserId
            ?? council.Members.FirstOrDefault(m => IsSecretary(m.MemberRole))?.UserId;

        if (secretaryId.HasValue)
        {
            var chairName = await _review.Query()
                .Where(c => c.Id == councilId)
                .SelectMany(c => c.Members)
                .Where(m => m.UserId == chairUserId)
                .Select(m => m.User!.FullName)
                .FirstOrDefaultAsync() ?? "Chủ tịch hội đồng";

            await _notifier.NotifyAsync(
                secretaryId.Value,
                "MINUTES_REVISION_REQUESTED",
                "Chủ tịch yêu cầu sửa biên bản",
                $"{chairName} đề nghị chỉnh sửa biên bản trước khi chốt. Nội dung cần sửa: {note.Trim()}",
                actionUrl: "/assigned-reviews",
                entityType: "CouncilDecision",
                entityId: decision.Id.ToString(),
                priority: "HIGH");
        }

        return MapDecision(decision);
    }

    public async Task<CouncilDecisionDto> ApproveMinutesAsync(Guid councilId, Guid chairUserId, Guid? projectId = null)
    {
        var council = await _review.Query().Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        if (council.Status == CouncilStatus.Decided)
            throw new InvalidOperationException("Biên bản đã được khoá.");

        var me = council.Members.FirstOrDefault(m => m.UserId == chairUserId)
            ?? throw new ForbiddenException("Bạn không thuộc hội đồng này.");
        AssertConfirmed(me);
        if (!IsChair(me.MemberRole))
            throw new ForbiddenException("Chỉ Chủ tịch hội đồng được duyệt biên bản.");

        // Bản nháp đang chờ duyệt (per project) — council 1 đề tài thì chỉ có 1 nháp.
        var draftQuery = _review.Decisions
            .Include(d => d.QaEntries)
            .Include(d => d.MemberOpinions)
            .Where(d => d.CouncilId == councilId && d.FinalizedAt == null);
        // Chi ro de tai thi chot dung bien ban do -- hoi dong cham nhieu de tai truoc day bao
        // "co nhieu bien ban nhap" va khong chot duoc cai nao.
        if (projectId.HasValue) draftQuery = draftQuery.Where(d => d.ProjectId == projectId.Value);
        var drafts = await draftQuery.ToListAsync();
        if (drafts.Count == 0)
            throw new InvalidOperationException("Chưa có biên bản nháp để duyệt.");
        if (drafts.Count > 1)
            throw new InvalidOperationException(
                "Hội đồng có nhiều biên bản nháp (nhiều đề tài) — duyệt từng biên bản qua API theo đề tài.");
        var decision = drafts[0];

        await AssertQuorumAsync(council, decision.ProjectId, "chốt biên bản");

        decision.ChairUserId = chairUserId;
        decision.FinalizedAt = _clock.UtcNow;      // duyệt = khoá

        // Chủ tịch chốt biên bản = buổi họp đã diễn ra xong. Trước đây buổi họp kẹt ở SCHEDULED
        // vĩnh viễn (không ai bấm Bắt đầu/Kết thúc), nên lịch vẫn hiện như sắp họp dù đề tài đã
        // có kết quả. Mọi thứ khác (đề tài, vòng, hội đồng) đều đã đổi trạng thái, riêng buổi họp
        // thì không.
        var openMeetings = await _review.Meetings
            .Where(m => m.CouncilId == council.Id && m.Status != MeetingStatus.Completed)
            .ToListAsync();
        foreach (var m in openMeetings)
        {
            m.ActualEndAt ??= _clock.UtcNow;
            m.Status = MeetingStatus.Completed;
        }

        await MarkDecidedIfAllProjectsFinalizedAsync(council, decision.ProjectId);

        // Vòng NGHIỆM THU khác vòng XÉT DUYỆT: nghiệm thu Đạt → đề tài HOÀN THÀNH (Process_Spec:
        // ACCEPTANCE → COMPLETED), chưa đạt → quay lại IN_PROGRESS để PI làm tiếp. Trước đây mọi
        // vòng đều set APPROVED/CANCELLED nên nghiệm thu xong đề tài vẫn "Đã duyệt" — đứt mạch cuối.
        ReviewRound? round = null;
        if (council.RoundId.HasValue)
        {
            round = await _review.ReviewRounds
                .Include(r => r.ProjectRounds)
                .FirstOrDefaultAsync(r => r.Id == council.RoundId.Value);
        }
        var isAcceptance = string.Equals(round?.RoundType, "ACCEPTANCE", StringComparison.OrdinalIgnoreCase);

        // Chỉ KHI Chủ tịch duyệt mới cập nhật status đề tài (bản hiện hành + project).
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.ProjectId == decision.ProjectId && p.IsCurrent);
        if (proposal != null)
        {
            if (!isAcceptance)
            {
                proposal.Status = decision.Result switch
                {
                    ReviewResult.Approved => ProposalStatus.Approved,
                    ReviewResult.Rejected => ProposalStatus.Rejected,
                    ReviewResult.RevisionRequired => ProposalStatus.RevisionRequired,
                    _ => proposal.Status
                };
                proposal.UpdatedAt = DateTime.UtcNow;
            }

            proposal.Project.Status = isAcceptance
                ? decision.Result switch
                {
                    ReviewResult.Approved => ProjectStatus.Completed,      // nghiệm thu ĐẠT → hoàn thành
                    ReviewResult.Rejected => ProjectStatus.InProgress,     // chưa đạt → làm tiếp
                    ReviewResult.RevisionRequired => ProjectStatus.InProgress,
                    _ => proposal.Project.Status
                }
                : decision.Result switch
                {
                    ReviewResult.Approved => ProjectStatus.Approved,
                    ReviewResult.Rejected => ProjectStatus.Cancelled,
                    _ => proposal.Project.Status
                };
            proposal.Project.UpdatedAt = DateTime.UtcNow;
        }

        // Nối mạch project_round (dùng chung logic với CloseRoundAsync qua ReviewRoundFinalizer):
        // đánh dấu kết quả CỦA ĐỀ TÀI NÀY + tự đóng round khi mọi đề tài đã CHỐT terminal.
        // REVISION_REQUIRED giữ vòng mở (không terminal) để PI sửa & nộp lại — rule #1.
        var projectRound = round?.ProjectRounds.FirstOrDefault(pr => pr.ProjectId == decision.ProjectId);
        if (round != null && projectRound != null)
            ReviewRoundFinalizer.ApplyProjectResult(round, projectRound, decision.Result, _clock.UtcNow);

        await _review.SaveChangesAsync();

        // Biên bản khoá = kết quả CHÍNH THỨC (rule #12). Hai nhóm cần biết ngay:
        //  · thành viên hội đồng — để thôi chờ, và biết bản mình góp ý đã chốt;
        //  · chủ nhiệm — nhưng CHỈ ở vòng nghiệm thu. Vòng xét duyệt đã có
        //    `REVIEW_ROUND_CLOSED` bắn lúc đóng vòng; báo cả hai chỗ là chủ nhiệm nhận trùng.
        var resultText = decision.Result switch
        {
            ReviewResult.Approved => isAcceptance ? "ĐẠT" : "ĐƯỢC DUYỆT",
            ReviewResult.Rejected => isAcceptance ? "KHÔNG ĐẠT" : "KHÔNG DUYỆT",
            ReviewResult.RevisionRequired => "CẦN CHỈNH SỬA",
            _ => "đã chốt"
        };
        var stageText = isAcceptance ? "nghiệm thu" : "xét duyệt đề cương";
        var projectTitle = proposal?.Project.TitleVi ?? proposal?.TitleVi ?? "đề tài";

        var memberUserIds = council.Members
            .Where(m => m.UserId != Guid.Empty)
            .Select(m => m.UserId)
            .ToList();
        await _notifier.NotifyManyAsync(
            memberUserIds,
            "MINUTES_FINALIZED",
            "Biên bản đã được Chủ tịch chốt",
            $"Biên bản {stageText} đề tài \"{projectTitle}\" đã được Chủ tịch duyệt và khoá. Kết luận: {resultText}.",
            actionUrl: "/council-memberships",
            entityType: "ReviewCouncil",
            entityId: council.Id.ToString());

        if (isAcceptance && proposal != null)
        {
            await _notifier.NotifyAsync(
                proposal.Project.PiUserId,
                "ACCEPTANCE_FINALIZED",
                "Kết quả nghiệm thu đề tài",
                $"Hội đồng nghiệm thu đã kết luận đề tài \"{projectTitle}\": {resultText}.",
                actionUrl: "/my-timeline",
                entityType: "ReviewCouncil",
                entityId: council.Id.ToString(),
                priority: "HIGH");
        }

        return MapDecision(decision);
    }

    // Council chỉ chuyển DECIDED khi MỌI đề tài được gán đều đã có biên bản khoá.
    private async Task MarkDecidedIfAllProjectsFinalizedAsync(ReviewCouncil council, Guid justFinalizedProjectId)
    {
        var assigned = await _review.ProjectAssignments
            .Where(a => a.CouncilId == council.Id)
            .Select(a => a.ProjectId)
            .ToListAsync();
        var finalized = await _review.Decisions
            .Where(d => d.CouncilId == council.Id && d.FinalizedAt != null)
            .Select(d => d.ProjectId)
            .ToListAsync();
        finalized.Add(justFinalizedProjectId);

        if (assigned.All(pid => finalized.Contains(pid)))
        {
            council.Status = CouncilStatus.Decided;
            council.UpdatedAt = DateTime.UtcNow;
        }
    }


    /// <summary>
    /// QĐ543 **BM12 mục 10.1**: *"Số phiếu phát ra … thu về … hợp lệ … không hợp lệ;
    /// Kết quả đánh giá: **Đạt** … **Không đạt** …"*.
    ///
    /// Trước đây màn biên bản chỉ có 4 ô (thành viên · có mặt · phiếu hợp lệ · điểm TB) nên Thư ký
    /// không có số để điền vào biểu mẫu, và không ai biết điểm nào của ai.
    ///
    /// Vòng XÉT DUYỆT chấm điểm (BM03 thang 100), vòng NGHIỆM THU chỉ Đạt/Không đạt (BM11 không
    /// có thang điểm) — nên trả cả hai kiểu, phía nào không dùng thì để null.
    /// </summary>
    public async Task<BallotTallyDto> GetBallotTallyAsync(Guid councilId, Guid? projectId)
    {
        var council = await _review.Query().Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var pid = await ResolveProjectIdAsync(councilId, projectId);
        var isAcceptance = await IsAcceptanceCouncilAsync(council);

        var scores = await _review.ReviewScores
            .Include(sc => sc.Template)
            .Where(sc => sc.CouncilId == councilId && sc.ProjectId == pid && sc.SubmittedAt != null)
            .ToListAsync();
        var scoreTotals = await _review.ReviewScoreDetails
            .Where(d => scores.Select(sc => sc.Id).Contains(d.ScoreId))
            .GroupBy(d => d.ScoreId)
            .Select(g => new { ScoreId = g.Key, Total = g.Sum(d => d.GivenScore) })
            .ToDictionaryAsync(x => x.ScoreId, x => x.Total);

        var evals = await _review.AcceptanceEvaluations
            .Where(e => e.CouncilId == councilId && e.ProjectId == pid && e.SubmittedAt != null)
            .ToListAsync();

        var ballots = new List<MemberBallotDto>();
        foreach (var m in council.Members.OrderBy(m => m.MemberRole))
        {
            var sc = scores.FirstOrDefault(x => x.EvaluatorMemberId == m.Id);
            var ev = evals.FirstOrDefault(x => x.EvaluatorMemberId == m.Id);
            ballots.Add(new MemberBallotDto
            {
                MemberId = m.Id,
                MemberName = m.User?.FullName ?? "—",
                MemberRole = m.MemberRole,
                HasSubmitted = sc != null || ev != null,
                IsValidBallot = ev?.IsValidBallot ?? sc?.IsValidBallot ?? false,
                TotalScore = sc != null && scoreTotals.TryGetValue(sc.Id, out var t) ? t : null,
                MaxScore = sc?.Template?.MaxTotalScore,
                Result = ev?.Result,
                Comments = ev?.FailReason ?? sc?.GeneralComments,
                SubmittedAt = ev?.SubmittedAt ?? sc?.SubmittedAt
            });
        }

        var returned = ballots.Count(b => b.HasSubmitted);
        var validTotals = ballots.Where(b => b.IsValidBallot && b.TotalScore.HasValue)
            .Select(b => b.TotalScore!.Value).ToList();

        return new BallotTallyDto
        {
            CouncilId = councilId,
            ProjectId = pid,
            IsAcceptanceRound = isAcceptance,
            TotalMembers = council.Members.Count,      // = số phiếu phát ra
            BallotsReturned = returned,
            ValidBallots = ballots.Count(b => b.HasSubmitted && b.IsValidBallot),
            InvalidBallots = ballots.Count(b => b.HasSubmitted && !b.IsValidBallot),
            PassCount = ballots.Count(b => b.Result == ReviewResult.Approved || b.Result == "PASS" || b.Result == "PASSED"),
            FailCount = ballots.Count(b => b.Result == ReviewResult.Rejected || b.Result == "FAIL" || b.Result == "FAILED"),
            AverageScore = validTotals.Count > 0 ? Math.Round(validTotals.Average(), 2) : null,
            Ballots = ballots
        };
    }

    /// <summary>
    /// Điều kiện họp hợp lệ theo QĐ543 — **Điều 8.3.b** (Hội đồng Xét duyệt) và **Điều 12.3.b**
    /// (Hội đồng Nghiệm thu):
    ///
    /// > *"Tham dự của **ít nhất 2/3 số thành viên** dưới sự chủ trì của Chủ tịch Hội đồng"*
    /// > *"…và **sự tham dự của thành viên phản biện**"* (riêng nghiệm thu)
    /// > *"các thành viên tham dự họp **cần đánh giá thẩm định** đề cương (Biểu mẫu 03)"*
    ///
    /// Vì thành viên dự họp bắt buộc phải chấm, số phiếu đã nộp chính là thước đo số người dự.
    /// Trước đây không kiểm gì: 1 người chấm trong hội đồng 5 người vẫn chốt được biên bản, mà
    /// biên bản là căn cứ đổi trạng thái đề tài.
    /// </summary>
    private async Task AssertQuorumAsync(ReviewCouncil council, Guid projectId, string action)
    {
        var total = council.Members.Count;
        if (total == 0)
            throw new InvalidOperationException($"Hội đồng chưa có thành viên nào — không thể {action}.");

        // Vòng XÉT DUYỆT nộp phiếu điểm (BM03 → review_scores), vòng NGHIỆM THU nộp phiếu
        // Đạt/Không đạt (BM11 → acceptance_evaluations). Đếm quorum phải gộp CẢ HAI, nếu không
        // hội đồng nghiệm thu dù đủ 5/5 phiếu vẫn bị báo "mới có 0/5 phiếu" và không chốt được.
        var scoreBallots = await _review.ReviewScores
            .Where(sc => sc.CouncilId == council.Id && sc.ProjectId == projectId && sc.SubmittedAt != null)
            .Select(sc => sc.EvaluatorMemberId)
            .ToListAsync();
        var acceptanceBallots = await _review.AcceptanceEvaluations
            .Where(e => e.CouncilId == council.Id && e.ProjectId == projectId && e.SubmittedAt != null)
            .Select(e => e.EvaluatorMemberId)
            .ToListAsync();
        var submitted = scoreBallots.Concat(acceptanceBallots).Distinct().ToList();

        // 2/3 làm tròn LÊN: hội đồng 5 người thì cần 4, không phải 3 (3.33 → 4).
        var required = (int)Math.Ceiling(total * 2m / 3m);
        if (submitted.Count < required)
            throw new InvalidOperationException(
                $"Chưa đủ số thành viên chấm để {action}: mới có {submitted.Count}/{total} phiếu, " +
                $"cần ít nhất {required} (QĐ543 Điều 8.3.b — tham dự ít nhất 2/3 số thành viên).");

        // Nghiệm thu bắt buộc có phản biện dự họp (Điều 12.3.b).
        var isAcceptance = await IsAcceptanceCouncilAsync(council);
        if (isAcceptance)
        {
            var opponentIds = council.Members
                .Where(m => RoleIs(m.MemberRole, CouncilMemberRole.Opponent))
                .Select(m => m.Id)
                .ToHashSet();
            if (opponentIds.Count > 0 && !submitted.Any(opponentIds.Contains))
                throw new InvalidOperationException(
                    $"Hội đồng nghiệm thu phải có thành viên phản biện dự họp và cho ý kiến " +
                    $"(QĐ543 Điều 12.3.b) — chưa có phiếu nào của phản biện, không thể {action}.");
        }
    }

    private async Task<bool> IsAcceptanceCouncilAsync(ReviewCouncil council)
    {
        if (!council.RoundId.HasValue) return false;
        var type = await _review.ReviewRounds
            .Where(r => r.Id == council.RoundId.Value)
            .Select(r => r.RoundType)
            .FirstOrDefaultAsync();
        return string.Equals(type, "ACCEPTANCE", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(int Total, int Attending, int Valid, int Invalid, decimal? Avg)> ComputeTallyAsync(
        ReviewCouncil council, Guid councilId, Guid projectId)
    {
        var scores = await _review.ReviewScores
            .Where(s => s.CouncilId == councilId && s.ProjectId == projectId && s.SubmittedAt != null)
            .ToListAsync();

        // Vòng nghiệm thu không chấm điểm (BM11 chỉ Đạt/Không đạt) — phiếu nằm ở bảng khác.
        // Không gộp thì biên bản nghiệm thu luôn hiện "0 phiếu hợp lệ" dù cả hội đồng đã bỏ phiếu.
        var evals = await _review.AcceptanceEvaluations
            .Where(e => e.CouncilId == councilId && e.ProjectId == projectId && e.SubmittedAt != null)
            .ToListAsync();
        var scoredMemberIds = scores.Select(s => s.EvaluatorMemberId).ToHashSet();
        var extraEvals = evals.Where(e => !scoredMemberIds.Contains(e.EvaluatorMemberId)).ToList();

        var validBallots = scores.Count(s => s.IsValidBallot) + extraEvals.Count(e => e.IsValidBallot);
        var returned = scores.Count + extraEvals.Count;

        // Điểm trung bình chỉ có nghĩa với vòng chấm điểm; nghiệm thu để null.
        var scored = scores.Where(s => s.IsValidBallot).ToList();
        var avgScore = scored.Count > 0
            ? scored.Average(s =>
                _review.ReviewScoreDetails
                    .Where(d => d.ScoreId == s.Id)
                    .Sum(d => d.GivenScore))
            : (decimal?)null;

        return (council.Members.Count, returned, validBallots, returned - validBallots, avgScore);
    }

    private static bool RoleIs(string? role, string target)
        => !string.IsNullOrWhiteSpace(role) && role.Trim().Equals(target, StringComparison.OrdinalIgnoreCase);

    private static bool IsChair(string? role) => RoleIs(role, CouncilMemberRole.Chair) || RoleIs(role, "Chairman");
    private static bool IsSecretary(string? role) => RoleIs(role, CouncilMemberRole.Secretary);

    /// <summary>
    /// Có tên trong hội đồng là CHƯA đủ — phải đã xác nhận lời mời mới được chấm/soạn/duyệt biên bản.
    /// Người mới gán (ASSIGNED), chưa trả lời (INVITED), đã từ chối (DECLINED) hay quá hạn (EXPIRED)
    /// đều không có tư cách tham gia (rule #13: quá hạn/từ chối thì Staff đi tìm người thay).
    /// </summary>
    private static void AssertConfirmed(CouncilMember member)
    {
        if (!RoleIs(member.Status, CouncilMemberStatus.Confirmed))
            throw new ForbiddenException(
                "Bạn chưa xác nhận tham gia hội đồng này (trạng thái: " +
                $"{member.Status ?? "chưa rõ"}), nên chưa thể chấm điểm hoặc soạn biên bản.");
    }

    /// <summary>
    /// Bien ban cua MOT de tai. Thieu loc thi hoi dong nhieu de tai tra ve bien ban cua de tai
    /// nao ghi truoc -- man hinh de tai dang cham do lai hien bien ban "da khoa" cua de tai khac,
    /// va giao dien an luon nut soan cua Thu ky.
    /// </summary>
    public async Task<CouncilDecisionDto?> GetDecisionAsync(Guid councilId, Guid? projectId = null)
    {
        var pid = await ResolveProjectIdAsync(councilId, projectId);
        var decision = await _review.Decisions
            .Include(d => d.QaEntries)
            .Include(d => d.MemberOpinions)
            .FirstOrDefaultAsync(d => d.CouncilId == councilId && d.ProjectId == pid);
        return decision == null ? null : MapDecision(decision);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static RubricTemplateDto MapTemplate(Domain.Entities.Financial.RubricTemplate t) => new()
    {
        Id = t.Id,
        TemplateType = t.TemplateType,
        Name = t.Name,
        MaxTotalScore = t.MaxTotalScore,
        IsActive = t.IsActive,
        Criteria = t.Criteria.OrderBy(c => c.Sequence).Select(c => new RubricCriterionDto
        {
            Id = c.Id,
            CriterionName = c.CriterionName,
            MaxScore = c.MaxScore,
            Sequence = c.Sequence
        }).ToList()
    };

    private static ReviewScoreDto MapScore(Domain.Entities.Review.ProposalReviewScore s, Guid userId) => new()
    {
        Id = s.Id,
        CouncilId = s.CouncilId,
        EvaluatorMemberId = s.EvaluatorMemberId,
        EvaluatorName = s.EvaluatorMember?.User?.FullName ?? userId.ToString(),
        TemplateId = s.TemplateId,
        TotalScore = s.ScoreDetails.Sum(d => d.GivenScore),
        MaxPossibleScore = s.Template?.MaxTotalScore ?? 0m,
        IsValidBallot = s.IsValidBallot,
        GeneralComments = s.GeneralComments,
        OtherRecommendations = s.OtherRecommendations,
        SubmittedAt = s.SubmittedAt,
        ScoreDetails = s.ScoreDetails.Select(d => new ReviewScoreDetailDto
        {
            Id = d.Id,
            CriterionId = d.CriterionId,
            CriterionName = d.Criterion?.CriterionName ?? "—",
            MaxScore = d.Criterion?.MaxScore ?? 0m,
            GivenScore = d.GivenScore,
            Comments = d.Comments
        }).ToList()
    };

    private static CouncilDecisionDto MapDecision(CouncilDecision d) => new()
    {
        Id = d.Id,
        CouncilId = d.CouncilId,
        TotalMembers = d.TotalMembers,
        AttendingMembers = d.AttendingMembers,
        ValidBallots = d.ValidBallots,
        InvalidBallots = d.InvalidBallots,
        AverageScore = d.AverageScore,
        Result = d.Result,
        CouncilComments = d.CouncilComments,
        Recommendations = d.Recommendations,
        FinalizedAt = d.FinalizedAt,
        RevisionRequestNote = d.RevisionRequestNote,
        RevisionRequestedAt = d.RevisionRequestedAt,
        QaEntries = d.QaEntries.OrderBy(q => q.Order).Select(q => new QaEntryDto
        {
            AskedBy = q.AskedBy,
            Question = q.Question,
            Answer = q.Answer,
            Order = q.Order
        }).ToList(),
        MemberOpinions = d.MemberOpinions.OrderBy(o => o.Order).Select(o => new MemberOpinionDto
        {
            MemberName = o.MemberName,
            AcademicComment = o.AcademicComment,
            BudgetComment = o.BudgetComment,
            Order = o.Order
        }).ToList()
    };

    private async Task<ReviewScoreDto> BuildScoreDto(
        Domain.Entities.Review.ProposalReviewScore score,
        Domain.Entities.Financial.RubricTemplate template,
        Guid userId)
    {
        var details = await _review.ReviewScoreDetails
            .Include(d => d.Criterion)
            .Where(d => d.ScoreId == score.Id)
            .ToListAsync();

        score.ScoreDetails = details;
        score.Template = template;

        return new ReviewScoreDto
        {
            Id = score.Id,
            CouncilId = score.CouncilId,
            EvaluatorMemberId = score.EvaluatorMemberId,
            EvaluatorName = userId.ToString(),
            TemplateId = score.TemplateId,
            TotalScore = details.Sum(d => d.GivenScore),
            MaxPossibleScore = template.MaxTotalScore,
            IsValidBallot = score.IsValidBallot,
            GeneralComments = score.GeneralComments,
            OtherRecommendations = score.OtherRecommendations,
            SubmittedAt = score.SubmittedAt,
            ScoreDetails = details.Select(d => new ReviewScoreDetailDto
            {
                Id = d.Id,
                CriterionId = d.CriterionId,
                CriterionName = d.Criterion?.CriterionName ?? "—",
                MaxScore = d.Criterion?.MaxScore ?? 0m,
                GivenScore = d.GivenScore,
                Comments = d.Comments
            }).ToList()
        };
    }
}
