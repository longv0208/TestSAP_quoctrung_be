using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Decisions;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IProjectDecisionService"/>
public class ProjectDecisionService : IProjectDecisionService
{
    private const string AttachmentEntityType = "ProjectDecision";

    /// <summary>Trần đề tài quét mỗi lần backfill — để một lần gọi không khoá DB quá lâu.</summary>
    private const int MaxProjectsPerBackfill = 500;

    private readonly FURPMSDbContext _db;
    private readonly IClock _clock;

    public ProjectDecisionService(FURPMSDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỌC
    // ══════════════════════════════════════════════════════════════════════

    public async Task<ProjectDecisionDossierResponse> GetDossierAsync(
        Guid projectId, Guid userId, IEnumerable<string> roles)
    {
        var project = await _db.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề tài.");

        await AssertCanViewAsync(projectId, project.PiUserId, userId, roles);

        var rows = await _db.ProjectDecisions
            .Where(d => d.ProjectId == projectId)
            .Select(d => new
            {
                d.Id, d.DecisionType, d.Result, d.Summary, d.Reason, d.DocumentNo,
                d.SourceEntityType, d.SourceEntityId, d.DecidedBy, d.DecidedByRole, d.DecidedAt,
                DecidedByName = d.DecidedByUser != null ? d.DecidedByUser.FullName : null
            })
            .ToListAsync();

        // Xếp ở bộ nhớ vì thứ tự vòng đời là logic C#, không dịch được sang SQL. Danh sách này là
        // hồ sơ của MỘT đề tài nên chỉ vài chục dòng — không đáng phải nhét vào truy vấn.
        rows = rows
            .OrderBy(r => r.DecidedAt.Date)
            .ThenBy(r => DecisionTypes.LifecycleOrder(r.DecisionType))
            .ThenBy(r => r.DecidedAt)
            .ToList();

        var ids = rows.Select(r => r.Id.ToString()).ToList();

        // Một truy vấn cho toàn bộ file đính kèm thay vì mỗi dòng một truy vấn.
        var attachments = await _db.Documents
            .Where(x => x.EntityType == AttachmentEntityType && ids.Contains(x.EntityId) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id, x.EntityId, x.OriginalFileName, x.StorageUrl, x.DocumentCategory, x.UploadedAt
            })
            .ToListAsync();

        var byDecision = attachments
            .GroupBy(a => a.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(a => new DecisionAttachmentDto
            {
                Id = a.Id,
                FileName = a.OriginalFileName,
                FileUrl = a.StorageUrl,
                DocumentType = a.DocumentCategory,
                UploadedAt = a.UploadedAt
            }).ToList());

        return new ProjectDecisionDossierResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            TitleVi = project.TitleVi,
            TotalCount = rows.Count,
            Decisions = rows.Select(r => new ProjectDecisionDto
            {
                Id = r.Id,
                DecisionType = r.DecisionType,
                Stage = DecisionTypes.StageOf(r.DecisionType),
                Result = r.Result,
                Summary = r.Summary,
                Reason = r.Reason,
                DocumentNo = r.DocumentNo,
                SourceEntityType = r.SourceEntityType,
                SourceEntityId = r.SourceEntityId,
                DecidedBy = r.DecidedBy,
                DecidedByName = r.DecidedByName,
                DecidedByRole = r.DecidedByRole,
                DecidedAt = r.DecidedAt,
                Attachments = byDecision.GetValueOrDefault(r.Id.ToString()) ?? new()
            }).ToList()
        };
    }

    /// <summary>Ai xem được — cùng quy tắc với màn kinh phí và dòng thời gian.</summary>
    private async Task AssertCanViewAsync(
        Guid projectId, Guid piUserId, Guid userId, IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Contains("Admin") || roleSet.Contains("Staff")) return;
        if (piUserId == userId) return;

        var isCouncilMember = await _db.CouncilProjectAssignments
            .Where(a => a.ProjectId == projectId)
            .AnyAsync(a => _db.CouncilMembers
                .Any(m => m.CouncilId == a.CouncilId && m.UserId == userId));
        if (isCouncilMember) return;

        throw new ForbiddenException("Bạn không có quyền xem hồ sơ quyết định của đề tài này.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // DỰNG LẠI HỒ SƠ TỪ DỮ LIỆU CŨ
    // ══════════════════════════════════════════════════════════════════════

    public async Task<BackfillDecisionsResponse> BackfillAsync(Guid? projectId, bool dryRun)
    {
        var result = new BackfillDecisionsResponse { DryRun = dryRun };

        var projectIds = await _db.Projects
            .IgnoreQueryFilters()
            .Where(p => projectId == null || p.Id == projectId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.Id)
            .Take(MaxProjectsPerBackfill)
            .ToListAsync();

        result.ProjectsScanned = projectIds.Count;
        if (projectIds.Count == 0) return result;

        // Nạp trước MỌI khoá đã có trong sổ để nhận diện trùng bằng bộ nhớ, thay vì hỏi DB một lần
        // cho mỗi ứng viên. Đây là thứ khiến chạy lại lần hai vừa nhanh vừa không sinh bản trùng.
        var existing = (await _db.ProjectDecisions
                .Where(d => projectIds.Contains(d.ProjectId))
                .Select(d => new { d.SourceEntityType, d.SourceEntityId, d.DecisionType })
                .ToListAsync())
            .Select(d => Key(d.SourceEntityType, d.SourceEntityId, d.DecisionType))
            .ToHashSet();

        var candidates = new List<ProjectDecision>();
        await CollectProposalsAsync(projectIds, candidates);
        await CollectCouncilDecisionsAsync(projectIds, candidates);
        await CollectProjectRoundsAsync(projectIds, candidates);
        await CollectContractsAsync(projectIds, candidates);
        await CollectAmendmentsAsync(projectIds, candidates);
        await CollectProgressReportsAsync(projectIds, candidates);
        await CollectDeliverablesAsync(projectIds, candidates);
        await CollectDisbursementsAsync(projectIds, candidates);
        await CollectFinalReportsAsync(projectIds, candidates);
        await CollectSettlementsAsync(projectIds, candidates);

        foreach (var c in candidates)
        {
            if (!existing.Add(Key(c.SourceEntityType, c.SourceEntityId, c.DecisionType)))
            {
                result.Skipped++;
                continue;
            }

            result.Created++;
            result.ByType[c.DecisionType] = result.ByType.GetValueOrDefault(c.DecisionType) + 1;
            if (!dryRun) _db.ProjectDecisions.Add(c);
        }

        if (!dryRun && result.Created > 0) await _db.SaveChangesAsync();
        return result;
    }

    private static string Key(string sourceType, string sourceId, string decisionType) =>
        $"{sourceType}|{sourceId}|{decisionType}";

    private ProjectDecision Row(
        Guid projectId, string type, string summary, string sourceType, string sourceId,
        DateTime decidedAt, string? result = null, string? reason = null,
        string? documentNo = null, Guid? decidedBy = null, string? decidedByRole = null) => new()
    {
        ProjectId = projectId,
        DecisionType = type,
        Summary = summary.Length > 1000 ? summary[..1000] : summary,
        Result = result,
        Reason = reason is { Length: > 2000 } ? reason[..2000] : reason,
        DocumentNo = documentNo,
        SourceEntityType = sourceType,
        SourceEntityId = sourceId,
        DecidedBy = decidedBy,
        DecidedByRole = decidedByRole,
        DecidedAt = decidedAt,
        CreatedAt = _clock.UtcNow
    };

    // ── Đề cương ─────────────────────────────────────────────────────────
    private async Task CollectProposalsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.Proposals
            .IgnoreQueryFilters()
            .Where(p => projectIds.Contains(p.ProjectId) && p.SubmittedAt != null)
            .Select(p => new
            {
                p.Id, p.ProjectId, p.VersionNo, p.SubmittedAt, p.TitleVi,
                PiUserId = p.Project.PiUserId
            })
            .ToListAsync();

        foreach (var p in rows)
        {
            // Bản v1 là "nộp lần đầu"; v2 trở đi là "nộp bản chỉnh sửa" — hai việc khác nhau
            // trong mắt hội đồng, nên tách loại chứ không gộp thành một.
            var isFirst = p.VersionNo <= 1;
            into.Add(Row(
                p.ProjectId,
                isFirst ? DecisionTypes.ProposalSubmitted : DecisionTypes.ProposalRevised,
                isFirst
                    ? $"Chủ nhiệm nộp đề cương \"{p.TitleVi}\""
                    : $"Chủ nhiệm nộp bản chỉnh sửa lần {p.VersionNo}",
                "Proposal", p.Id.ToString(), p.SubmittedAt!.Value,
                result: "SUBMITTED",
                decidedBy: p.PiUserId, decidedByRole: "Chủ nhiệm đề tài"));
        }
    }

    // ── Kết luận hội đồng ────────────────────────────────────────────────
    private async Task CollectCouncilDecisionsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.CouncilDecisions
            .Where(d => projectIds.Contains(d.ProjectId) && d.FinalizedAt != null)
            .Select(d => new
            {
                d.Id, d.ProjectId, d.Result, d.FinalizedAt, d.ChairUserId,
                d.CouncilComments, d.AverageScore,
                ChairName = d.ChairUser != null ? d.ChairUser.FullName : null
            })
            .ToListAsync();

        foreach (var d in rows)
        {
            var score = d.AverageScore is { } s ? $" (điểm trung bình {s:0.##})" : "";
            into.Add(Row(
                d.ProjectId, DecisionTypes.CouncilDecision,
                $"Hội đồng chốt kết luận: {VietnameseResult(d.Result)}{score}",
                "CouncilDecision", d.Id.ToString(), d.FinalizedAt!.Value,
                result: d.Result, reason: d.CouncilComments, documentNo: "BM04",
                decidedBy: d.ChairUserId, decidedByRole: "Chủ tịch hội đồng"));
        }
    }

    // ── Kết quả từng vòng ────────────────────────────────────────────────
    private async Task CollectProjectRoundsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.ProjectRounds
            .Where(r => projectIds.Contains(r.ProjectId) && r.FinalizedAt != null
                        && (r.Status == "PASSED" || r.Status == "FAILED"))
            .Select(r => new
            {
                r.Id, r.ProjectId, r.Status, r.Result, r.FinalizedAt,
                RoundNumber = r.Round.RoundNumber, RoundType = r.Round.RoundType
            })
            .ToListAsync();

        foreach (var r in rows)
        {
            into.Add(Row(
                r.ProjectId, DecisionTypes.RoundResult,
                $"{RoundTypeVi(r.RoundType)} — vòng {r.RoundNumber}: {(r.Status == "PASSED" ? "Đạt" : "Không đạt")}",
                "ProjectRound", r.Id.ToString(), r.FinalizedAt!.Value,
                result: r.Status, decidedByRole: "Hội đồng"));
        }
    }

    // ── Hợp đồng ─────────────────────────────────────────────────────────
    private async Task CollectContractsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.Contracts
            .Where(c => projectIds.Contains(c.ProjectId))
            .Select(c => new
            {
                c.Id, c.ProjectId, c.ContractNumber, c.TotalAmount, c.CreatedAt, c.CreatedBy,
                c.SignedAt, c.TerminatedAt, c.TerminatedReason, c.TerminatedBy
            })
            .ToListAsync();

        foreach (var c in rows)
        {
            into.Add(Row(
                c.ProjectId, DecisionTypes.ContractCreated,
                $"Lập hợp đồng {c.ContractNumber} — giá trị {c.TotalAmount:N0} đ",
                "Contract", c.Id.ToString(), c.CreatedAt,
                documentNo: c.ContractNumber, decidedBy: c.CreatedBy, decidedByRole: "Phòng QLKH"));

            if (c.SignedAt is { } signedAt)
                into.Add(Row(
                    c.ProjectId, DecisionTypes.ContractSigned,
                    $"Ký hợp đồng {c.ContractNumber}",
                    "Contract", c.Id.ToString(), signedAt,
                    result: "SIGNED", documentNo: c.ContractNumber,
                    decidedByRole: "Hai bên ký kết"));

            if (c.TerminatedAt is { } terminatedAt)
                into.Add(Row(
                    c.ProjectId, DecisionTypes.ContractTerminated,
                    $"Chấm dứt hợp đồng {c.ContractNumber}",
                    "Contract", c.Id.ToString(), terminatedAt,
                    result: "TERMINATED", reason: c.TerminatedReason,
                    documentNo: c.ContractNumber, decidedBy: c.TerminatedBy));
        }
    }

    // ── Phụ lục điều chỉnh ───────────────────────────────────────────────
    private async Task CollectAmendmentsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.AmendmentRequests
            .Where(a => projectIds.Contains(a.Contract.ProjectId) && a.ReviewedAt != null)
            .Select(a => new
            {
                a.Id, ProjectId = a.Contract.ProjectId, a.Status, a.ChangeDescription,
                a.ReviewedAt, a.ReviewedBy, a.ReviewerComments
            })
            .ToListAsync();

        foreach (var a in rows)
        {
            into.Add(Row(
                a.ProjectId, DecisionTypes.AmendmentApproved,
                $"Xét phụ lục điều chỉnh: {a.ChangeDescription}",
                "AmendmentRequest", a.Id.ToString(), a.ReviewedAt!.Value,
                result: a.Status, reason: a.ReviewerComments,
                decidedBy: a.ReviewedBy, decidedByRole: "Phòng QLKH"));
        }
    }

    // ── Báo cáo tiến độ ──────────────────────────────────────────────────
    private async Task CollectProgressReportsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.ProgressReports
            .Where(r => projectIds.Contains(r.Contract.ProjectId) && r.EvaluatedAt != null)
            .Select(r => new
            {
                r.Id, ProjectId = r.Contract.ProjectId, r.ReportRound, r.RoundName,
                r.EvaluationResult, r.EvaluationComments, r.EvaluatedAt, r.EvaluatedBy
            })
            .ToListAsync();

        foreach (var r in rows)
        {
            var name = string.IsNullOrWhiteSpace(r.RoundName) ? $"kỳ {r.ReportRound}" : r.RoundName;
            into.Add(Row(
                r.ProjectId, DecisionTypes.ProgressEvaluated,
                $"Đánh giá báo cáo tiến độ {name}: {VietnameseResult(r.EvaluationResult)}",
                "ProgressReport", r.Id.ToString(), r.EvaluatedAt!.Value,
                result: r.EvaluationResult, reason: r.EvaluationComments,
                documentNo: "BM06", decidedBy: r.EvaluatedBy, decidedByRole: "Phòng QLKH"));
        }
    }

    // ── Nghiệm thu sản phẩm ──────────────────────────────────────────────
    private async Task CollectDeliverablesAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.ProjectDeliverables
            .Where(d => projectIds.Contains(d.ProjectId) && d.AcceptanceStatus != null)
            .Select(d => new
            {
                d.Id, d.ProjectId, d.ProductName, d.AcceptanceStatus,
                d.QualityAssessment, d.SubmittedAt
            })
            .ToListAsync();

        foreach (var d in rows)
        {
            // Không có cột "nghiệm thu lúc nào" — lấy mốc nộp làm xấp xỉ và NÓI RÕ trong lý do,
            // để người đọc hồ sơ không tưởng đó là ngày hội đồng kết luận.
            into.Add(Row(
                d.ProjectId, DecisionTypes.DeliverableAccepted,
                $"Nghiệm thu sản phẩm \"{d.ProductName}\": {VietnameseResult(d.AcceptanceStatus)}",
                "ProjectDeliverable", d.Id.ToString(),
                d.SubmittedAt ?? _clock.UtcNow,
                result: d.AcceptanceStatus, decidedByRole: "Hội đồng nghiệm thu",
                reason: Combine(d.QualityAssessment,
                    "Mốc thời gian dựng lại từ ngày nộp sản phẩm (dữ liệu có trước khi có sổ quyết định).")));
        }
    }

    // ── Giải ngân ────────────────────────────────────────────────────────
    private async Task CollectDisbursementsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.ContractDisbursements
            .Where(x => projectIds.Contains(x.Contract.ProjectId) && x.DisbursedAt != null)
            .Select(x => new
            {
                x.Id, ProjectId = x.Contract.ProjectId, x.RoundNumber, x.Percentage,
                x.PlannedAmount, x.ActualAmount, x.DisbursedAt, x.ProcessedBy, x.BankReference
            })
            .ToListAsync();

        foreach (var x in rows)
        {
            var amount = x.ActualAmount ?? x.PlannedAmount;
            var tam = x.ActualAmount is null ? " (tạm tính theo kế hoạch)" : "";
            into.Add(Row(
                x.ProjectId, DecisionTypes.DisbursementConfirmed,
                $"Xác nhận giải ngân đợt {x.RoundNumber} — {x.Percentage:0.##}%, {amount:N0} đ{tam}",
                "ContractDisbursement", x.Id.ToString(), x.DisbursedAt!.Value,
                result: "DISBURSED", reason: x.BankReference,
                decidedBy: x.ProcessedBy, decidedByRole: "Phòng QLKH"));
        }
    }

    // ── Báo cáo tổng kết ─────────────────────────────────────────────────
    private async Task CollectFinalReportsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.FinalReports
            .Where(r => projectIds.Contains(r.ProjectId) && r.ArchivedAt != null)
            .Select(r => new { r.Id, r.ProjectId, r.Status, r.ArchivedAt })
            .ToListAsync();

        foreach (var r in rows)
        {
            into.Add(Row(
                r.ProjectId, DecisionTypes.FinalReportApproved,
                "Duyệt và lưu trữ báo cáo tổng kết đề tài",
                "FinalReport", r.Id.ToString(), r.ArchivedAt!.Value,
                result: r.Status, documentNo: "BM12", decidedByRole: "Phòng QLKH"));
        }
    }

    // ── Quyết toán, thanh lý ─────────────────────────────────────────────
    private async Task CollectSettlementsAsync(List<Guid> projectIds, List<ProjectDecision> into)
    {
        var rows = await _db.ContractSettlements
            .Where(s => projectIds.Contains(s.Contract.ProjectId) && s.SettlementSignedAt != null)
            .Select(s => new
            {
                s.Id, ProjectId = s.Contract.ProjectId, s.SettlementSignedAt,
                s.SideASigneeId, s.TotalDisbursedAmount, s.TotalReturnedAmount
            })
            .ToListAsync();

        foreach (var s in rows)
        {
            var tra = s.TotalReturnedAmount > 0 ? $", hoàn trả {s.TotalReturnedAmount:N0} đ" : "";
            into.Add(Row(
                s.ProjectId, DecisionTypes.SettlementSigned,
                $"Ký biên bản thanh lý — đã chi {s.TotalDisbursedAmount:N0} đ{tra}",
                "ContractSettlement", s.Id.ToString(), s.SettlementSignedAt!.Value,
                result: "SIGNED", documentNo: "BM13",
                decidedBy: s.SideASigneeId, decidedByRole: "Đại diện Bên A"));
        }
    }

    private static string RoundTypeVi(string? roundType) => roundType switch
    {
        "SCREENING" => "Sơ loại",
        "REVIEW" => "Xét duyệt",
        "ACCEPTANCE" => "Nghiệm thu",
        null or "" => "Vòng chấm",
        _ => roundType
    };

    // ── Chuyển mã kết luận sang tiếng Việt cho câu tóm tắt ────────────────
    private static string VietnameseResult(string? result) => result switch
    {
        "APPROVED" or "PASSED" or "PASS" => "Đạt",
        "REJECTED" or "FAILED" or "FAIL" => "Không đạt",
        "NEEDS_IMPROVEMENT" => "Cần cải thiện",
        "REVISION_REQUIRED" or "REVISION" => "Yêu cầu chỉnh sửa",
        "SIGNED" => "Đã ký",
        "TERMINATED" => "Đã chấm dứt",
        null or "" => "chưa có kết luận",
        _ => result
    };

    private static string? Combine(string? a, string b) =>
        string.IsNullOrWhiteSpace(a) ? b : $"{a} — {b}";
}
