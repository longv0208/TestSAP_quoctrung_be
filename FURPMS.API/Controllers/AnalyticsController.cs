using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

// Lưu ý authz: nhiều [Authorize] là AND — class-level giữ [Authorize] chung, role gating
// đặt ở TỪNG endpoint (dashboard faculty/reviewer cần role khác Admin/Staff).
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly FURPMSDbContext _db;

    public AnalyticsController(FURPMSDbContext db) => _db = db;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("overview")]
    [Authorize(Roles = "Admin,Staff")]
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
    [Authorize(Roles = "Admin,Staff")]
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
    [Authorize(Roles = "Admin,Staff")]
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

    // ── Dashboard theo vai trò (FE mới: StaffDashboardData / PiDashboardData / ReviewerDashboardData) ──

    // GET /api/analytics/dashboard/staff — tổng quan tác nghiệp cho Staff.
    [HttpGet("dashboard/staff")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> StaffDashboard()
    {
        var totalProposals = await _db.Proposals.CountAsync(p => p.IsCurrent);
        var pendingReview = await _db.Proposals.CountAsync(p => p.IsCurrent && p.Status == "SUBMITTED");
        var formingCouncils = await _db.ReviewCouncils.CountAsync(c => c.Status == "FORMING");
        var upcomingMeetings = await _db.CouncilMeetings.CountAsync(m => m.Status == "SCHEDULED");

        var kpis = new object[]
        {
            new { id = "proposals", label = "Tổng đề xuất",       value = totalProposals },
            new { id = "pending",   label = "Chờ xét duyệt",       value = pendingReview },
            new { id = "councils",  label = "Hội đồng đang lập",   value = formingCouncils },
            new { id = "meetings",  label = "Cuộc họp sắp tới",    value = upcomingMeetings },
        };

        // Tiến độ chấm theo vòng: completed = đề tài đã có kết quả, pending = còn chờ.
        var reviewProgress = await _db.ReviewRounds
            .Select(r => new
            {
                label = r.Dimension + " " + r.RoundType + " #" + r.RoundNumber,
                completed = r.ProjectRounds.Count(pr => pr.Status == "PASSED" || pr.Status == "FAILED"),
                pending = r.ProjectRounds.Count(pr => pr.Status != "PASSED" && pr.Status != "FAILED"),
            })
            .ToListAsync();

        // Điểm TB mỗi hội đồng (điểm/phiếu chỉ để tham khảo — rule #12).
        var scores = await _db.ProposalReviewScores
            .Where(s => s.SubmittedAt != null)
            .Select(s => new { s.CouncilId, Total = s.ScoreDetails.Sum(d => d.GivenScore) })
            .ToListAsync();
        var councilPerformance = scores
            .GroupBy(s => s.CouncilId)
            .Select(g => new { council = "HĐ " + g.Key.ToString().Substring(0, 8), score = Math.Round((double)g.Average(x => x.Total), 1) })
            .ToList();

        var activity = await RecentActivityAsync(null);

        return Ok(ApiResponse<object>.Ok(new { kpis, reviewProgress, councilPerformance, activity }));
    }

    // GET /api/analytics/dashboard/faculty — dashboard PI (theo user đang đăng nhập).
    [HttpGet("dashboard/faculty")]
    [Authorize(Roles = "Faculty,Admin")]
    public async Task<IActionResult> FacultyDashboard()
    {
        var userId = CurrentUserId();
        var myProposals = _db.Proposals.Where(p => p.IsCurrent && p.Project.PiUserId == userId);

        var kpis = new object[]
        {
            new { id = "total",    label = "Đề tài của tôi",   value = await myProposals.CountAsync() },
            new { id = "approved", label = "Đã duyệt",          value = await myProposals.CountAsync(p => p.Status == "APPROVED") },
            new { id = "active",   label = "Đang thực hiện",    value = await _db.Projects.CountAsync(p => p.PiUserId == userId && p.Status == "IN_PROGRESS") },
            new { id = "revision", label = "Cần chỉnh sửa",     value = await myProposals.CountAsync(p => p.Status == "REVISION_REQUIRED") },
        };

        var proposalStatus = await myProposals
            .GroupBy(p => p.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        var soon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var upcomingDeadlines = new object[]
        {
            new { type = "Sản phẩm sắp đến hạn (30 ngày)", count = await _db.ProjectDeliverables.CountAsync(d => d.Project.PiUserId == userId && d.DueDate != null && d.DueDate <= soon && d.AcceptanceStatus == "PENDING") },
            new { type = "Đề tài cần chỉnh sửa",            count = await myProposals.CountAsync(p => p.Status == "REVISION_REQUIRED") },
            new { type = "Báo cáo tiến độ nháp",            count = await _db.ProgressReports.CountAsync(r => r.Contract.Project.PiUserId == userId && r.Status == "DRAFT") },
        };

        var activity = await RecentActivityAsync(userId);

        return Ok(ApiResponse<object>.Ok(new { kpis, proposalStatus, upcomingDeadlines, aiSuggestions = Array.Empty<string>(), activity }));
    }

    // GET /api/analytics/dashboard/reviewer — dashboard thành viên hội đồng (theo user đang đăng nhập).
    [HttpGet("dashboard/reviewer")]
    [Authorize(Roles = "ReviewCommittee,Admin")]
    public async Task<IActionResult> ReviewerDashboard()
    {
        var userId = CurrentUserId();
        var myMemberships = _db.CouncilMembers.Where(m => m.UserId == userId);
        var myCouncilIds = await myMemberships.Select(m => m.CouncilId).ToListAsync();

        var myScores = _db.ProposalReviewScores
            .Where(s => s.EvaluatorMember.UserId == userId && s.SubmittedAt != null);

        var kpis = new object[]
        {
            new { id = "memberships", label = "Hội đồng tham gia", value = await myMemberships.CountAsync() },
            new { id = "invited",     label = "Lời mời chờ trả lời", value = await myMemberships.CountAsync(m => m.Status == "INVITED") },
            new { id = "scored",      label = "Phiếu đã chấm",      value = await myScores.CountAsync() },
            new { id = "deciding",    label = "Hội đồng chưa chốt", value = await _db.ReviewCouncils.CountAsync(c => myCouncilIds.Contains(c.Id) && c.Status != "DECIDED") },
        };

        // Xu hướng chấm theo tháng (6 tháng gần nhất): completed = phiếu tôi đã nộp.
        var since = DateTime.UtcNow.AddMonths(-5);
        var submitted = await myScores
            .Where(s => s.SubmittedAt >= since)
            .Select(s => s.SubmittedAt!.Value)
            .ToListAsync();
        var reviewCompletionTrend = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var month = DateTime.UtcNow.AddMonths(i - 5);
                return new
                {
                    label = $"{month:MM/yyyy}",
                    completed = submitted.Count(d => d.Year == month.Year && d.Month == month.Month),
                    pending = 0,
                };
            })
            .ToList();

        var reviewDecisions = await _db.CouncilDecisions
            .Where(d => myCouncilIds.Contains(d.CouncilId) && d.FinalizedAt != null)
            .GroupBy(d => d.Result)
            .Select(g => new { decision = g.Key, count = g.Count() })
            .ToListAsync();

        var activity = await RecentActivityAsync(userId);

        return Ok(ApiResponse<object>.Ok(new { kpis, reviewCompletionTrend, reviewDecisions, activity }));
    }

    // Hoạt động gần đây từ notifications (userId=null → toàn hệ thống, cho Staff).
    private async Task<List<object>> RecentActivityAsync(Guid? userId)
    {
        var query = _db.Notifications.AsQueryable();
        if (userId.HasValue) query = query.Where(n => n.UserId == userId.Value);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .Select(n => new { n.Id, n.Title, n.NotificationType, n.CreatedAt })
            .ToListAsync();

        return items.Select(n => (object)new
        {
            id = n.Id.ToString(),
            message = n.Title,
            actor = "Hệ thống",
            timestamp = n.CreatedAt.ToString("o"),
            type = MapActivityType(n.NotificationType),
        }).ToList();
    }

    // Map NotificationType → ActivityType của FE ("proposal|review|council|meeting|contract|system").
    private static string MapActivityType(string? notificationType)
    {
        var t = (notificationType ?? "").ToUpperInvariant();
        if (t.Contains("PROPOSAL")) return "proposal";
        if (t.Contains("REVIEW") || t.Contains("ROUND") || t.Contains("SCORE")) return "review";
        if (t.Contains("COUNCIL") || t.Contains("INVIT")) return "council";
        if (t.Contains("MEETING")) return "meeting";
        if (t.Contains("CONTRACT") || t.Contains("DISBURS") || t.Contains("DELIVER")) return "contract";
        return "system";
    }
}
