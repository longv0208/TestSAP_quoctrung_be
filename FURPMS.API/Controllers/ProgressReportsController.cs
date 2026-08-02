using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/progress-reports")]
[Authorize]
public class ProgressReportsController : ControllerBase
{
    private readonly IProgressReportService _service;

    public ProgressReportsController(IProgressReportService service)
    {
        _service = service;
    }

    // GET /api/progress-reports?contractId={id}
    [HttpGet]
    public async Task<IActionResult> GetByContract([FromQuery] Guid contractId)
    {
        var result = await _service.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<ProgressReportSummaryDto>>.Ok(result));
    }

    // GET /api/progress-reports/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<ProgressReportDto>.Ok(result));
    }

    // POST /api/progress-reports?contractId={id}
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromQuery] Guid contractId,
        [FromBody] CreateProgressReportRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(contractId, request, userId);
        return Ok(ApiResponse<ProgressReportDto>.Ok(result));
    }

    // POST /api/progress-reports/{id}/submit
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.SubmitAsync(id, userId);
        return Ok(ApiResponse<ProgressReportDto>.Ok(result));
    }

    // PATCH /api/progress-reports/{id}/schedule — Staff đặt hạn nộp + lịch họp + link
    [HttpPatch("{id:guid}/schedule")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleProgressReportRequest request)
    {
        var result = await _service.ScheduleAsync(id, request);
        return Ok(ApiResponse<ProgressReportDto>.Ok(result));
    }

    // POST /api/progress-reports/{id}/evaluate
    [HttpPost("{id:guid}/evaluate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Evaluate(Guid id, [FromBody] EvaluateProgressReportRequest request)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.EvaluateAsync(id, request, staffId);
        return Ok(ApiResponse<ProgressReportDto>.Ok(result));
    }
}
