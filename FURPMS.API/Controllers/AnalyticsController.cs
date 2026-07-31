using FURPMS.Application.Common;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin,Staff")]
public class AnalyticsController : ControllerBase
{
    private readonly FURPMSDbContext _db;

    public AnalyticsController(FURPMSDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var proposals = await _db.Proposals.ToListAsync();

        var byStatus = proposals
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var byType = await _db.Proposals
            .GroupBy(p => p.Project.ResearchType.Name)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var totalPIs = await _db.Projects.Select(p => p.PiUserId).Distinct().CountAsync();

        var reviewerRoleId = await _db.Roles
            .Where(r => r.Name == "ReviewCommittee")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var totalReviewers = reviewerRoleId > 0
            ? await _db.UserRoles.Where(ur => ur.RoleId == reviewerRoleId).Select(ur => ur.UserId).Distinct().CountAsync()
            : 0;

        var activeCycle = await _db.ResearchCycles
            .Where(c => c.Status == "OPEN")
            .OrderByDescending(c => c.CycleYear)
            .Select(c => c.SemesterCode ?? c.CycleYear.ToString())
            .FirstOrDefaultAsync();

        var result = new
        {
            totalProposals = proposals.Count,
            totalByStatus = byStatus,
            totalByResearchType = byType,
            totalPIs,
            totalReviewers,
            activeCycle,
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("by-track")]
    public async Task<IActionResult> ByTrack([FromQuery] int? cycleId)
    {
        var query = _db.Proposals.AsQueryable();
        if (cycleId.HasValue)
            query = query.Where(p => p.Project.CycleTrack.CycleId == cycleId.Value);

        var data = await query
            .GroupBy(p => p.Project.CycleTrack.Track.Name)
            .Select(g => new
            {
                trackName = g.Key,
                total = g.Count(),
                passed = g.Count(p => p.Status == "APPROVED" || p.Status == "ACCEPTED" || p.Status == "CONTRACT_SIGNED" || p.Status == "IN_PROGRESS"),
                failed = g.Count(p => p.Status == "REJECTED" || p.Status == "WITHDRAWN"),
                pending = g.Count(p => p.Status == "SUBMITTED" || p.Status == "UNDER_REVIEW"),
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> Funnel([FromQuery] int? cycleId)
    {
        var query = _db.Proposals.AsQueryable();
        if (cycleId.HasValue)
            query = query.Where(p => p.Project.CycleTrack.CycleId == cycleId.Value);

        var counts = await query
            .GroupBy(p => 1)
            .Select(g => new
            {
                submitted = g.Count(p => p.Status != "DRAFT"),
                underReview = g.Count(p => p.Status == "UNDER_REVIEW"),
                approved = g.Count(p => p.Status == "APPROVED" || p.Status == "CONTRACT_SIGNED" || p.Status == "IN_PROGRESS" || p.Status == "ACCEPTANCE_PENDING" || p.Status == "ACCEPTED"),
                contracted = g.Count(p => p.Status == "CONTRACT_SIGNED" || p.Status == "IN_PROGRESS" || p.Status == "ACCEPTANCE_PENDING" || p.Status == "ACCEPTED"),
                inProgress = g.Count(p => p.Status == "IN_PROGRESS" || p.Status == "ACCEPTANCE_PENDING"),
                accepted = g.Count(p => p.Status == "ACCEPTED"),
            })
            .FirstOrDefaultAsync();

        var funnel = new[]
        {
            new { stage = "Đã nộp",    count = counts?.submitted ?? 0 },
            new { stage = "Đang xét",  count = counts?.underReview ?? 0 },
            new { stage = "Đã duyệt",  count = counts?.approved ?? 0 },
            new { stage = "Đã ký HĐ", count = counts?.contracted ?? 0 },
            new { stage = "Thực hiện", count = counts?.inProgress ?? 0 },
            new { stage = "Nghiệm thu",count = counts?.accepted ?? 0 },
        };

        return Ok(ApiResponse<object>.Ok(funnel));
    }
}
