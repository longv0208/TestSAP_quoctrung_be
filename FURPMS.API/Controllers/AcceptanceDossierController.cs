using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Review;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

/// <summary>
/// Hồ sơ để hội đồng NGHIỆM THU chấm: thông tin đề tài · báo cáo tiến độ · sản phẩm · báo cáo tổng kết.
/// <para>
/// Vì sao có endpoint riêng: các endpoint hợp đồng hiện chỉ mở cho PI/Staff. Mở rộng
/// chúng cho reviewer sẽ nới quyền quá tay (reviewer thấy được hợp đồng của mọi đề tài).
/// Endpoint này gác đúng phạm vi: **chỉ thành viên của hội đồng đó** (hoặc Admin/Staff).
/// </para>
/// </summary>
[ApiController]
[Route("api/councils/{councilId:guid}/proposals/{proposalId:guid}/dossier")]
[Authorize]
public class AcceptanceDossierController : ControllerBase
{
    private readonly FURPMSDbContext _db;

    public AcceptanceDossierController(FURPMSDbContext db) => _db = db;

    [ProducesResponseType(typeof(ApiResponse<AcceptanceDossierDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> Get(Guid councilId, Guid proposalId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            var isMember = await _db.CouncilMembers.AnyAsync(m => m.CouncilId == councilId && m.UserId == userId);
            if (!isMember) throw new ForbiddenException("Bạn không phải thành viên hội đồng này.");
        }

        var proposal = await _db.Proposals.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var dossier = new AcceptanceDossierDto
        {
            Project = await BuildProjectAsync(proposal.ProjectId, proposal.Id)
        };

        // Hợp đồng của đề tài — mốc để lấy báo cáo tiến độ & sản phẩm.
        var contract = await _db.Contracts
            .Where(c => c.ProjectId == proposal.ProjectId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (contract != null)
        {
            dossier.ContractNumber = contract.ContractNumber;
            dossier.ContractStatus = contract.Status;
            dossier.ContractStartDate = contract.StartDate.ToString("yyyy-MM-dd");
            dossier.ContractEndDate = contract.EndDate.ToString("yyyy-MM-dd");
            dossier.ContractSignedAt = contract.SignedAt;
            dossier.ContractTotalAmount = contract.TotalAmount;
            dossier.ContractFiles = await FilesForAsync("Contract", contract.Id.ToString());

            dossier.ProgressReports = await BuildProgressReportsAsync(contract.Id);
        }

        dossier.Deliverables = await BuildDeliverablesAsync(proposal.ProjectId);
        dossier.DeliverablesTotal = dossier.Deliverables.Count;
        dossier.DeliverablesPassed = dossier.Deliverables.Count(d => d.AcceptanceStatus == "PASSED");

        var final = await _db.FinalReports.FirstOrDefaultAsync(f => f.ProjectId == proposal.ProjectId);
        if (final != null)
        {
            dossier.FinalReport = new DossierFinalReportDto
            {
                Status = final.Status,
                SubmittedAt = final.FinalSubmittedAt ?? final.SubmittedAt,
                Deadline = final.Deadline?.ToString("yyyy-MM-dd"),
                HasFile = !string.IsNullOrWhiteSpace(final.ReportFileUrl),
                ReportFileUrl = final.ReportFileUrl,
                SummaryFileUrl = final.SummaryFileUrl,
                // File báo cáo tổng kết được lưu theo HỢP ĐỒNG (xem ProposalDocumentService).
                Files = contract == null
                    ? new List<DossierFileDto>()
                    : await FilesForAsync("FinalReport", contract.Id.ToString())
            };
        }

        return Ok(ApiResponse<AcceptanceDossierDto>.Ok(dossier));
    }

    // ── C4: thông tin đề tài đầy đủ ──────────────────────────────────────────
    private async Task<DossierProjectDto?> BuildProjectAsync(Guid projectId, Guid proposalId)
    {
        var project = await _db.Projects.IgnoreQueryFilters()
            .Where(p => p.Id == projectId)
            .Select(p => new
            {
                p.Id, p.ProjectCode, p.TitleVi, p.TitleEn, p.Status,
                p.PlannedStartDate, p.PlannedEndDate,
                PiName = p.PiUser.FullName,
                PiEmail = p.PiUser.Email,
                UnitName = p.HostingUnit.Name,
                TypeName = p.ResearchType.Name,
                TrackName = p.CycleTrack.Track.Name,
                CycleYear = p.CycleTrack.Cycle.CycleYear,
                CycleCode = p.CycleTrack.Cycle.SemesterCode
            })
            .FirstOrDefaultAsync();
        if (project == null) return null;

        var current = await _db.Proposals.IgnoreQueryFilters()
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.IsCurrent).ThenByDescending(p => p.VersionNo)
            .Select(p => new { p.Id, p.DurationMonths, p.ResearchObjectives, p.Methodology, p.ExpectedOutput })
            .FirstOrDefaultAsync();

        var budget = current == null
            ? null
            : await _db.ProposalBudgets.Where(b => b.ProposalId == current.Id)
                .Select(b => (decimal?)b.TotalAmount).FirstOrDefaultAsync();

        return new DossierProjectDto
        {
            Id = project.Id,
            ProjectCode = project.ProjectCode,
            TitleVi = project.TitleVi,
            TitleEn = project.TitleEn,
            Status = project.Status,
            PiName = project.PiName,
            PiEmail = project.PiEmail,
            HostingUnitName = project.UnitName,
            ResearchTypeName = project.TypeName,
            TrackName = project.TrackName,
            CycleName = $"{project.CycleYear}{(string.IsNullOrWhiteSpace(project.CycleCode) ? "" : $" · {project.CycleCode}")}",
            DurationMonths = current?.DurationMonths ?? 0,
            PlannedStartDate = project.PlannedStartDate.ToString("yyyy-MM-dd"),
            PlannedEndDate = project.PlannedEndDate.ToString("yyyy-MM-dd"),
            TotalBudget = budget,
            ResearchObjectives = current?.ResearchObjectives,
            Methodology = current?.Methodology,
            ExpectedOutput = current?.ExpectedOutput,
            Members = await _db.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .OrderBy(m => m.Sequence)
                .Select(m => new DossierMemberDto
                {
                    FullName = m.FullName,
                    AcademicTitle = m.AcademicTitle,
                    UnitName = m.UnitName,
                    WorkContent = m.WorkContent,
                    IsPi = m.IsPi
                })
                .ToListAsync(),
            ProposalFiles = await FilesForAsync("Proposal", proposalId.ToString())
        };
    }

    // ── C5: từng kỳ báo cáo — ai duyệt, vai gì, duyệt lúc nào, file của chính kỳ đó ──
    private async Task<List<DossierProgressReportDto>> BuildProgressReportsAsync(Guid contractId)
    {
        var reports = await _db.ProgressReports
            .Where(r => r.ContractId == contractId)
            .OrderBy(r => r.ReportRound)
            .Select(r => new DossierProgressReportDto
            {
                Id = r.Id,
                ReportRound = r.ReportRound,
                RoundName = r.RoundName,
                ReportingPeriodStart = r.ReportingPeriodStart.ToString("yyyy-MM-dd"),
                ReportingPeriodEnd = r.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
                OverallCompletionPct = r.OverallCompletionPct,
                Status = r.Status,
                CompletedContent = r.CompletedContent,
                PendingContent = r.PendingContent,
                NextPeriodPlan = r.NextPeriodPlan,
                PiRecommendations = r.PiRecommendations,
                EvaluationResult = r.EvaluationResult,
                EvaluationComments = r.EvaluationComments,
                EvaluatedByName = r.EvaluatedByUser == null ? null : r.EvaluatedByUser.FullName,
                EvaluatedAt = r.EvaluatedAt,
                SubmittedAt = r.SubmittedAt,
                ReportFileUrl = r.ReportFileUrl
            })
            .ToListAsync();
        if (reports.Count == 0) return reports;

        // Vai của người duyệt: lấy một lượt cho tất cả các kỳ, tránh N+1.
        var evaluatorIds = await _db.ProgressReports
            .Where(r => r.ContractId == contractId && r.EvaluatedBy != null)
            .Select(r => new { r.Id, EvaluatedBy = r.EvaluatedBy!.Value })
            .ToListAsync();
        var roles = await _db.UserRoles
            .Where(ur => evaluatorIds.Select(e => e.EvaluatedBy).Contains(ur.UserId))
            .Select(ur => new { ur.UserId, ur.Role.Name })
            .ToListAsync();

        var reportIds = reports.Select(r => r.Id.ToString()).ToList();
        var files = await _db.Documents
            .Where(d => d.EntityType == "ProgressReport" && reportIds.Contains(d.EntityId) && !d.IsDeleted)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();

        foreach (var r in reports)
        {
            var evaluator = evaluatorIds.FirstOrDefault(e => e.Id == r.Id);
            if (evaluator != null)
            {
                var names = roles.Where(x => x.UserId == evaluator.EvaluatedBy).Select(x => x.Name).ToList();
                r.EvaluatedByRole = names.Count > 0 ? string.Join(", ", names) : null;
            }
            r.Files = files.Where(d => d.EntityId == r.Id.ToString()).Select(Map).ToList();
        }
        return reports;
    }

    private async Task<List<DossierDeliverableDto>> BuildDeliverablesAsync(Guid projectId)
    {
        var deliverables = await _db.ProjectDeliverables
            .Where(d => d.ProjectId == projectId)
            .OrderBy(d => d.Sequence).ThenBy(d => d.Id)
            .Select(d => new DossierDeliverableDto
            {
                Id = d.Id,
                ProductName = d.ProductName,
                Description = d.Description,
                ScientificRequirements = d.ScientificRequirements,
                AcceptanceStatus = d.AcceptanceStatus,
                QualityAssessment = d.QualityAssessment,
                SubmittedAt = d.SubmittedAt,
                DueDate = d.DueDate == null ? null : d.DueDate.Value.ToString("yyyy-MM-dd"),
                HasFile = d.FileUrl != null && d.FileUrl != "",
                FileUrl = d.FileUrl,
                TrialEvidenceUrl = d.TrialEvidenceUrl
            })
            .ToListAsync();
        if (deliverables.Count == 0) return deliverables;

        var ids = deliverables.Select(d => d.Id.ToString()).ToList();
        var files = await _db.Documents
            .Where(d => d.EntityType == "Deliverable" && ids.Contains(d.EntityId) && !d.IsDeleted)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();

        foreach (var d in deliverables)
        {
            d.Files = files.Where(f => f.EntityId == d.Id.ToString()).Select(Map).ToList();
            if (d.Files.Count > 0) d.HasFile = true;
        }
        return deliverables;
    }

    private async Task<List<DossierFileDto>> FilesForAsync(string entityType, string entityId)
        => (await _db.Documents
                .Where(d => d.EntityType == entityType && d.EntityId == entityId && !d.IsDeleted)
                .OrderBy(d => d.UploadedAt)
                .ToListAsync())
            .Select(Map).ToList();

    private static DossierFileDto Map(Document d) => new()
    {
        Id = d.Id,
        FileName = FileNames.Normalize(d.OriginalFileName),
        Category = d.DocumentCategory,
        SizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt,
        DownloadUrl = d.StorageUrl
    };
}
