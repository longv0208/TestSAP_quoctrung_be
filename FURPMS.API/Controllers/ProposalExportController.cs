using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/proposals/{proposalId:guid}/export")]
[Authorize]
public class ProposalExportController : ControllerBase
{
    private readonly IDocumentExportService _exportService;
    private readonly FURPMSDbContext _db;

    public ProposalExportController(IDocumentExportService exportService, FURPMSDbContext db)
    {
        _exportService = exportService;
        _db = db;
    }

    [HttpGet("scientific")]
    public async Task<IActionResult> ExportScientific(Guid proposalId)
    {
        await AuthorizeProposalAccessAsync(proposalId);
        var (content, fileName) = await _exportService.ExportScientificDocAsync(proposalId);
        return File(content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    [HttpGet("budget")]
    public async Task<IActionResult> ExportBudget(Guid proposalId)
    {
        await AuthorizeProposalAccessAsync(proposalId);
        var (content, fileName) = await _exportService.ExportBudgetDocAsync(proposalId);
        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task AuthorizeProposalAccessAsync(Guid proposalId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();

        if (roles.Contains("Admin") || roles.Contains("Staff"))
            return;

        var piId = await _db.Proposals
            .IgnoreQueryFilters()
            .Where(p => p.Id == proposalId)
            .Select(p => (Guid?)p.Project.PiUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Proposal {proposalId} not found.");

        if (piId.ToString() != userId)
            throw new UnauthorizedAccessException("Access denied: not the PI of this proposal.");
    }
}
