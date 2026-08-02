using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

/// <summary>
/// "Bộ tiêu chí" chấm (thầy 29/07: tiêu chí chia theo loại đề tài + lĩnh vực, phải linh hoạt).
/// 1 bộ = nhiều tiêu chí; gắn vào nhiều (đợt + lĩnh vực); dùng lại được cho nhiều đợt.
/// Ràng buộc: mỗi (đợt + lĩnh vực + LOẠI VÒNG) chỉ 1 bộ — nhưng cùng lĩnh vực vẫn có bộ riêng
/// cho Xét duyệt và bộ riêng cho Nghiệm thu.
/// </summary>
[ApiController]
[Route("api/rubric-templates")]
[Authorize]
public class RubricTemplatesController : ControllerBase
{
    private readonly IMasterDataRepository _repo;
    private readonly IReviewRepository _review;
    private readonly ICycleRepository _cycles;

    public RubricTemplatesController(IMasterDataRepository repo, IReviewRepository review, ICycleRepository cycles)
    {
        _repo = repo;
        _review = review;
        _cycles = cycles;
    }

    // GET /api/rubric-templates/for-council/{councilId}
    // Reviewer chấm chỉ có councilId → BE tự suy (đợt, lĩnh vực, loại vòng) rồi trả đúng bộ.
    // Không có bộ riêng → bộ mặc định (giống /resolve).
    [HttpGet("for-council/{councilId:guid}")]
    public async Task<IActionResult> ResolveForCouncil(Guid councilId)
    {
        var council = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException("Không tìm thấy hội đồng.");

        var round = council.RoundId.HasValue
            ? await _review.ReviewRounds.FirstOrDefaultAsync(r => r.Id == council.RoundId.Value)
            : null;

        // Thứ tự ưu tiên (để vừa linh hoạt vừa đỡ phải cấu hình từng vòng):
        //   1. Bộ GẮN RIÊNG cho vòng này  → mỗi vòng dùng bộ khác nhau; 1 bộ gắn được nhiều vòng.
        //   2. Bộ theo (đợt + lĩnh vực + loại vòng) → mặc định cho cả đợt, cấu hình 1 lần.
        //   3. Bộ mặc định chung           → không bao giờ kẹt không chấm được.
        if (round?.RubricTemplateId is int overrideId)
        {
            var pinned = await _repo.RubricTemplates
                .Include(t => t.Criteria).Include(t => t.Scopes)
                .FirstOrDefaultAsync(t => t.Id == overrideId && t.IsActive);
            if (pinned != null) return Ok(ApiResponse<object?>.Ok(Map(pinned)));
        }

        var templateType = round?.RoundType ?? council.CouncilType;

        int cycleId = 0, trackId = 0;
        if (round != null)
        {
            var ct = await _cycles.CycleTracks.FirstOrDefaultAsync(x => x.Id == round.CycleTrackId);
            if (ct != null) { cycleId = ct.CycleId; trackId = ct.TrackId; }
        }

        return Ok(ApiResponse<object?>.Ok(await ResolveInternalAsync(cycleId, trackId, templateType)));
    }

    // GET /api/rubric-templates — danh sách bộ + tiêu chí + phạm vi đã gắn
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repo.RubricTemplates
            .Include(t => t.Criteria)
            .Include(t => t.Scopes)
            .OrderBy(t => t.TemplateType).ThenBy(t => t.Name)
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<object>>.Ok(items.Select(Map)));
    }

    // GET /api/rubric-templates/resolve?cycleId=&trackId=&templateType=
    // Bộ áp dụng cho (đợt, lĩnh vực, loại vòng); không có bộ riêng → BỘ MẶC ĐỊNH (bộ chưa gắn
    // phạm vi nào) để không bao giờ kẹt không chấm được.
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] int cycleId, [FromQuery] int trackId, [FromQuery] string templateType)
        => Ok(ApiResponse<object?>.Ok(await ResolveInternalAsync(cycleId, trackId, templateType)));

    private async Task<object?> ResolveInternalAsync(int cycleId, int trackId, string? templateType)
    {
        if (string.IsNullOrWhiteSpace(templateType)) return null;

        var scoped = await _repo.RubricTemplates
            .Include(t => t.Criteria).Include(t => t.Scopes)
            .FirstOrDefaultAsync(t => t.IsActive && t.TemplateType == templateType
                && t.Scopes.Any(s => s.CycleId == cycleId && s.TrackId == trackId));

        // Chưa gắn bộ riêng cho lĩnh vực này → bộ mặc định (bộ chưa gắn phạm vi nào).
        scoped ??= await _repo.RubricTemplates
            .Include(t => t.Criteria).Include(t => t.Scopes)
            .FirstOrDefaultAsync(t => t.IsActive && t.TemplateType == templateType && !t.Scopes.Any());

        return scoped == null ? null : Map(scoped);
    }

    // PATCH /api/rubric-templates/rounds/{roundId} — gắn/gỡ bộ tiêu chí RIÊNG cho 1 vòng.
    // 1 bộ gắn được nhiều vòng; mỗi vòng dùng bộ khác nhau. templateId = null → bỏ gắn riêng,
    // quay về dùng bộ theo (đợt + lĩnh vực).
    [HttpPatch("rounds/{roundId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> SetRoundTemplate(Guid roundId, [FromBody] SetRoundTemplateRequest request)
    {
        var round = await _review.ReviewRounds.FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException("Không tìm thấy vòng chấm.");

        if (request.TemplateId is int id)
        {
            var exists = await _repo.RubricTemplates.AnyAsync(t => t.Id == id && t.IsActive);
            if (!exists) throw new ArgumentException("Bộ tiêu chí không tồn tại hoặc đã tắt.");
        }

        round.RubricTemplateId = request.TemplateId;
        await _review.SaveChangesAsync();
        return Ok(ApiResponse.Ok(request.TemplateId == null
            ? "Đã bỏ gắn bộ riêng — vòng này dùng bộ theo đợt/lĩnh vực."
            : "Đã gắn bộ tiêu chí cho vòng."));
    }

    // PATCH /api/rubric-templates/{id} — đổi tên + loại đề tài áp dụng
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTemplateRequest request)
    {
        var t = await _repo.RubricTemplates.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy bộ tiêu chí.");

        if (!string.IsNullOrWhiteSpace(request.Name)) t.Name = request.Name.Trim();
        if (request.AppliesBasic.HasValue) t.AppliesBasic = request.AppliesBasic.Value;
        if (request.AppliesApplied.HasValue) t.AppliesApplied = request.AppliesApplied.Value;
        if (request.IsActive.HasValue) t.IsActive = request.IsActive.Value;
        if (!t.AppliesBasic && !t.AppliesApplied)
            throw new ArgumentException("Bộ tiêu chí phải áp dụng cho ít nhất 1 loại đề tài.");

        _repo.Update(t);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã lưu bộ tiêu chí."));
    }

    // PUT /api/rubric-templates/{id}/scopes — lưu danh sách (đợt, lĩnh vực) áp dụng bộ này
    [HttpPut("{id:int}/scopes")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> SaveScopes(int id, [FromBody] SaveScopesRequest request)
    {
        var template = await _repo.RubricTemplates
            .Include(t => t.Scopes)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy bộ tiêu chí.");

        var entries = request.Entries ?? new List<ScopeEntry>();

        // Chặn đụng bộ khác: cùng (đợt, lĩnh vực) mà CÙNG loại vòng thì chỉ được 1 bộ.
        var taken = await _repo.RubricTemplateScopes
            .Include(s => s.Template)
            .Where(s => s.TemplateId != id && s.Template.TemplateType == template.TemplateType)
            .Select(s => new { s.CycleId, s.TrackId, s.Template.Name })
            .ToListAsync();

        foreach (var e in entries)
        {
            var clash = taken.FirstOrDefault(x => x.CycleId == e.CycleId && x.TrackId == e.TrackId);
            if (clash != null)
                throw new InvalidOperationException(
                    $"Lĩnh vực này trong đợt đã dùng bộ \"{clash.Name}\" cho cùng loại vòng — gỡ ở bộ đó trước.");
        }

        foreach (var old in template.Scopes.ToList()) _repo.Remove(old);
        foreach (var e in entries.DistinctBy(x => (x.CycleId, x.TrackId)))
            _repo.Add(new RubricTemplateScope { TemplateId = id, CycleId = e.CycleId, TrackId = e.TrackId });

        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã lưu phạm vi áp dụng."));
    }

    // POST /api/rubric-templates/{id}/duplicate — sao chép bộ (kèm tiêu chí) để sửa cho nhanh.
    // KHÔNG copy phạm vi: mỗi (đợt+lĩnh vực+loại vòng) chỉ 1 bộ nên copy sẽ đụng nhau ngay.
    [HttpPost("{id:int}/duplicate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var src = await _repo.RubricTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy bộ tiêu chí.");

        var copy = new RubricTemplate
        {
            TemplateType = src.TemplateType,
            Name = $"Sao chép \"{src.Name}\"",
            MaxTotalScore = src.MaxTotalScore,
            AppliesBasic = src.AppliesBasic,
            AppliesApplied = src.AppliesApplied,
            IsActive = src.IsActive,
            Criteria = src.Criteria
                .Where(c => c.IsActive)
                .OrderBy(c => c.Sequence)
                .Select(c => new RubricCriterion
                {
                    CriterionName = c.CriterionName,
                    MaxScore = c.MaxScore,
                    Sequence = c.Sequence,
                    IsActive = true
                }).ToList()
        };

        _repo.Add(copy);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(Map(copy), "Đã sao chép bộ tiêu chí."));
    }

    private static object Map(RubricTemplate t) => new
    {
        t.Id,
        t.TemplateType,
        t.Name,
        t.MaxTotalScore,
        t.AppliesBasic,
        t.AppliesApplied,
        t.IsActive,
        Criteria = t.Criteria.Where(c => c.IsActive).OrderBy(c => c.Sequence)
            .Select(c => new { c.Id, c.CriterionName, c.MaxScore, c.Sequence }),
        Scopes = t.Scopes.Select(s => new { s.Id, s.CycleId, s.TrackId }),
    };
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public bool? AppliesBasic { get; set; }
    public bool? AppliesApplied { get; set; }
    public bool? IsActive { get; set; }
}

public class ScopeEntry
{
    public int CycleId { get; set; }
    public int TrackId { get; set; }
}

public class SaveScopesRequest
{
    public List<ScopeEntry>? Entries { get; set; }
}

public class SetRoundTemplateRequest
{
    /// <summary>null = bỏ gắn riêng, dùng bộ theo (đợt + lĩnh vực).</summary>
    public int? TemplateId { get; set; }
}
