using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Cycles;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class CycleService : ICycleService
{
    private readonly ICycleRepository _cycles;
    private readonly IMasterDataRepository _masterData;
    private readonly IProposalRepository _proposals;

    // Chỉ dùng để kiểm "đợt đã có vòng chấm chưa" trước khi cho xoá.
    private readonly IReviewRepository _review;

    private readonly INotifier _notifier;

    public CycleService(ICycleRepository cycles, IMasterDataRepository masterData,
        IProposalRepository proposals, IReviewRepository review,
        INotifier notifier)
    {
        _cycles = cycles;
        _masterData = masterData;
        _proposals = proposals;
        _review = review;
        _notifier = notifier;
    }

    public async Task<IEnumerable<CycleDto>> GetCyclesAsync()
    {
        var cycles = await _cycles.Query()
            .Include(c => c.ResearchType)
            .OrderByDescending(c => c.CycleYear)
            .ThenBy(c => c.SemesterCode)
            .ToListAsync();

        // Đếm lĩnh vực theo TỪNG đợt qua bảng nối cycle_track (Review 2 điểm b).
        var trackCounts = await _cycles.CycleTracks
            .GroupBy(ct => ct.CycleId)
            .Select(g => new { CycleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CycleId, x => x.Count);

        // Ghi đè deadline hiển thị = hạn HIỆU LỰC (sau gia hạn) — trước đây luôn trả hạn gốc dù đã gia hạn.
        var cycleIdStrs = cycles.Select(c => c.Id.ToString()).ToList();
        var exts = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == TargetTypeCycle && cycleIdStrs.Contains(e.TargetId))
            .ToListAsync();
        var extByCycle = exts.GroupBy(e => e.TargetId).ToDictionary(g => g.Key, g => g.ToList());

        return cycles.Select(c =>
        {
            var dto = MapCycle(c, trackCounts.GetValueOrDefault(c.Id));
            ApplyEffectiveDeadline(dto, extByCycle.GetValueOrDefault(c.Id.ToString()));
            return dto;
        }).ToList();
    }

    // Nếu đợt đã gia hạn: SubmissionDeadline = hạn mới nhất, giữ hạn gốc ở OriginalDeadline + đếm số lần.
    private static void ApplyEffectiveDeadline(CycleDto dto, List<DeadlineExtension>? exts)
    {
        if (exts == null || exts.Count == 0) return;
        var latest = exts.OrderByDescending(e => e.CreatedAt).First();
        dto.OriginalDeadline = dto.SubmissionDeadline;
        dto.SubmissionDeadline = latest.NewDeadline.ToString("yyyy-MM-dd");
        dto.ExtensionCount = exts.Count;
    }

    public async Task<CycleDto> GetCycleByIdAsync(int cycleId)
    {
        var cycle = await _cycles.Query()
            .Include(c => c.ResearchType)
            .FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        var tracks = await GetTracksByCycleAsync(cycleId);
        var trackList = tracks.ToList();

        var dto = MapCycle(cycle, trackList.Count, trackList);
        var exts = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == TargetTypeCycle && e.TargetId == cycleId.ToString())
            .ToListAsync();
        ApplyEffectiveDeadline(dto, exts);
        return dto;
    }

    // Lĩnh vực ĐÃ GẮN vào đợt này qua cycle_track (không phải toàn bộ lĩnh vực toàn hệ thống).
    public async Task<IEnumerable<TrackDto>> GetTracksByCycleAsync(int cycleId)
    {
        var links = await _cycles.CycleTracks
            .Where(ct => ct.CycleId == cycleId)
            .Include(ct => ct.Track)
            .ToListAsync();

        return links.Select(ct => MapTrack(ct.Track, cycleId));
    }

    // Tạo lĩnh vực MỚI và gắn luôn vào đợt đang thao tác (tự tạo cycle_track).
    public async Task<TrackDto> CreateTrackForCycleAsync(int cycleId, CreateTrackRequest request)
    {
        _ = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        var track = await CreateTrackInternalAsync(request);

        var alreadyLinked = await _cycles.CycleTracks.AnyAsync(ct => ct.CycleId == cycleId && ct.TrackId == track.Id);
        if (!alreadyLinked)
        {
            await _cycles.AddCycleTrackAsync(new CycleTrack { CycleId = cycleId, TrackId = track.Id });
            await _cycles.SaveChangesAsync();
        }

        return MapTrack(track, cycleId);
    }

    // Gắn 1 lĩnh vực CÓ SẴN vào đợt (đợt tự chọn lĩnh vực nó mở — reuse global track).
    public async Task AttachTrackToCycleAsync(int cycleId, int trackId)
    {
        _ = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");
        _ = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException($"Track {trackId} not found.");

        if (await _cycles.CycleTracks.AnyAsync(ct => ct.CycleId == cycleId && ct.TrackId == trackId))
            throw new InvalidOperationException("Lĩnh vực đã được gắn vào đợt này.");

        await _cycles.AddCycleTrackAsync(new CycleTrack { CycleId = cycleId, TrackId = trackId });
        await _cycles.SaveChangesAsync();
    }

    public async Task DetachTrackFromCycleAsync(int cycleId, int trackId)
    {
        var link = await _cycles.CycleTracks.FirstOrDefaultAsync(ct => ct.CycleId == cycleId && ct.TrackId == trackId)
            ?? throw new KeyNotFoundException("Lĩnh vực không được gắn vào đợt này.");

        // Chặn gỡ nếu đã có đề tài trong (đợt, lĩnh vực) này — tránh vỡ FK / mất dữ liệu.
        if (await _proposals.Projects.IgnoreQueryFilters().AnyAsync(p => p.CycleTrackId == link.Id))
            throw new InvalidOperationException("Lĩnh vực đã có đề tài trong đợt này — không thể gỡ.");

        _cycles.RemoveCycleTrack(link);
        await _cycles.SaveChangesAsync();
    }

    public async Task<IEnumerable<ResearchTypeDto>> GetResearchTypesAsync(bool includeInactive = false)
    {
        var query = _masterData.ResearchTypes.AsQueryable();
        if (!includeInactive) query = query.Where(t => t.IsActive);
        var types = await query.OrderBy(t => t.Id).ToListAsync();
        return types.Select(MapType);
    }

    public async Task<ResearchTypeDto> CreateResearchTypeAsync(CreateResearchTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên loại đề tài là bắt buộc.");

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? request.Name.ToUpperInvariant().Replace(" ", "_")
            : request.Code!.Trim().ToUpperInvariant();

        if (await _masterData.ResearchTypes.AnyAsync(t => t.Code == code))
            throw new InvalidOperationException($"Mã loại '{code}' đã tồn tại.");

        var type = new ResearchType
        {
            Code = code,
            Name = request.Name.Trim(),
            MaxBudgetCap = request.MaxBudgetCap,
            RequireOrderingUnit = request.RequireOrderingUnit,
            RequirePublication = false,
            IsActive = true
        };
        _masterData.Add(type);
        await _masterData.SaveChangesAsync();
        return MapType(type);
    }

    public async Task<ResearchTypeDto> UpdateResearchTypeAsync(int id, UpdateResearchTypeRequest request)
    {
        var type = await _masterData.ResearchTypes.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Loại đề tài {id} không tồn tại.");

        if (!string.IsNullOrWhiteSpace(request.Name)) type.Name = request.Name.Trim();
        if (request.MaxBudgetCap.HasValue) type.MaxBudgetCap = request.MaxBudgetCap.Value;
        if (request.RequireOrderingUnit.HasValue) type.RequireOrderingUnit = request.RequireOrderingUnit.Value;

        _masterData.Update(type);
        await _masterData.SaveChangesAsync();
        return MapType(type);
    }

    public async Task<ResearchTypeDto> DeactivateResearchTypeAsync(int id) => await SetActiveAsync(id, false);
    public async Task<ResearchTypeDto> ReactivateResearchTypeAsync(int id) => await SetActiveAsync(id, true);

    private async Task<ResearchTypeDto> SetActiveAsync(int id, bool active)
    {
        var type = await _masterData.ResearchTypes.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Loại đề tài {id} không tồn tại.");
        type.IsActive = active;
        _masterData.Update(type);
        await _masterData.SaveChangesAsync();
        return MapType(type);
    }

    public async Task DeleteResearchTypeAsync(int id)
    {
        var type = await _masterData.ResearchTypes.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Loại đề tài {id} không tồn tại.");

        // Chỉ xoá vĩnh viễn khi KHÔNG có đợt/đề tài nào tham chiếu (tránh vỡ FK).
        var usedByCycle = await _cycles.Query().AnyAsync(c => c.ResearchTypeId == id);
        var usedByProposal = await _proposals.Projects.IgnoreQueryFilters().AnyAsync(p => p.ResearchTypeId == id);
        if (usedByCycle || usedByProposal)
            throw new InvalidOperationException(
                "Loại đề tài đang được đợt/đề tài sử dụng — chỉ có thể vô hiệu hóa, không thể xoá vĩnh viễn.");

        _masterData.Remove(type);
        await _masterData.SaveChangesAsync();
    }

    private static ResearchTypeDto MapType(ResearchType t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        MaxBudgetCap = t.MaxBudgetCap,
        RequireOrderingUnit = t.RequireOrderingUnit,
        IsActive = t.IsActive
    };

    public async Task<CycleDto> CreateCycleAsync(CreateCycleRequest request, Guid createdBy)
    {
        if (!int.TryParse(request.AcademicYear, out int cycleYear))
            throw new ArgumentException("AcademicYear phải là số năm hợp lệ.");

        if (!DateOnly.TryParse(request.SubmissionStartDate, out var openDate))
            throw new ArgumentException("SubmissionStartDate phải là ngày hợp lệ (yyyy-MM-dd).");

        if (!DateOnly.TryParse(request.SubmissionDeadline, out var deadline))
            throw new ArgumentException("SubmissionDeadline phải là ngày hợp lệ (yyyy-MM-dd).");

        var researchType = await _masterData.ResearchTypes.FirstOrDefaultAsync(t => t.Id == request.ResearchTypeId)
            ?? throw new KeyNotFoundException($"Loại đề tài {request.ResearchTypeId} không tồn tại.");

        var cycle = new ResearchCycle
        {
            CycleYear = cycleYear,
            SemesterCode = request.Name,
            ResearchTypeId = researchType.Id,
            SubmissionOpenDate = openDate,
            SubmissionDeadline = deadline,
            ReviewDeadline = deadline.AddDays(30),
            Description = request.Description,
            Status = CycleStatus.Planning,
            CreatedBy = createdBy
        };

        await _cycles.AddAsync(cycle);
        await _cycles.SaveChangesAsync();

        return await GetCycleByIdAsync(cycle.Id);
    }

    public async Task<CycleDto> UpdateCycleAsync(int cycleId, CreateCycleRequest request)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            cycle.SemesterCode = request.Name;

        if (!string.IsNullOrWhiteSpace(request.AcademicYear) && int.TryParse(request.AcademicYear, out int cycleYear))
            cycle.CycleYear = cycleYear;

        if (request.ResearchTypeId > 0)
        {
            var researchType = await _masterData.ResearchTypes.FirstOrDefaultAsync(t => t.Id == request.ResearchTypeId)
                ?? throw new KeyNotFoundException($"Loại đề tài {request.ResearchTypeId} không tồn tại.");
            cycle.ResearchTypeId = researchType.Id;
        }

        if (!string.IsNullOrWhiteSpace(request.SubmissionStartDate) && DateOnly.TryParse(request.SubmissionStartDate, out var openDate))
            cycle.SubmissionOpenDate = openDate;

        if (!string.IsNullOrWhiteSpace(request.SubmissionDeadline) && DateOnly.TryParse(request.SubmissionDeadline, out var deadline))
        {
            cycle.SubmissionDeadline = deadline;
            cycle.ReviewDeadline = deadline.AddDays(30);
        }

        if (request.Description != null)
            cycle.Description = request.Description;

        cycle.UpdatedAt = DateTime.UtcNow;
        await _cycles.SaveChangesAsync();
        return await GetCycleByIdAsync(cycleId);
    }

    /// <summary>
    /// Xoá một đợt lỡ tạo nhầm. Chỉ xoá được khi **chưa có gì bám vào**: không đề tài, không vòng
    /// chấm, không danh mục đặt hàng do người dùng tạo, chưa từng gia hạn.
    /// <para>
    /// Đợt là gốc của cả cây dữ liệu — lĩnh vực, đề tài, vòng chấm, hội đồng, hợp đồng đều treo
    /// dưới nó. Xoá một đợt đang có đề tài là mất trắng, nên chặn thẳng thay vì xoá lan.
    /// Đợt đã dùng thật thì <b>đóng</b> (`CloseCycleAsync`), không xoá.
    /// </para>
    /// </summary>
    public async Task DeleteCycleAsync(int cycleId)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Không tìm thấy đợt {cycleId}.");

        var trackIds = await _cycles.CycleTracks
            .Where(ct => ct.CycleId == cycleId)
            .Select(ct => ct.Id)
            .ToListAsync();

        var blockers = new List<string>();

        if (trackIds.Count > 0)
        {
            if (await _proposals.Projects.IgnoreQueryFilters().AnyAsync(p => trackIds.Contains(p.CycleTrackId)))
                blockers.Add("đề tài");
            if (await _review.ReviewRounds.AnyAsync(r => trackIds.Contains(r.CycleTrackId)))
                blockers.Add("vòng chấm");
        }
        // Danh mục mặc định do hệ thống tự sinh thì xoá theo được; danh mục Staff tạo tay thì không.
        if (await _cycles.Orders.AnyAsync(o => o.CycleId == cycleId && !o.IsDefault))
            blockers.Add("danh mục đặt hàng");
        var cycleKey = cycleId.ToString();
        if (await _cycles.DeadlineExtensions.AnyAsync(e => e.TargetType == "CYCLE" && e.TargetId == cycleKey))
            blockers.Add("lịch sử gia hạn");

        if (blockers.Count > 0)
            throw new InvalidOperationException(
                $"Đợt \"{CycleLabel(cycle)}\" đã có {string.Join(", ", blockers)} — không xoá được. " +
                "Đợt đã dùng thật thì ĐÓNG lại, không xoá khỏi lịch sử.");

        // Dọn phần hệ thống tự sinh: liên kết lĩnh vực và danh mục mặc định.
        var defaults = await _cycles.Orders.Where(o => o.CycleId == cycleId && o.IsDefault).ToListAsync();
        foreach (var o in defaults) _cycles.RemoveOrder(o);
        var links = await _cycles.CycleTracks.Where(ct => ct.CycleId == cycleId).ToListAsync();
        foreach (var l in links) _cycles.RemoveCycleTrack(l);

        _cycles.Remove(cycle);
        await _cycles.SaveChangesAsync();
    }

    private static string CycleLabel(Domain.Entities.Cycles.ResearchCycle c)
        => string.IsNullOrWhiteSpace(c.SemesterCode) ? c.CycleYear.ToString() : $"{c.SemesterCode} ({c.CycleYear})";

    public async Task<CycleDto> OpenCycleAsync(int cycleId)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        if (cycle.Status == CycleStatus.Open)
            throw new InvalidOperationException("Đợt này đang mở rồi.");

        cycle.Status = CycleStatus.Open;
        cycle.UpdatedAt = DateTime.UtcNow;
        await _cycles.SaveChangesAsync();

        return await GetCycleByIdAsync(cycleId);
    }

    public async Task<CycleDto> CloseCycleAsync(int cycleId)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        if (cycle.Status != CycleStatus.Open)
            throw new InvalidOperationException($"Đợt đang ở trạng thái {StatusText.Vi(cycle.Status)} — chỉ đóng được đợt đang mở.");

        cycle.Status = CycleStatus.Closed;
        cycle.UpdatedAt = DateTime.UtcNow;
        await _cycles.SaveChangesAsync();

        return await GetCycleByIdAsync(cycleId);
    }

    public async Task<IEnumerable<TrackDto>> GetTracksAsync()
    {
        var researchTracks = await _cycles.Tracks
            .Where(t => t.IsActive)
            .ToListAsync();

        return researchTracks.Select(MapTrack);
    }

    public async Task<TrackDto> CreateTrackAsync(CreateTrackRequest request)
    {
        var track = await CreateTrackInternalAsync(request);
        return MapTrack(track);
    }

    private async Task<ResearchTrack> CreateTrackInternalAsync(CreateTrackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên lĩnh vực là bắt buộc.");

        var code = request.Name.Trim().ToUpperInvariant().Replace(" ", "_");
        if (await _cycles.Tracks.AnyAsync(t => t.Code == code))
            throw new InvalidOperationException($"Lĩnh vực với mã '{code}' đã tồn tại — hãy đặt tên khác.");

        Guid? ownerId = null;
        if (!string.IsNullOrWhiteSpace(request.OwnerId))
        {
            if (!Guid.TryParse(request.OwnerId, out var parsed))
                throw new ArgumentException("OwnerId không hợp lệ.");
            ownerId = parsed;
        }

        var track = new ResearchTrack
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = true,
            OwnerId = ownerId,
        };

        _masterData.Add(track);
        await _masterData.SaveChangesAsync();

        return track;
    }

    public async Task<TrackDto> UpdateTrackAsync(int trackId, UpdateTrackRequest request)
    {
        var track = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException($"Track {trackId} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var newCode = request.Name.Trim().ToUpperInvariant().Replace(" ", "_");
            if (newCode != track.Code && await _cycles.Tracks.AnyAsync(t => t.Code == newCode && t.Id != trackId))
                throw new InvalidOperationException($"Lĩnh vực với mã '{newCode}' đã tồn tại — hãy đặt tên khác.");
            track.Name = request.Name.Trim();
            track.Code = newCode;
        }
        if (request.Description != null)
            track.Description = request.Description;
        // Đổi người phụ trách ngay trong form Sửa.
        track.OwnerId = string.IsNullOrWhiteSpace(request.OwnerId) ? null : Guid.Parse(request.OwnerId);

        _masterData.Update(track);
        await _masterData.SaveChangesAsync();
        return MapTrack(track);
    }

    public async Task<TrackDto> AssignTrackOwnerAsync(int trackId, Guid? ownerId)
    {
        var track = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException($"Track {trackId} not found.");

        track.OwnerId = ownerId;
        _masterData.Update(track);
        await _masterData.SaveChangesAsync();
        return MapTrack(track);
    }

    public async Task<TrackDto> DeactivateTrackAsync(int trackId)
    {
        var track = await _cycles.Tracks.FirstOrDefaultAsync(t => t.Id == trackId)
            ?? throw new KeyNotFoundException($"Track {trackId} not found.");

        if (!track.IsActive)
            throw new InvalidOperationException("Lĩnh vực này đã ngừng hoạt động.");

        track.IsActive = false;
        _masterData.Update(track);
        await _masterData.SaveChangesAsync();
        return MapTrack(track);
    }

    // ── Gia hạn deadline đợt (rule tuần 10) — ghi log, deadline gốc giữ nguyên ─────
    private const string TargetTypeCycle = "CYCLE";

    public async Task<DeadlineExtensionDto> ExtendCycleDeadlineAsync(int cycleId, ExtendDeadlineRequest request, Guid createdBy)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Đợt {cycleId} không tồn tại.");
        if (!DateOnly.TryParse(request.NewDeadline, out var newDeadline))
            throw new ArgumentException("NewDeadline phải là ngày hợp lệ (yyyy-MM-dd).");

        var current = await GetEffectiveDeadlineAsync(cycleId, cycle.SubmissionDeadline);
        if (newDeadline <= current)
            throw new ArgumentException($"Ngày gia hạn phải SAU deadline hiện tại ({current:yyyy-MM-dd}).");

        var ext = new DeadlineExtension
        {
            TargetType = TargetTypeCycle,
            TargetId = cycleId.ToString(),
            OldDeadline = current,          // gốc/hiệu lực trước khi gia hạn — KHÔNG đụng cycle.SubmissionDeadline
            NewDeadline = newDeadline,
            Reason = request.Reason,
            CreatedBy = createdBy
        };
        await _cycles.AddDeadlineExtensionAsync(ext);
        await _cycles.SaveChangesAsync();

        // Gia hạn mà không báo thì vô nghĩa: người cần biết nhất là chủ nhiệm đang chạy đua với hạn
        // cũ. Gửi cho mọi chủ nhiệm có đề tài trong đợt này — kể cả người đã nộp, vì rút lại để sửa
        // rồi nộp lại vẫn còn kịp trong thời gian gia hạn.
        var piUserIds = await _proposals.Query().IgnoreQueryFilters()
            .Where(p => p.Project.CycleTrack.CycleId == cycleId)
            .Select(p => p.Project.PiUserId)
            .Distinct()
            .ToListAsync();

        await _notifier.NotifyManyAsync(
            piUserIds,
            "CYCLE_DEADLINE_EXTENDED",
            "Hạn nộp của đợt đã được gia hạn",
            $"Đợt \"{cycle.SemesterCode ?? cycle.CycleYear.ToString()}\" gia hạn nộp đề cương " +
            $"từ {current:dd/MM/yyyy} sang {newDeadline:dd/MM/yyyy}." +
            (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" Lý do: {request.Reason}"),
            actionUrl: "/my-proposals",
            entityType: "ResearchCycle",
            entityId: cycleId.ToString(),
            priority: "HIGH");

        return new DeadlineExtensionDto
        {
            Id = ext.Id,
            OldDeadline = ext.OldDeadline.ToString("yyyy-MM-dd"),
            NewDeadline = ext.NewDeadline.ToString("yyyy-MM-dd"),
            Reason = ext.Reason,
            CreatedAt = ext.CreatedAt
        };
    }

    public async Task<IEnumerable<DeadlineExtensionDto>> GetCycleDeadlineExtensionsAsync(int cycleId)
    {
        var rows = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == TargetTypeCycle && e.TargetId == cycleId.ToString())
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.OldDeadline, e.NewDeadline, e.Reason, e.CreatedAt, Name = e.CreatedByUser.FullName })
            .ToListAsync();

        return rows.Select(e => new DeadlineExtensionDto
        {
            Id = e.Id,
            OldDeadline = e.OldDeadline.ToString("yyyy-MM-dd"),
            NewDeadline = e.NewDeadline.ToString("yyyy-MM-dd"),
            Reason = e.Reason,
            CreatedByName = e.Name,
            CreatedAt = e.CreatedAt
        });
    }

    private async Task<DateOnly> GetEffectiveDeadlineAsync(int cycleId, DateOnly original)
    {
        var latest = await _cycles.DeadlineExtensions
            .Where(e => e.TargetType == TargetTypeCycle && e.TargetId == cycleId.ToString())
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
        return latest?.NewDeadline ?? original;
    }

    private static CycleDto MapCycle(ResearchCycle c, int trackCount = 0, List<TrackDto>? tracks = null) => new()
    {
        Id = c.Id.ToString(),
        Name = c.SemesterCode ?? c.CycleYear.ToString(),
        AcademicYear = c.CycleYear.ToString(),
        Status = c.Status == CycleStatus.Open ? "Open" : c.Status == CycleStatus.Closed ? "Closed" : c.Status,
        ResearchTypeId = c.ResearchTypeId,
        ResearchTypeName = c.ResearchType?.Name ?? string.Empty,
        SubmissionStartDate = c.SubmissionOpenDate.ToString("yyyy-MM-dd"),
        SubmissionDeadline = c.SubmissionDeadline.ToString("yyyy-MM-dd"),
        FundingCap = c.ResearchType?.MaxBudgetCap ?? 0m,
        Description = c.Description,
        CreatedAt = c.CreatedAt,
        TrackCount = trackCount,
        Tracks = tracks ?? new List<TrackDto>()
    };

    private static TrackDto MapTrack(ResearchTrack t, int cycleId = 0) => new()
    {
        Id = t.Id.ToString(),
        CycleId = cycleId.ToString(),
        Name = t.Name,
        Description = t.Description,
        OwnerId = t.OwnerId?.ToString(),
        OwnerName = null,
        IsActive = t.IsActive,
        CreatedAt = DateTime.UtcNow
    };
}
