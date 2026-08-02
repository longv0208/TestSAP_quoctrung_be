using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/final-reports")]
[Authorize]
public class FinalReportsController : ControllerBase
{
    private readonly IFinalReportService _service;

    public FinalReportsController(IFinalReportService service)
    {
        _service = service;
    }

    // GET /api/final-reports/{contractId}
    [HttpGet("{contractId:guid}")]
    public async Task<IActionResult> GetByContract(Guid contractId)
    {
        var result = await _service.GetByContractAsync(contractId);
        return Ok(ApiResponse<FinalReportDto?>.Ok(result));
    }

    // POST /api/final-reports/{contractId}/submit
    [HttpPost("{contractId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid contractId, [FromBody] SubmitFinalReportRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.SubmitAsync(contractId, request, userId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/request-revision
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RequestRevisionRequest request)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.RequestRevisionAsync(id, request, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/accept
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.AcceptAsync(id, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/archive
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ArchiveAsync(id, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }
}
