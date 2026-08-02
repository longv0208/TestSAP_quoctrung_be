using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/final-reports")]
[Authorize]
public class FinalReportsController : ControllerBase
{
    private readonly IFinalReportService _service;
    private readonly IProposalDocumentService _docs;

    public FinalReportsController(IFinalReportService service, IProposalDocumentService docs)
    {
        _service = service;
        _docs = docs;
    }

    // ── File báo cáo tổng kết (BM09) — upload PDF thay vì dán URL ──────────────

    // GET /api/final-reports/{contractId}/documents
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProposalDocumentDto>>), StatusCodes.Status200OK)]
    [HttpGet("{contractId:guid}/documents")]
    public async Task<IActionResult> ListDocuments(Guid contractId)
    {
        var result = await _docs.ListForFinalReportAsync(contractId);
        return Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(result));
    }

    // POST /api/final-reports/{contractId}/documents
    [ProducesResponseType(typeof(ApiResponse<ProposalDocumentDto>), StatusCodes.Status200OK)]
    [HttpPost("{contractId:guid}/documents")]
    public async Task<IActionResult> UploadDocument(Guid contractId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        var result = await _docs.UploadForFinalReportAsync(
            contractId, stream, file.FileName, file.ContentType, file.Length, userId);
        return Ok(ApiResponse<ProposalDocumentDto>.Ok(result, "Đã tải file lên."));
    }

    // GET /api/final-reports/{contractId}/documents/{documentId}/download
    [HttpGet("{contractId:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid contractId, Guid documentId)
    {
        var (stream, contentType, fileName) = await _docs.DownloadFinalReportDocAsync(documentId);
        return File(stream, contentType, fileName);
    }

    // GET /api/final-reports/{contractId}
    [ProducesResponseType(typeof(ApiResponse<FinalReportDto?>), StatusCodes.Status200OK)]
    [HttpGet("{contractId:guid}")]
    public async Task<IActionResult> GetByContract(Guid contractId)
    {
        var result = await _service.GetByContractAsync(contractId);
        return Ok(ApiResponse<FinalReportDto?>.Ok(result));
    }

    // POST /api/final-reports/{contractId}/submit
    [ProducesResponseType(typeof(ApiResponse<FinalReportDto>), StatusCodes.Status200OK)]
    [HttpPost("{contractId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid contractId, [FromBody] SubmitFinalReportRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.SubmitAsync(contractId, request, userId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/request-revision
    [ProducesResponseType(typeof(ApiResponse<FinalReportDto>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RequestRevisionRequest request)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.RequestRevisionAsync(id, request, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/accept
    [ProducesResponseType(typeof(ApiResponse<FinalReportDto>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.AcceptAsync(id, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }

    // POST /api/final-reports/{id}/archive
    [ProducesResponseType(typeof(ApiResponse<FinalReportDto>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ArchiveAsync(id, staffId);
        return Ok(ApiResponse<FinalReportDto>.Ok(result));
    }
}
