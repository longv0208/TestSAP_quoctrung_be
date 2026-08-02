using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Proposals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class ProposalContentsController : ControllerBase
{
    private readonly IProposalRepository _proposals;

    public ProposalContentsController(IProposalRepository proposals) => _proposals = proposals;

    // ── Research Contents ──────────────────────────────────────────────────────

    [HttpGet("api/proposals/{proposalId:guid}/research-contents")]
    public async Task<IActionResult> GetContents(Guid proposalId)
    {
        var list = await _proposals.ResearchContents
            .Where(c => c.ProposalId == proposalId)
            .Include(c => c.Activities)
            .OrderBy(c => c.Sequence)
            .Select(c => new ResearchContentDto
            {
                Id = c.Id,
                ProposalId = c.ProposalId,
                ContentNumber = c.ContentNumber,
                Title = c.Title,
                Description = c.Description,
                Sequence = c.Sequence,
                Activities = c.Activities.OrderBy(a => a.Sequence).Select(a => ToActivityDto(a)).ToList(),
            })
            .ToListAsync();
        return Ok(ApiResponse<List<ResearchContentDto>>.Ok(list));
    }

    [HttpPost("api/proposals/{proposalId:guid}/research-contents")]
    public async Task<IActionResult> CreateContent(Guid proposalId, [FromBody] ResearchContentRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var content = new ProposalResearchContent
        {
            ProposalId = proposalId,
            ContentNumber = request.ContentNumber,
            Title = request.Title,
            Description = request.Description,
            Sequence = request.Sequence,
        };
        _proposals.AddResearchContent(content);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ResearchContentDto>.Ok(new ResearchContentDto
        {
            Id = content.Id,
            ProposalId = content.ProposalId,
            ContentNumber = content.ContentNumber,
            Title = content.Title,
            Description = content.Description,
            Sequence = content.Sequence,
        }));
    }

    [HttpPut("api/proposals/{proposalId:guid}/research-contents/{contentId:int}")]
    public async Task<IActionResult> UpdateContent(Guid proposalId, int contentId, [FromBody] ResearchContentRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var content = await _proposals.ResearchContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.ProposalId == proposalId)
            ?? throw new KeyNotFoundException("Research content not found.");

        content.ContentNumber = request.ContentNumber;
        content.Title = request.Title;
        content.Description = request.Description;
        content.Sequence = request.Sequence;
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ResearchContentDto>.Ok(new ResearchContentDto
        {
            Id = content.Id, ProposalId = content.ProposalId, ContentNumber = content.ContentNumber,
            Title = content.Title, Description = content.Description, Sequence = content.Sequence,
        }));
    }

    [HttpDelete("api/proposals/{proposalId:guid}/research-contents/{contentId:int}")]
    public async Task<IActionResult> DeleteContent(Guid proposalId, int contentId)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var content = await _proposals.ResearchContents
            .FirstOrDefaultAsync(c => c.Id == contentId && c.ProposalId == proposalId)
            ?? throw new KeyNotFoundException("Research content not found.");
        _proposals.RemoveResearchContent(content);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── Activities ─────────────────────────────────────────────────────────────

    [HttpPost("api/proposals/{proposalId:guid}/research-contents/{contentId:int}/activities")]
    public async Task<IActionResult> CreateActivity(Guid proposalId, int contentId, [FromBody] ActivityRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var contentExists = await _proposals.ResearchContents
            .AnyAsync(c => c.Id == contentId && c.ProposalId == proposalId);
        if (!contentExists) throw new KeyNotFoundException("Research content not found.");

        var activity = new ProposalActivity
        {
            ContentId = contentId,
            ProposalId = proposalId,
            ActivityName = request.ActivityName,
            ExpectedResult = request.ExpectedResult,
            StartMonth = request.StartMonth,
            EndMonth = request.EndMonth,
            ResponsiblePerson = request.ResponsiblePerson,
            EstimatedCost = request.EstimatedCost,
            Sequence = request.Sequence,
            ActivityType = request.ActivityType ?? "NORMAL",
            RequiresApproval = request.RequiresApproval,
        };
        _proposals.AddActivity(activity);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ActivityDto>.Ok(ToActivityDto(activity)));
    }

    [HttpPut("api/proposals/{proposalId:guid}/activities/{activityId:int}")]
    public async Task<IActionResult> UpdateActivity(Guid proposalId, int activityId, [FromBody] ActivityRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var activity = await _proposals.Activities
            .FirstOrDefaultAsync(a => a.Id == activityId && a.ProposalId == proposalId)
            ?? throw new KeyNotFoundException("Activity not found.");

        activity.ActivityName = request.ActivityName;
        activity.ExpectedResult = request.ExpectedResult;
        activity.StartMonth = request.StartMonth;
        activity.EndMonth = request.EndMonth;
        activity.ResponsiblePerson = request.ResponsiblePerson;
        activity.EstimatedCost = request.EstimatedCost;
        activity.Sequence = request.Sequence;
        activity.ActivityType = request.ActivityType ?? activity.ActivityType;
        activity.RequiresApproval = request.RequiresApproval;
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ActivityDto>.Ok(ToActivityDto(activity)));
    }

    [HttpDelete("api/proposals/{proposalId:guid}/activities/{activityId:int}")]
    public async Task<IActionResult> DeleteActivity(Guid proposalId, int activityId)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var activity = await _proposals.Activities
            .FirstOrDefaultAsync(a => a.Id == activityId && a.ProposalId == proposalId)
            ?? throw new KeyNotFoundException("Activity not found.");
        _proposals.RemoveActivity(activity);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── Expected Products ──────────────────────────────────────────────────────

    // Sản phẩm dự kiến giờ là PROJECT DELIVERABLE (con của project) — route giữ theo proposalId.
    [HttpGet("api/proposals/{proposalId:guid}/expected-products")]
    public async Task<IActionResult> GetExpectedProducts(Guid proposalId)
    {
        var projectId = await ResolveProjectIdAsync(proposalId);
        var list = await _proposals.Deliverables
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Sequence)
            .Select(p => new ExpectedProductDto
            {
                Id = p.Id,
                ProposalId = proposalId,
                CategoryId = p.CategoryId,
                ProductName = p.ProductName,
                ScientificRequirements = p.ScientificRequirements,
                Notes = p.Notes,
                Sequence = p.Sequence,
            })
            .ToListAsync();
        return Ok(ApiResponse<List<ExpectedProductDto>>.Ok(list));
    }

    [HttpPost("api/proposals/{proposalId:guid}/expected-products")]
    public async Task<IActionResult> CreateExpectedProduct(Guid proposalId, [FromBody] ExpectedProductRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var projectIdC = await ResolveProjectIdAsync(proposalId);
        var product = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = projectIdC,
            CategoryId = request.CategoryId,
            ProductName = request.ProductName,
            ScientificRequirements = request.ScientificRequirements,
            Notes = request.Notes,
            Sequence = request.Sequence,
        };
        _proposals.AddDeliverable(product);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ExpectedProductDto>.Ok(new ExpectedProductDto
        {
            Id = product.Id, ProposalId = proposalId, CategoryId = product.CategoryId,
            ProductName = product.ProductName, ScientificRequirements = product.ScientificRequirements,
            Notes = product.Notes, Sequence = product.Sequence,
        }));
    }

    [HttpPut("api/proposals/{proposalId:guid}/expected-products/{productId:int}")]
    public async Task<IActionResult> UpdateExpectedProduct(Guid proposalId, int productId, [FromBody] ExpectedProductRequest request)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var projectIdU = await ResolveProjectIdAsync(proposalId);
        var product = await _proposals.Deliverables
            .FirstOrDefaultAsync(p => p.Id == productId && p.ProjectId == projectIdU)
            ?? throw new KeyNotFoundException("Expected product not found.");

        product.CategoryId = request.CategoryId;
        product.ProductName = request.ProductName;
        product.ScientificRequirements = request.ScientificRequirements;
        product.Notes = request.Notes;
        product.Sequence = request.Sequence;
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse<ExpectedProductDto>.Ok(new ExpectedProductDto
        {
            Id = product.Id, ProposalId = proposalId, CategoryId = product.CategoryId,
            ProductName = product.ProductName, ScientificRequirements = product.ScientificRequirements,
            Notes = product.Notes, Sequence = product.Sequence,
        }));
    }

    [HttpDelete("api/proposals/{proposalId:guid}/expected-products/{productId:int}")]
    public async Task<IActionResult> DeleteExpectedProduct(Guid proposalId, int productId)
    {
        await EnsureProposalOwnerOrStaff(proposalId);
        var projectIdD = await ResolveProjectIdAsync(proposalId);
        var product = await _proposals.Deliverables
            .FirstOrDefaultAsync(p => p.Id == productId && p.ProjectId == projectIdD)
            ?? throw new KeyNotFoundException("Expected product not found.");
        _proposals.RemoveDeliverable(product);
        await _proposals.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Deleted."));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task EnsureProposalOwnerOrStaff(Guid proposalId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Staff")) return;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var proposal = await _proposals.Query().Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Proposal not found.");
        if (proposal.Project.PiUserId != userId)
            throw new UnauthorizedAccessException("You do not own this proposal.");
    }

    private async Task<Guid> ResolveProjectIdAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Proposal not found.");
        return proposal.ProjectId;
    }

    private static ActivityDto ToActivityDto(ProposalActivity a) => new()
    {
        Id = a.Id,
        ContentId = a.ContentId,
        ProposalId = a.ProposalId,
        ActivityName = a.ActivityName,
        ExpectedResult = a.ExpectedResult,
        StartMonth = a.StartMonth,
        EndMonth = a.EndMonth,
        ResponsiblePerson = a.ResponsiblePerson,
        EstimatedCost = a.EstimatedCost,
        Sequence = a.Sequence,
        ActivityType = a.ActivityType,
        RequiresApproval = a.RequiresApproval,
    };
}

public class ResearchContentDto
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int ContentNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Sequence { get; set; }
    public List<ActivityDto> Activities { get; set; } = new();
}

public class ResearchContentRequest
{
    public int ContentNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Sequence { get; set; }
}

public class ActivityDto
{
    public int Id { get; set; }
    public int ContentId { get; set; }
    public Guid ProposalId { get; set; }
    public string ActivityName { get; set; } = null!;
    public string ExpectedResult { get; set; } = null!;
    public int StartMonth { get; set; }
    public int EndMonth { get; set; }
    public string? ResponsiblePerson { get; set; }
    public decimal EstimatedCost { get; set; }
    public int Sequence { get; set; }
    public string ActivityType { get; set; } = null!;
    public bool RequiresApproval { get; set; }
}

public class ActivityRequest
{
    public string ActivityName { get; set; } = null!;
    public string ExpectedResult { get; set; } = null!;
    public int StartMonth { get; set; }
    public int EndMonth { get; set; }
    public string? ResponsiblePerson { get; set; }
    public decimal EstimatedCost { get; set; }
    public int Sequence { get; set; }
    public string? ActivityType { get; set; }
    public bool RequiresApproval { get; set; }
}

public class ExpectedProductDto
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int? CategoryId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ScientificRequirements { get; set; }
    public string? Notes { get; set; }
    public int Sequence { get; set; }
}

public class ExpectedProductRequest
{
    public int? CategoryId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ScientificRequirements { get; set; }
    public string? Notes { get; set; }
    public int Sequence { get; set; }
}
