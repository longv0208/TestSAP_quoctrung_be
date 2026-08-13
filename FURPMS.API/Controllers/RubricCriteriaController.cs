using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/rubric-criteria")]
[Authorize]
public class RubricCriteriaController : ControllerBase
{
    private readonly IMasterDataRepository _repo;

    // FE sends roundType as int (1/2/3); map to TemplateType strings
    private static readonly Dictionary<int, string> TypeMap = new()
    {
        { 1, "REVIEW" },
        // 2 = PROGRESS_CHECK đã BỎ (rule #16): báo cáo tiến độ do Staff duyệt thẳng,
        // không lập hội đồng nên không có phiếu chấm. Đã kiểm: ProgressReportService
        // không hề tham chiếu rubric. Giữ số 3 cho ACCEPTANCE để FE cũ không lệch.
        { 3, "ACCEPTANCE" },
    };
    // Reverse: TemplateType → roundType key used by FE (old "ProposalReview" etc.)
    private static readonly Dictionary<string, string> RoundTypeKey = new()
    {
        { "REVIEW",         "ProposalReview" },
        { "ACCEPTANCE",     "Acceptance" },
    };

    public RubricCriteriaController(IMasterDataRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? roundType)
    {
        var query = _repo.RubricCriteria.Include(c => c.Template).AsQueryable();

        if (!string.IsNullOrWhiteSpace(roundType))
            query = query.Where(c => c.Template.TemplateType == roundType);

        var items = await query.OrderBy(c => c.Template.TemplateType).ThenBy(c => c.Sequence).ToListAsync();

        var result = items.Select(c => MapDto(c));
        return Ok(ApiResponse<IEnumerable<object>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SaveCriterionRequest request)
    {
        if (!TypeMap.TryGetValue(request.RoundType, out var templateType))
            throw new ArgumentException(
                "Loại vòng chỉ nhận 1 (Xét duyệt đề cương) hoặc 3 (Nghiệm thu) — " +
                "quy định chỉ có hai hội đồng này.");

        // Find or create the template for this round type
        var template = await _repo.RubricTemplates.FirstOrDefaultAsync(t => t.TemplateType == templateType);
        if (template == null)
        {
            template = new RubricTemplate
            {
                TemplateType = templateType,
                Name = RoundTypeKey.GetValueOrDefault(templateType, templateType),
                IsActive = true,
            };
            _repo.Add(template);
            await _repo.SaveChangesAsync();
        }

        var criterion = new RubricCriterion
        {
            TemplateId = template.Id,
            CriterionName = request.Name,
            MaxScore = request.MaxScore,
            Sequence = request.OrderIndex,
            IsActive = request.IsActive,
        };
        _repo.Add(criterion);
        await _repo.SaveChangesAsync();

        criterion.Template = template;
        return Ok(ApiResponse<object>.Ok(MapDto(criterion)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCriterionRequest request)
    {
        var criterion = await _repo.RubricCriteria.Include(c => c.Template).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy tiêu chí chấm.");

        criterion.CriterionName = request.Name;
        criterion.MaxScore = request.MaxScore;
        criterion.Sequence = request.OrderIndex;
        criterion.IsActive = request.IsActive;

        // Update template type if changed
        if (TypeMap.TryGetValue(request.RoundType, out var templateType) && criterion.Template.TemplateType != templateType)
        {
            var newTemplate = await _repo.RubricTemplates.FirstOrDefaultAsync(t => t.TemplateType == templateType);
            if (newTemplate == null)
            {
                newTemplate = new RubricTemplate { TemplateType = templateType, Name = templateType, IsActive = true };
                _repo.Add(newTemplate);
                await _repo.SaveChangesAsync();
            }
            criterion.TemplateId = newTemplate.Id;
        }

        _repo.Update(criterion);
        await _repo.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(MapDto(criterion)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var criterion = await _repo.RubricCriteria.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy tiêu chí chấm.");

        criterion.IsActive = false;
        _repo.Update(criterion);
        await _repo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Criterion removed."));
    }

    private static object MapDto(RubricCriterion c) => new
    {
        id = c.Id.ToString(),
        roundType = RoundTypeKey.GetValueOrDefault(c.Template?.TemplateType ?? "", c.Template?.TemplateType ?? ""),
        orderIndex = c.Sequence,
        name = c.CriterionName,
        maxScore = (double)c.MaxScore,
        isActive = c.IsActive,
    };
}

public class SaveCriterionRequest
{
    public int RoundType { get; set; }
    public int OrderIndex { get; set; }
    public string Name { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public bool IsActive { get; set; } = true;
}
