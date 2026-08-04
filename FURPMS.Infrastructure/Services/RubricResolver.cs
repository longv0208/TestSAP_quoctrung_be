using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IRubricResolver"/>
public class RubricResolver : IRubricResolver
{
    private readonly FURPMSDbContext _db;

    public RubricResolver(FURPMSDbContext db)
    {
        _db = db;
    }

    public async Task<RubricTemplate?> ResolveForCouncilAsync(Guid councilId, CancellationToken ct = default)
    {
        var council = await _db.ReviewCouncils.FirstOrDefaultAsync(c => c.Id == councilId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy hội đồng.");

        var round = council.RoundId.HasValue
            ? await _db.ReviewRounds.FirstOrDefaultAsync(r => r.Id == council.RoundId.Value, ct)
            : null;

        // 1. Bộ gắn RIÊNG cho vòng này.
        if (round?.RubricTemplateId is int pinnedId)
        {
            var pinned = await WithChildren().FirstOrDefaultAsync(t => t.Id == pinnedId && t.IsActive, ct);
            if (pinned != null) return pinned;
        }

        var templateType = round?.RoundType ?? council.CouncilType;

        int cycleId = 0, trackId = 0;
        if (round != null)
        {
            var ct2 = await _db.CycleTracks.FirstOrDefaultAsync(x => x.Id == round.CycleTrackId, ct);
            if (ct2 != null) { cycleId = ct2.CycleId; trackId = ct2.TrackId; }
        }

        // 2. Bộ theo (đợt + lĩnh vực + loại vòng).
        var scoped = await WithChildren().FirstOrDefaultAsync(
            t => t.IsActive && t.TemplateType == templateType
                 && t.Scopes.Any(s => s.CycleId == cycleId && s.TrackId == trackId), ct);

        // 3. Bộ mặc định chung (chưa gắn phạm vi nào).
        scoped ??= await WithChildren().FirstOrDefaultAsync(
            t => t.IsActive && t.TemplateType == templateType && !t.Scopes.Any(), ct);

        return scoped;
    }

    private IQueryable<RubricTemplate> WithChildren() =>
        _db.RubricTemplates.Include(t => t.Criteria).Include(t => t.Scopes);
}
