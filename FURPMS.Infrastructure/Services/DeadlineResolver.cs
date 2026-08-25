using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IDeadlineResolver"/>
public class DeadlineResolver : IDeadlineResolver
{
    private readonly ICycleRepository _cycles;

    public DeadlineResolver(ICycleRepository cycles)
    {
        _cycles = cycles;
    }

    public async Task<DateOnly> EffectiveAsync(string targetType, string targetId, DateOnly original)
    {
        // Bản gia hạn MỚI NHẤT thắng — không phải bản có ngày xa nhất. Gia hạn rồi rút ngắn lại
        // vẫn là một quyết định hành chính hợp lệ, và bảng này là sổ ghi theo thứ tự thời gian.
        var latest = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == targetType && e.TargetId == targetId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => (DateOnly?)e.NewDeadline)
            .FirstOrDefaultAsync();

        return latest ?? original;
    }

    public async Task<IReadOnlyDictionary<string, DateOnly>> EffectiveManyAsync(
        string targetType, IEnumerable<string> targetIds)
    {
        var ids = targetIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, DateOnly>();

        var rows = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == targetType && ids.Contains(e.TargetId))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.TargetId, e.NewDeadline })
            .ToListAsync();

        // Đã sắp mới→cũ, nên bản đầu tiên gặp của mỗi target chính là bản mới nhất.
        var result = new Dictionary<string, DateOnly>();
        foreach (var row in rows)
            result.TryAdd(row.TargetId, row.NewDeadline);

        return result;
    }
}
