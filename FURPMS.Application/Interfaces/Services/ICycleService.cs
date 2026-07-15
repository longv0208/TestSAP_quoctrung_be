using FURPMS.Application.DTOs.Cycles;

namespace FURPMS.Application.Interfaces.Services;

public interface ICycleService
{
    Task<IEnumerable<ResearchTypeDto>> GetResearchTypesAsync(bool includeInactive = false);
    Task<ResearchTypeDto> CreateResearchTypeAsync(CreateResearchTypeRequest request);
    Task<ResearchTypeDto> UpdateResearchTypeAsync(int id, UpdateResearchTypeRequest request);
    Task<ResearchTypeDto> DeactivateResearchTypeAsync(int id);
    Task<ResearchTypeDto> ReactivateResearchTypeAsync(int id);
    Task DeleteResearchTypeAsync(int id);
    Task<IEnumerable<CycleDto>> GetCyclesAsync();
    Task<CycleDto> GetCycleByIdAsync(int cycleId);
    Task<CycleDto> CreateCycleAsync(CreateCycleRequest request, Guid createdBy);
    Task<CycleDto> UpdateCycleAsync(int cycleId, CreateCycleRequest request);
    Task<CycleDto> OpenCycleAsync(int cycleId);
    Task<CycleDto> CloseCycleAsync(int cycleId);
    Task<IEnumerable<TrackDto>> GetTracksAsync();
    Task<IEnumerable<TrackDto>> GetTracksByCycleAsync(int cycleId);
    Task<TrackDto> CreateTrackAsync(CreateTrackRequest request);
    Task<TrackDto> CreateTrackForCycleAsync(int cycleId, CreateTrackRequest request);
    Task<TrackDto> UpdateTrackAsync(int trackId, UpdateTrackRequest request);
    Task<TrackDto> AssignTrackOwnerAsync(int trackId, Guid? ownerId);
    Task<TrackDto> DeactivateTrackAsync(int trackId);
}
