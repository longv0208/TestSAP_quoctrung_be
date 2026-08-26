using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Timeline;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IProjectTimelineService"/>
public class ProjectTimelineService : IProjectTimelineService
{
    /// <summary>Còn ≤ ngần này ngày thì gắn cờ "sắp tới hạn" (vàng) thay vì "đang làm" (xám).</summary>
    private const int AtRiskDays = 7;

    private readonly IProposalRepository _proposals;
    private readonly IContractRepository _contracts;
    private readonly IReviewRepository _review;
    private readonly IDeadlineResolver _deadlines;
    private readonly ISystemSettingService _settings;
    private readonly IClock _clock;

    public ProjectTimelineService(
        IProposalRepository proposals,
        IContractRepository contracts,
        IReviewRepository review,
        IDeadlineResolver deadlines,
        ISystemSettingService settings,
        IClock clock)
    {
        _proposals = proposals;
        _contracts = contracts;
        _review = review;
        _deadlines = deadlines;
        _settings = settings;
        _clock = clock;
    }

    public async Task<ProjectTimelineResponse> GetAsync(
        Guid projectId, Guid userId, IEnumerable<string> roles)
    {
        var project = await _proposals.Projects
            .IgnoreQueryFilters()
            .Include(p => p.CycleTrack).ThenInclude(ct => ct.Cycle)
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề tài.");

        await AssertCanViewAsync(projectId, project.PiUserId, userId, roles);

        var dto = new ProjectTimelineResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            TitleVi = project.TitleVi,
            ProjectStatus = project.Status
        };

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var stages = new List<ProjectStageDto>();

        await AddSubmissionStageAsync(stages, project);
        await AddRevisionStageAsync(stages, projectId);
        await AddReviewStagesAsync(stages, project.CycleTrackId, projectId);
        await AddContractStagesAsync(stages, projectId);

        // Xếp lại theo thứ tự vòng đời rồi mới tính trạng thái — trạng thái phụ thuộc hạn và ngày
        // thực tế của TỪNG giai đoạn, không phụ thuộc thứ tự, nên tính sau cho gọn.
        dto.Stages = stages.OrderBy(s => s.Order).ToList();
        foreach (var stage in dto.Stages) Finalize(stage, today);

        dto.OverdueCount = dto.Stages.Count(s => s.Status == StageStatus.Overdue);
        return dto;
    }

    /// <summary>
    /// Trần số đề tài dựng dòng thời gian trong một lần gọi. Phòng QLKH nhìn toàn hệ thống, mà mỗi
    /// đề tài tốn chục truy vấn nhỏ — không chặn thì màn bảng điều khiển kéo cả trang xuống.
    /// </summary>
    private const int MaxProjectsScanned = 60;

    public async Task<IReadOnlyList<UpcomingDeadlineDto>> GetUpcomingAsync(
        Guid userId, IEnumerable<string> roles, int days)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isManaging = roleSet.Contains("Admin") || roleSet.Contains("Staff");

        // Chỉ nhìn đề tài CÒN SỐNG — đề tài đã hoàn thành/huỷ thì hạn cũ không còn ý nghĩa, để lẫn
        // vào chỉ làm thẻ nhắc việc đầy rác.
        var closed = new[] { ProjectStatus.Completed, ProjectStatus.Cancelled, ProjectStatus.Terminated };

        var query = _proposals.Projects.IgnoreQueryFilters()
            .Where(p => !closed.Contains(p.Status));

        if (!isManaging)
        {
            // Chủ nhiệm thấy đề tài mình; ủy viên hội đồng thấy đề tài mình được gán chấm.
            query = query.Where(p => p.PiUserId == userId
                || _review.ProjectAssignments.Any(a => a.ProjectId == p.Id
                    && _review.CouncilMembers.Any(m => m.CouncilId == a.CouncilId && m.UserId == userId)));
        }

        var projectIds = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => p.Id)
            .Take(MaxProjectsScanned)
            .ToListAsync();

        var horizon = DateOnly.FromDateTime(_clock.UtcNow).AddDays(days);
        var result = new List<UpcomingDeadlineDto>();

        foreach (var projectId in projectIds)
        {
            // Đi qua GetAsync để dùng ĐÚNG một phép tính hạn với dòng thời gian của từng đề tài.
            var timeline = await GetAsync(projectId, userId, roleSet);

            foreach (var stage in timeline.Stages)
            {
                if (stage.Status is StageStatus.Done or StageStatus.NoDeadline) continue;
                if (stage.Deadline is null || !DateOnly.TryParse(stage.Deadline, out var due)) continue;
                // Quá hạn thì luôn hiện, dù đã quá lâu — bỏ qua là để nó chìm luôn.
                if (stage.Status != StageStatus.Overdue && due > horizon) continue;

                result.Add(new UpcomingDeadlineDto
                {
                    ProjectId = timeline.ProjectId,
                    ProjectTitle = timeline.TitleVi,
                    Stage = stage
                });
            }
        }

        // Quá hạn lên đầu, rồi tới hạn gần nhất.
        return result
            .OrderBy(x => x.Stage.DaysLeft ?? int.MaxValue)
            .ToList();
    }

    /// <summary>Ai xem được — giống quy tắc của màn kinh phí: Phòng QLKH/Quản trị · chủ nhiệm · ủy viên hội đồng được gán.</summary>
    private async Task AssertCanViewAsync(
        Guid projectId, Guid piUserId, Guid userId, IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Contains("Admin") || roleSet.Contains("Staff")) return;
        if (piUserId == userId) return;

        var isCouncilMember = await _review.ProjectAssignments
            .Where(a => a.ProjectId == projectId)
            .AnyAsync(a => _review.CouncilMembers
                .Any(m => m.CouncilId == a.CouncilId && m.UserId == userId));
        if (isCouncilMember) return;

        throw new ForbiddenException("Bạn không có quyền xem tiến trình của đề tài này.");
    }

    // ── 1. Nộp đề cương ──────────────────────────────────────────────────────
    private async Task AddSubmissionStageAsync(
        List<ProjectStageDto> stages, Domain.Entities.Projects.Project project)
    {
        var cycle = project.CycleTrack?.Cycle;
        var submittedAt = await _proposals.Query()
            .IgnoreQueryFilters()
            .Where(p => p.ProjectId == project.Id && p.VersionNo == 1)
            .Select(p => p.SubmittedAt)
            .FirstOrDefaultAsync();

        var stage = new ProjectStageDto
        {
            Code = StageCodes.ProposalSubmission,
            Order = 1,
            ActualDate = Fmt(submittedAt),
            EntityType = "Project",
            EntityId = project.Id.ToString()
        };

        if (cycle is not null)
        {
            // Hạn HIỆU LỰC — đã tính gia hạn (rule #19). Đây chính là chỗ trước 25/08 hai nơi trong
            // hệ thống hiểu "hạn" khác nhau, làm chủ nhiệm bị chặn nộp dù đợt đã được gia hạn.
            var effective = await _deadlines.EffectiveAsync(
                IDeadlineResolver.TargetTypeCycle, cycle.Id.ToString(), cycle.SubmissionDeadline);

            var extended = effective != cycle.SubmissionDeadline;
            stage.Deadline = effective.ToString("yyyy-MM-dd");
            stage.IsExtended = extended;
            stage.DeadlineSource = extended ? StageDeadlineSource.Extension : StageDeadlineSource.Cycle;
            stage.DeadlineBasis = extended
                ? $"Hạn nộp của đợt, đã gia hạn từ {cycle.SubmissionDeadline:dd/MM/yyyy}"
                : "Hạn nộp của đợt nghiên cứu";
        }

        stages.Add(stage);
    }

    // ── 2. Nộp bản chỉnh sửa (chỉ hiện khi hội đồng có yêu cầu sửa) ──────────
    private async Task AddRevisionStageAsync(List<ProjectStageDto> stages, Guid projectId)
    {
        var revision = await _proposals.Query()
            .IgnoreQueryFilters()
            .Where(p => p.ProjectId == projectId && p.RevisionRequestedAt != null)
            .OrderByDescending(p => p.VersionNo)
            .Select(p => new { p.Id, p.RevisionRequestedAt, p.RevisionDeadline, p.VersionNo })
            .FirstOrDefaultAsync();
        if (revision is null) return;

        var revisionDays = await _settings.GetIntAsync(
            SystemSettingKeys.RevisionDeadlineDays, SystemSettingKeys.DefaultRevisionDeadlineDays);

        // Bản v+1 đã nộp chưa? Có bản mới hơn nghĩa là chủ nhiệm đã sửa xong.
        var resubmittedAt = await _proposals.Query()
            .IgnoreQueryFilters()
            .Where(p => p.ProjectId == projectId && p.VersionNo > revision.VersionNo)
            .OrderBy(p => p.VersionNo)
            .Select(p => p.SubmittedAt)
            .FirstOrDefaultAsync();

        stages.Add(new ProjectStageDto
        {
            Code = StageCodes.Revision,
            Order = 2,
            Deadline = revision.RevisionDeadline is { } d ? Fmt(d) : null,
            DeadlineSource = revision.RevisionDeadline is null
                ? StageDeadlineSource.NotSet
                : StageDeadlineSource.Derived,
            // ⚠️ In SỐ NGÀY, không in tên khoá cấu hình. `SystemSettingKeys.X` là chuỗi
            // "REVISION_DEADLINE_DAYS" — nội suy thẳng vào đây là người dùng đọc được tên hằng
            // trên màn hình (đã dính khi chạy thử lần đầu 25/08).
            DeadlineBasis = revision.RevisionDeadline is null
                ? null
                : $"{revisionDays} ngày kể từ khi hội đồng yêu cầu sửa",
            ActualDate = Fmt(resubmittedAt),
            EntityType = "Proposal",
            EntityId = revision.Id.ToString()
        });
    }

    // ── 3+4. Chấm từng vòng, và họp hội đồng của vòng đó ─────────────────────
    private async Task AddReviewStagesAsync(List<ProjectStageDto> stages, int cycleTrackId, Guid projectId)
    {
        var rounds = await _review.ReviewRounds
            // Một vòng thuộc chung đợt + lĩnh vực, nhưng timeline thuộc RIÊNG một đề tài.
            // Nếu chỉ lọc CycleTrackId thì mọi vòng của đề tài khác cùng lĩnh vực cũng bị kéo
            // vào timeline (đã thấy thật với NCKH-2026-008: hiện thêm vòng nghiệm thu của 007).
            .Where(r => r.CycleTrackId == cycleTrackId
                     && r.ProjectRounds.Any(pr => pr.ProjectId == projectId))
            .OrderBy(r => r.Sequence).ThenBy(r => r.RoundNumber)
            .Select(r => new { r.Id, r.RoundNumber, r.RoundType, r.Status, r.ScoringDeadline, r.ClosedAt })
            .ToListAsync();
        if (rounds.Count == 0) return;

        // Kết quả của ĐỀ TÀI này trong từng vòng — một vòng dùng chung cho cả lĩnh vực, nên trạng
        // thái của vòng chung không đại diện cho đề tài cụ thể.
        var projectRounds = await _review.ProjectRounds
            .Where(pr => pr.ProjectId == projectId)
            .ToDictionaryAsync(pr => pr.RoundId, pr => new { pr.Status, pr.FinalizedAt });

        var councils = await _review.ProjectAssignments
            .Where(a => a.ProjectId == projectId)
            // IReviewRepository là IRepository<ReviewCouncil> ⇒ Query() chính là bảng hội đồng.
            .Join(_review.Query(), a => a.CouncilId, c => c.Id, (a, c) => c)
            .Select(c => new { c.Id, c.RoundId, c.EstablishedAt, c.MeetingDeadline })
            .ToListAsync();

        var scoringWindowDays = await _settings.GetIntAsync(
            SystemSettingKeys.ScoringWindowDays, SystemSettingKeys.DefaultScoringWindowDays);
        var meetingWorkingDays = await _settings.GetIntAsync(
            SystemSettingKeys.MeetingDeadlineWorkingDays, SystemSettingKeys.DefaultMeetingDeadlineWorkingDays);

        var order = 10;
        foreach (var round in rounds)
        {
            // Codebase so chuỗi thẳng cho RoundType (xem ReviewShared.cs) — giữ đúng lối đó.
            var isAcceptance = round.RoundType == "ACCEPTANCE";
            projectRounds.TryGetValue(round.Id, out var pr);

            var scoring = new ProjectStageDto
            {
                Code = StageCodes.Review,
                Order = order++,
                ActualDate = Fmt(pr?.FinalizedAt ?? round.ClosedAt),
                EntityType = "ReviewRound",
                EntityId = round.Id.ToString()
            };

            if (round.ScoringDeadline is { } sd)
            {
                var effective = await _deadlines.EffectiveAsync(
                    IDeadlineResolver.TargetTypeReviewRound, round.Id.ToString(), sd);
                scoring.Deadline = effective.ToString("yyyy-MM-dd");
                scoring.IsExtended = effective != sd;
                scoring.DeadlineSource = scoring.IsExtended
                    ? StageDeadlineSource.Extension
                    : StageDeadlineSource.Derived;
                scoring.DeadlineBasis = $"Vòng {round.RoundNumber} — {scoringWindowDays} ngày kể từ khi mở vòng";
            }
            else
            {
                // Vòng tạo trước 25/08 chưa có hạn. Nói thẳng "chưa đặt hạn" thay vì bịa ra một ngày.
                scoring.DeadlineBasis = $"Vòng {round.RoundNumber} — chưa đặt hạn chấm";
            }

            stages.Add(scoring);

            var council = councils.FirstOrDefault(c => c.RoundId == round.Id);
            if (council is null) continue;

            var meeting = new ProjectStageDto
            {
                Code = isAcceptance ? StageCodes.AcceptanceMeeting : StageCodes.ReviewMeeting,
                Order = order++,
                ActualDate = Fmt(pr?.FinalizedAt),
                EntityType = "ReviewCouncil",
                EntityId = council.Id.ToString()
            };

            if (council.MeetingDeadline is { } md)
            {
                meeting.Deadline = Fmt(md);
                meeting.DeadlineSource = StageDeadlineSource.RuleQd543;
                meeting.DeadlineBasis = "QĐ543 Điều 8.3.a — hội đồng phải họp trong 15 ngày làm việc kể từ khi được lập";
            }
            else if (council.EstablishedAt is { } est)
            {
                // Suy ra để dòng thời gian không trống hoác, nhưng ghi rõ là SUY RA — hạn thật vẫn
                // chưa ai đặt trên bản ghi hội đồng.
                meeting.Deadline = Fmt(est.AddDays(meetingWorkingDays * 7 / 5));
                meeting.DeadlineSource = StageDeadlineSource.Derived;
                meeting.DeadlineBasis = $"Suy ra: {meetingWorkingDays} ngày làm việc kể từ ngày lập hội đồng (QĐ543 Điều 8.3.a)";
            }

            meeting.DeadlineBasis ??= isAcceptance
                ? "Họp hội đồng nghiệm thu — chưa đặt hạn họp"
                : "Họp hội đồng xét duyệt — chưa đặt hạn họp";

            stages.Add(meeting);
        }
    }

    // ── 5→12. Các giai đoạn gắn với hợp đồng ─────────────────────────────────
    private async Task AddContractStagesAsync(List<ProjectStageDto> stages, Guid projectId)
    {
        var contract = await _contracts.Query()
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        var signWindow = await _settings.GetIntAsync(
            SystemSettingKeys.ContractSignWindowDays, SystemSettingKeys.DefaultContractSignWindowDays);

        // 5. Ký hợp đồng — hạn suy từ ngày hội đồng chốt duyệt.
        var approvedAt = await _review.Decisions
            .Where(d => d.ProjectId == projectId && d.FinalizedAt != null
                        && d.Result == ReviewResult.Approved)
            .OrderBy(d => d.FinalizedAt)
            .Select(d => d.FinalizedAt)
            .FirstOrDefaultAsync();

        var signing = new ProjectStageDto
        {
            Code = StageCodes.ContractSigning,
            Order = 40,
            ActualDate = Fmt(contract?.SignedAt),
            EntityType = contract is null ? null : "Contract",
            EntityId = contract?.Id.ToString()
        };
        if (approvedAt is { } ap)
        {
            signing.Deadline = Fmt(DateOnly.FromDateTime(ap).AddDays(signWindow));
            signing.DeadlineSource = StageDeadlineSource.Derived;
            signing.DeadlineBasis = $"{signWindow} ngày kể từ khi hội đồng chốt duyệt đề cương";
        }
        // Chưa có quyết định duyệt thì chưa suy được hạn — nói rõ thay vì để trống trơn.
        signing.DeadlineBasis ??= "Ký hợp đồng sau khi hội đồng chốt duyệt đề cương";
        stages.Add(signing);

        if (contract is null) return;

        // 6. Báo cáo tiến độ từng kỳ.
        var reports = await _contracts.ProgressReports
            .Where(r => r.ContractId == contract.Id)
            .OrderBy(r => r.ReportRound)
            .Select(r => new { r.Id, r.ReportRound, r.DueDate, r.SubmittedAt })
            .ToListAsync();

        var order = 50;
        foreach (var r in reports)
        {
            stages.Add(new ProjectStageDto
            {
                Code = StageCodes.ProgressReport,
                Order = order++,
                Deadline = r.DueDate is { } due ? Fmt(due) : null,
                DeadlineSource = r.DueDate is null ? StageDeadlineSource.NotSet : StageDeadlineSource.Contract,
                DeadlineBasis = $"Báo cáo tiến độ kỳ {r.ReportRound} (BM06)",
                ActualDate = Fmt(r.SubmittedAt),
                EntityType = "ProgressReport",
                EntityId = r.Id.ToString()
            });
        }

        // 7. Nộp sản phẩm.
        var deliverables = await _contracts.Deliverables
            .Where(d => d.ProjectId == projectId)
            .OrderBy(d => d.DueDate)
            .Select(d => new { d.Id, d.ProductName, d.DueDate, d.SubmittedAt })
            .ToListAsync();

        order = 60;
        foreach (var d in deliverables)
        {
            stages.Add(new ProjectStageDto
            {
                Code = StageCodes.Deliverable,
                Order = order++,
                Deadline = d.DueDate is { } due ? Fmt(due) : null,
                DeadlineSource = d.DueDate is null ? StageDeadlineSource.NotSet : StageDeadlineSource.Contract,
                DeadlineBasis = d.ProductName,
                ActualDate = Fmt(d.SubmittedAt),
                EntityType = "Deliverable",
                EntityId = d.Id.ToString()
            });
        }

        // 8. Giải ngân — CỐ Ý không có hạn: đợt mở khoá theo ĐIỀU KIỆN (nghiệm thu sản phẩm, duyệt
        // báo cáo tiến độ), không theo ngày. Bịa một ngày ở đây là nói dối trên màn hình.
        var tranches = await _contracts.Disbursements
            .Where(t => t.ContractId == contract.Id)
            .OrderBy(t => t.RoundNumber)
            .Select(t => new { t.Id, t.RoundNumber, t.ConditionDescription, t.DisbursedAt })
            .ToListAsync();

        order = 70;
        foreach (var t in tranches)
        {
            stages.Add(new ProjectStageDto
            {
                Code = StageCodes.Disbursement,
                Order = order++,
                DeadlineSource = StageDeadlineSource.NotSet,
                DeadlineBasis = t.ConditionDescription,
                ActualDate = Fmt(t.DisbursedAt),
                EntityType = "Disbursement",
                EntityId = t.Id.ToString()
            });
        }

        // 9 + 12. Báo cáo tổng kết và lưu trữ hồ sơ.
        var final = await _contracts.FinalReports
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new { f.Id, f.Deadline, f.SubmittedAt, f.FinalSubmittedAt, f.ArchivalDeadline })
            .FirstOrDefaultAsync();

        stages.Add(new ProjectStageDto
        {
            Code = StageCodes.FinalReport,
            Order = 80,
            // Chưa nộp thì vẫn suy được hạn từ ngày kết thúc hợp đồng — đây mới là chỗ hạn có ích
            // nhất, vì nó nhắc TRƯỚC khi chủ nhiệm nộp.
            Deadline = Fmt(final?.Deadline ?? contract.EndDate.AddDays(-await FinalReportLeadAsync())),
            DeadlineSource = StageDeadlineSource.RuleQd543,
            DeadlineBasis = "QĐ543 Điều 11.2.a — nộp ít nhất 30 ngày trước khi kết thúc đề tài",
            ActualDate = Fmt(final?.FinalSubmittedAt ?? final?.SubmittedAt),
            EntityType = final is null ? "Contract" : "FinalReport",
            EntityId = (final?.Id ?? contract.Id).ToString()
        });

        // 11. Quyết toán / thanh lý (BM13).
        var settlement = await _contracts.Settlements
            .Where(s => s.ContractId == contract.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.SettlementDeadline, s.SettlementSignedAt })
            .FirstOrDefaultAsync();

        stages.Add(new ProjectStageDto
        {
            Code = StageCodes.Settlement,
            Order = 90,
            Deadline = settlement?.SettlementDeadline is { } sd ? Fmt(sd) : null,
            DeadlineSource = settlement?.SettlementDeadline is null
                ? StageDeadlineSource.NotSet
                : StageDeadlineSource.Contract,
            DeadlineBasis = "Quyết toán và ký Biên bản thanh lý (BM13)",
            ActualDate = Fmt(settlement?.SettlementSignedAt),
            EntityType = "Contract",
            EntityId = contract.Id.ToString()
        });

        if (final?.ArchivalDeadline is { } ad)
        {
            stages.Add(new ProjectStageDto
            {
                Code = StageCodes.Archival,
                Order = 100,
                Deadline = Fmt(ad),
                DeadlineSource = StageDeadlineSource.Derived,
                DeadlineBasis = $"{await ArchivalLeadAsync()} ngày kể từ khi báo cáo tổng kết được tiếp nhận",
                EntityType = "FinalReport",
                EntityId = final.Id.ToString()
            });
        }
    }

    private async Task<int> FinalReportLeadAsync() => await _settings.GetIntAsync(
        SystemSettingKeys.FinalReportLeadDays, SystemSettingKeys.DefaultFinalReportLeadDays);

    private async Task<int> ArchivalLeadAsync() => await _settings.GetIntAsync(
        SystemSettingKeys.ArchivalLeadDays, SystemSettingKeys.DefaultArchivalLeadDays);

    /// <summary>Tính trạng thái + số ngày còn lại. Tách riêng để mọi giai đoạn theo cùng một luật.</summary>
    private static void Finalize(ProjectStageDto stage, DateOnly today)
    {
        if (stage.ActualDate is not null)
        {
            stage.Status = StageStatus.Done;
            return;
        }

        if (stage.Deadline is null || !DateOnly.TryParse(stage.Deadline, out var deadline))
        {
            stage.Status = StageStatus.NoDeadline;
            return;
        }

        var daysLeft = deadline.DayNumber - today.DayNumber;
        stage.DaysLeft = daysLeft;
        stage.Status = daysLeft < 0
            ? StageStatus.Overdue
            : daysLeft <= AtRiskDays
                ? StageStatus.AtRisk
                : StageStatus.InProgress;
    }

    private static string? Fmt(DateTime? value) => value?.ToString("yyyy-MM-dd");
    private static string Fmt(DateOnly value) => value.ToString("yyyy-MM-dd");
}
