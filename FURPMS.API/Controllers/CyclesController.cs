using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Cycles;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/cycles")]
[Authorize]
public class CyclesController : ControllerBase
{
    private readonly ICycleService _cycles;

    public CyclesController(ICycleService cycles)
    {
        _cycles = cycles;
    }

    [HttpGet("research-types")]
    public async Task<IActionResult> GetResearchTypes([FromQuery] bool includeInactive = false)
    {
        var result = await _cycles.GetResearchTypesAsync(includeInactive);
        return Ok(ApiResponse<IEnumerable<ResearchTypeDto>>.Ok(result));
    }

    [HttpPost("research-types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateResearchType([FromBody] CreateResearchTypeRequest request)
    {
        var result = await _cycles.CreateResearchTypeAsync(request);
        return Ok(ApiResponse<ResearchTypeDto>.Ok(result, "Đã tạo loại đề tài."));
    }

    [HttpPut("research-types/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateResearchType(int id, [FromBody] UpdateResearchTypeRequest request)
    {
        var result = await _cycles.UpdateResearchTypeAsync(id, request);
        return Ok(ApiResponse<ResearchTypeDto>.Ok(result));
    }

    [HttpPatch("research-types/{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateResearchType(int id)
    {
        var result = await _cycles.DeactivateResearchTypeAsync(id);
        return Ok(ApiResponse<ResearchTypeDto>.Ok(result, "Đã chuyển vào thùng rác."));
    }

    [HttpPatch("research-types/{id:int}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivateResearchType(int id)
    {
        var result = await _cycles.ReactivateResearchTypeAsync(id);
        return Ok(ApiResponse<ResearchTypeDto>.Ok(result, "Đã khôi phục."));
    }

    [HttpDelete("research-types/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteResearchType(int id)
    {
        await _cycles.DeleteResearchTypeAsync(id);
        return Ok(ApiResponse.Ok("Đã xóa vĩnh viễn loại đề tài."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _cycles.GetCyclesAsync();
        return Ok(ApiResponse<IEnumerable<CycleDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _cycles.GetCycleByIdAsync(id);
        return Ok(ApiResponse<CycleDto>.Ok(result));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCycleRequest request)
    {
        var result = await _cycles.UpdateCycleAsync(id, request);
        return Ok(ApiResponse<CycleDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateCycleRequest request)
    {
        var createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _cycles.CreateCycleAsync(request, createdBy);
        return Ok(ApiResponse<CycleDto>.Ok(result));
    }

    [HttpPost("{id:int}/open")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Open(int id)
    {
        var result = await _cycles.OpenCycleAsync(id);
        return Ok(ApiResponse<CycleDto>.Ok(result));
    }

    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _cycles.CloseCycleAsync(id);
        return Ok(ApiResponse<CycleDto>.Ok(result));
    }

    [HttpGet("tracks")]
    public async Task<IActionResult> GetTracks()
    {
        var result = await _cycles.GetTracksAsync();
        return Ok(ApiResponse<IEnumerable<TrackDto>>.Ok(result));
    }

    [HttpPost("tracks")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateTrack([FromBody] CreateTrackRequest request)
    {
        var result = await _cycles.CreateTrackAsync(request);
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }

    // Lĩnh vực GẮN vào 1 đợt cụ thể (cycle_track) — dùng cho màn "Đợt & Lĩnh vực".
    [HttpGet("{id:int}/tracks")]
    public async Task<IActionResult> GetTracksByCycle(int id)
    {
        var result = await _cycles.GetTracksByCycleAsync(id);
        return Ok(ApiResponse<IEnumerable<TrackDto>>.Ok(result));
    }

    [HttpPost("{id:int}/tracks")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateTrackForCycle(int id, [FromBody] CreateTrackRequest request)
    {
        var result = await _cycles.CreateTrackForCycleAsync(id, request);
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }

    [HttpPut("tracks/{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateTrack(int id, [FromBody] UpdateTrackRequest request)
    {
        var result = await _cycles.UpdateTrackAsync(id, request);
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }

    [HttpPatch("tracks/{id:int}/owner")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AssignTrackOwner(int id, [FromBody] AssignTrackOwnerRequest request)
    {
        Guid? ownerId = string.IsNullOrWhiteSpace(request.OwnerId) ? null : Guid.Parse(request.OwnerId);
        var result = await _cycles.AssignTrackOwnerAsync(id, ownerId);
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }

    [HttpPatch("tracks/{id:int}/deactivate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> DeactivateTrack(int id)
    {
        var result = await _cycles.DeactivateTrackAsync(id);
        return Ok(ApiResponse<TrackDto>.Ok(result));
    }
}
