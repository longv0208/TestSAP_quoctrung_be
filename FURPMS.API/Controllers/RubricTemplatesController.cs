using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
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
    private readonly IRubricResolver _resolver;

    public RubricTemplatesController(
        IMasterDataRepository repo, IReviewRepository review, ICycleRepository cycles, IRubricResolver resolver)
    {
        _repo = repo;
        _review = review;
        _cycles = cycles;
        _resolver = resolver;
    }

    // GET /api/rubric-templates/for-council/{councilId}
    // Reviewer chấm chỉ có councilId → BE tự suy (đợt, lĩnh vực, loại vòng) rồi trả đúng bộ.
    // Không có bộ riêng → bộ mặc định (giống /resolve).
    [HttpGet("for-council/{councilId:guid}")]
    public async Task<IActionResult> ResolveForCouncil(Guid councilId)
    {
        // Thứ tự ưu tiên nằm trong IRubricResolver — dùng chung với AI gợi ý chấm điểm,
        // để không có 2 bản logic rồi lệch nhau.
        var template = await _resolver.ResolveForCouncilAsync(councilId);
        return Ok(ApiResponse<object?>.Ok(template == null ? null : Map(template)));
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
        // Màn quản lý: hiện cả tiêu chí đã tắt để còn bật lại được.
        return Ok(ApiResponse<IEnumerable<object>>.Ok(items.Select(t => Map(t, includeInactive: true))));
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

    // ── CRUD bộ tiêu chí ────────────────────────────────────────────────────
    // Trước đây KHÔNG có đường tạo/xoá bộ, và tiêu chí chỉ thêm được qua
    // POST /api/rubric-criteria — endpoint đó tìm bộ bằng LOẠI VÒNG nên luôn rơi vào
    // bộ đầu tiên cùng loại ⇒ bộ sao chép vĩnh viễn rỗng, không thể có 2 bộ REVIEW
    // khác nội dung. Tức là mô hình "1 bộ = 1 hộp tiêu chí, gắn cho đợt/lĩnh vực"
    // không thể dùng được. Nhóm endpoint dưới đây vá đúng chỗ đó.

    private static readonly string[] AllowedTypes = { "REVIEW", "ACCEPTANCE" };

    // POST /api/rubric-templates — tạo bộ MỚI (đặt tên, chọn loại vòng)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên bộ tiêu chí là bắt buộc.");

        var type = (request.TemplateType ?? "").Trim().ToUpperInvariant();
        if (!AllowedTypes.Contains(type))
            throw new ArgumentException("Loại vòng chỉ nhận REVIEW hoặc ACCEPTANCE (rule #16: chỉ 2 hội đồng).");

        if (!request.AppliesBasic && !request.AppliesApplied)
            throw new ArgumentException("Bộ tiêu chí phải áp dụng cho ít nhất 1 loại đề tài.");

        var tpl = new RubricTemplate
        {
            Name = request.Name.Trim(),
            TemplateType = type,
            AppliesBasic = request.AppliesBasic,
            AppliesApplied = request.AppliesApplied,
            IsActive = true
        };
        _repo.Add(tpl);
        await _repo.SaveChangesAsync();

        tpl.Criteria = new List<RubricCriterion>();
        tpl.Scopes = new List<RubricTemplateScope>();
        return Ok(ApiResponse<object>.Ok(Map(tpl)));
    }

    // DELETE /api/rubric-templates/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var tpl = await _repo.RubricTemplates.Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy bộ tiêu chí.");

        // Đã có người chấm bằng bộ này ⇒ xoá là mất luôn lịch sử điểm. Chặn, bảo họ TẮT.
        if (await _review.ReviewScores.AnyAsync(s => s.TemplateId == id))
            throw new InvalidOperationException(
                "Bộ này đã được dùng để chấm điểm — không xoá được (sẽ mất lịch sử). Hãy TẮT bộ thay vì xoá.");

        if (await _review.ReviewRounds.AnyAsync(r => r.RubricTemplateId == id))
            throw new InvalidOperationException(
                "Bộ này đang được gắn riêng cho một vòng chấm. Hãy gỡ khỏi vòng đó trước khi xoá.");

        foreach (var c in tpl.Criteria) _repo.Remove(c);
        _repo.Remove(tpl);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã xoá bộ tiêu chí."));
    }

    // ── Tiêu chí BÊN TRONG một bộ ───────────────────────────────────────────

    // POST /api/rubric-templates/{id}/criteria
    [HttpPost("{id:int}/criteria")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AddCriterion(int id, [FromBody] SaveTemplateCriterionRequest request)
    {
        var tpl = await _repo.RubricTemplates.Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy bộ tiêu chí.");

        if (string.IsNullOrWhiteSpace(request.CriterionName))
            throw new ArgumentException("Tên tiêu chí là bắt buộc.");
        if (request.MaxScore <= 0)
            throw new ArgumentException("Điểm tối đa phải lớn hơn 0.");

        var criterion = new RubricCriterion
        {
            TemplateId = tpl.Id,
            CriterionName = request.CriterionName.Trim(),
            MaxScore = request.MaxScore,
            Sequence = request.Sequence ?? (tpl.Criteria.Count == 0 ? 1 : tpl.Criteria.Max(c => c.Sequence) + 1),
            IsActive = request.IsActive ?? true
        };
        _repo.Add(criterion);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã thêm tiêu chí."));
    }

    // PUT /api/rubric-templates/{id}/criteria/{criterionId}
    [HttpPut("{id:int}/criteria/{criterionId:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateCriterion(
        int id, int criterionId, [FromBody] SaveTemplateCriterionRequest request)
    {
        var c = await _repo.RubricCriteria.FirstOrDefaultAsync(x => x.Id == criterionId && x.TemplateId == id)
            ?? throw new KeyNotFoundException("Tiêu chí không thuộc bộ này.");

        if (!string.IsNullOrWhiteSpace(request.CriterionName)) c.CriterionName = request.CriterionName.Trim();
        if (request.MaxScore > 0) c.MaxScore = request.MaxScore;
        if (request.Sequence.HasValue) c.Sequence = request.Sequence.Value;
        if (request.IsActive.HasValue) c.IsActive = request.IsActive.Value;

        _repo.Update(c);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã lưu tiêu chí."));
    }

    // DELETE /api/rubric-templates/{id}/criteria/{criterionId}
    [HttpDelete("{id:int}/criteria/{criterionId:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> DeleteCriterion(int id, int criterionId)
    {
        var c = await _repo.RubricCriteria.FirstOrDefaultAsync(x => x.Id == criterionId && x.TemplateId == id)
            ?? throw new KeyNotFoundException("Tiêu chí không thuộc bộ này.");

        // Đã có điểm chấm theo tiêu chí này ⇒ chỉ tắt, giữ lịch sử.
        if (await _review.ReviewScoreDetails.AnyAsync(d => d.CriterionId == criterionId))
        {
            c.IsActive = false;
            _repo.Update(c);
            await _repo.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Tiêu chí đã được dùng để chấm nên chỉ TẮT, không xoá (giữ lịch sử điểm)."));
        }

        _repo.Remove(c);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã xoá tiêu chí."));
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

    /// <param name="includeInactive">
    /// Màn QUẢN LÝ cần thấy cả tiêu chí đã tắt để bật lại — trước đây lọc bỏ nên tiêu chí
    /// bị tắt tự động (do đã có điểm chấm) biến mất vĩnh viễn, không có đường khôi phục.
    /// Còn khi RESOLVE để chấm thì chỉ lấy tiêu chí đang bật.
    /// </param>
    private static object Map(RubricTemplate t, bool includeInactive = false) => new
    {
        t.Id,
        t.TemplateType,
        t.Name,
        t.MaxTotalScore,
        t.AppliesBasic,
        t.AppliesApplied,
        t.IsActive,
        Criteria = t.Criteria.Where(c => includeInactive || c.IsActive).OrderBy(c => c.Sequence)
            .Select(c => new { c.Id, c.CriterionName, c.MaxScore, c.Sequence, c.IsActive }),
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

public class CreateTemplateRequest
{
    public string Name { get; set; } = null!;
    /// <summary>REVIEW hoặc ACCEPTANCE (rule #16 — chỉ 2 hội đồng).</summary>
    public string TemplateType { get; set; } = null!;
    public bool AppliesBasic { get; set; } = true;
    public bool AppliesApplied { get; set; } = true;
}

/// <summary>Thêm/sửa tiêu chí NGAY TRONG một bộ cụ thể.</summary>
public class SaveTemplateCriterionRequest
{
    public string CriterionName { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public int? Sequence { get; set; }
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
