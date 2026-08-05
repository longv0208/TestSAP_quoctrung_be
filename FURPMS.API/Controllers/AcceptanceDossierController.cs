using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Review;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

/// <summary>
/// Hồ sơ để hội đồng NGHIỆM THU chấm: báo cáo tiến độ · sản phẩm · báo cáo tổng kết.
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

        var dossier = new AcceptanceDossierDto();

        // Hợp đồng của đề tài — mốc để lấy báo cáo tiến độ & sản phẩm.
        var contract = await _db.Contracts
            .Where(c => c.ProjectId == proposal.ProjectId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (contract != null)
        {
            dossier.ContractNumber = contract.ContractNumber;
            dossier.ContractStatus = contract.Status;

            dossier.ProgressReports = await _db.ProgressReports
                .Where(r => r.ContractId == contract.Id)
                .OrderBy(r => r.ReportRound)
                .Select(r => new DossierProgressReportDto
                {
                    ReportRound = r.ReportRound,
                    RoundName = r.RoundName,
                    ReportingPeriodStart = r.ReportingPeriodStart.ToString("yyyy-MM-dd"),
                    ReportingPeriodEnd = r.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
                    OverallCompletionPct = r.OverallCompletionPct,
                    Status = r.Status,
                    EvaluationResult = r.EvaluationResult,
                    EvaluationComments = r.EvaluationComments,
                    SubmittedAt = r.SubmittedAt,
                })
                .ToListAsync();
        }

        dossier.Deliverables = await _db.ProjectDeliverables
            .Where(d => d.ProjectId == proposal.ProjectId)
            .OrderBy(d => d.Sequence).ThenBy(d => d.Id)
            .Select(d => new DossierDeliverableDto
            {
                Id = d.Id,
                ProductName = d.ProductName,
                Description = d.Description,
                AcceptanceStatus = d.AcceptanceStatus,
                QualityAssessment = d.QualityAssessment,
                SubmittedAt = d.SubmittedAt,
                DueDate = d.DueDate == null ? null : d.DueDate.Value.ToString("yyyy-MM-dd"),
                HasFile = d.FileUrl != null && d.FileUrl != "",
            })
            .ToListAsync();

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
            };
        }

        return Ok(ApiResponse<AcceptanceDossierDto>.Ok(dossier));
    }
}
