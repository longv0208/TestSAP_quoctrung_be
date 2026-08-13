using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/councils/{councilId:guid}/feedback")]
public class ReviewerFeedbackController : ControllerBase
{
    private readonly IReviewRepository _review;

    public ReviewerFeedbackController(IReviewRepository review) => _review = review;

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid councilId)
    {
        // Admin/Staff xem được mọi hội đồng; ngoài ra phải là THÀNH VIÊN hội đồng đó
        // (Thư ký cần điểm/feedback để lập biên bản — trước đây bị chặn 403 oan).
        await EnsureAdminStaffOrMemberAsync(councilId);

        var list = await _review.ReviewerFeedbacks
            .Include(f => f.ReviewerMember).ThenInclude(m => m.User)
            .Where(f => f.CouncilId == councilId)
            .Select(f => new ReviewerFeedbackDto
            {
                Id = f.Id,
                CouncilId = f.CouncilId,
                ReviewerMemberId = f.ReviewerMemberId,
                ReviewerName = f.ReviewerMember.User.FullName,
                UrgencyScore = f.UrgencyScore,
                ScientificContributionScore = f.ScientificContributionScore,
                PracticalSignificanceScore = f.PracticalSignificanceScore,
                ActualVsExpectedScore = f.ActualVsExpectedScore,
                OtherComments = f.OtherComments,
                OverallAssessment = f.OverallAssessment,
                SubmittedAt = f.SubmittedAt,
            })
            .ToListAsync();
        return Ok(ApiResponse<List<ReviewerFeedbackDto>>.Ok(list));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid councilId, [FromBody] SubmitReviewerFeedbackRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.CouncilId == councilId && m.UserId == userId)
            ?? throw new KeyNotFoundException("Bạn không thuộc hội đồng này.");

        var existing = await _review.ReviewerFeedbacks
            .FirstOrDefaultAsync(f => f.CouncilId == councilId && f.ReviewerMemberId == member.Id);
        if (existing != null)
            throw new InvalidOperationException("Bạn đã gửi nhận xét cho hội đồng này rồi.");

        var feedback = new ReviewerFeedback
        {
            CouncilId = councilId,
            ReviewerMemberId = member.Id,
            UrgencyScore = request.UrgencyScore,
            ScientificContributionScore = request.ScientificContributionScore,
            PracticalSignificanceScore = request.PracticalSignificanceScore,
            ActualVsExpectedScore = request.ActualVsExpectedScore,
            OtherComments = request.OtherComments,
            OverallAssessment = request.OverallAssessment,
            SubmittedAt = DateTime.UtcNow,
        };

        await _review.AddReviewerFeedbackAsync(feedback);
        await _review.SaveChangesAsync();

        return Ok(ApiResponse<ReviewerFeedbackDto>.Ok(new ReviewerFeedbackDto
        {
            Id = feedback.Id,
            CouncilId = feedback.CouncilId,
            ReviewerMemberId = feedback.ReviewerMemberId,
            UrgencyScore = feedback.UrgencyScore,
            ScientificContributionScore = feedback.ScientificContributionScore,
            PracticalSignificanceScore = feedback.PracticalSignificanceScore,
            ActualVsExpectedScore = feedback.ActualVsExpectedScore,
            OtherComments = feedback.OtherComments,
            OverallAssessment = feedback.OverallAssessment,
            SubmittedAt = feedback.SubmittedAt,
        }));
    }

    /// <summary>Admin/Staff qua tự do; còn lại phải là thành viên hội đồng mới được xem.</summary>
    private async Task EnsureAdminStaffOrMemberAsync(Guid councilId)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToHashSet();
        if (roles.Contains("Admin") || roles.Contains("Staff")) return;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isMember = await _review.CouncilMembers.AnyAsync(m => m.CouncilId == councilId && m.UserId == userId);
        if (!isMember)
            throw new ForbiddenException("Bạn không thuộc hội đồng này.");
    }
}

public class ReviewerFeedbackDto
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ReviewerMemberId { get; set; }
    public string? ReviewerName { get; set; }
    public int? UrgencyScore { get; set; }
    public int? ScientificContributionScore { get; set; }
    public int? PracticalSignificanceScore { get; set; }
    public int? ActualVsExpectedScore { get; set; }
    public string? OtherComments { get; set; }
    public string? OverallAssessment { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class SubmitReviewerFeedbackRequest
{
    public int? UrgencyScore { get; set; }
    public int? ScientificContributionScore { get; set; }
    public int? PracticalSignificanceScore { get; set; }
    public int? ActualVsExpectedScore { get; set; }
    public string? OtherComments { get; set; }
    public string? OverallAssessment { get; set; }
}
