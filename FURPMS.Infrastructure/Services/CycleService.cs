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

    public CycleService(ICycleRepository cycles, IMasterDataRepository masterData, IProposalRepository proposals)
    {
        _cycles = cycles;
        _masterData = masterData;
        _proposals = proposals;
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

        return cycles.Select(c => MapCycle(c, trackCounts.GetValueOrDefault(c.Id)));
    }

    public async Task<CycleDto> GetCycleByIdAsync(int cycleId)
    {
        var cycle = await _cycles.Query()
            .Include(c => c.ResearchType)
            .FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        var tracks = await GetTracksByCycleAsync(cycleId);
        var trackList = tracks.ToList();

        return MapCycle(cycle, trackList.Count, trackList);
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

        // Chỉ xóa vĩnh viễn khi KHÔNG có đợt/đề tài nào tham chiếu (tránh vỡ FK).
        var usedByCycle = await _cycles.Query().AnyAsync(c => c.ResearchTypeId == id);
        var usedByProposal = await _proposals.Projects.IgnoreQueryFilters().AnyAsync(p => p.ResearchTypeId == id);
        if (usedByCycle || usedByProposal)
            throw new InvalidOperationException(
                "Loại đề tài đang được đợt/đề tài sử dụng — chỉ có thể vô hiệu hóa, không thể xóa vĩnh viễn.");

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

    public async Task<CycleDto> OpenCycleAsync(int cycleId)
    {
        var cycle = await _cycles.Query().FirstOrDefaultAsync(c => c.Id == cycleId)
            ?? throw new KeyNotFoundException($"Cycle {cycleId} not found.");

        if (cycle.Status == CycleStatus.Open)
            throw new InvalidOperationException("Cycle is already open.");

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
            throw new InvalidOperationException($"Cycle is '{cycle.Status}'; only OPEN cycles can be closed.");

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
            track.Name = request.Name;
            track.Code = request.Name.ToUpperInvariant().Replace(" ", "_");
        }
        if (request.Description != null)
            track.Description = request.Description;

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
            throw new InvalidOperationException("Track is already inactive.");

        track.IsActive = false;
        _masterData.Update(track);
        await _masterData.SaveChangesAsync();
        return MapTrack(track);
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
