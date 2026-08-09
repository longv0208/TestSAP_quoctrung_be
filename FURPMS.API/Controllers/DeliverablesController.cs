using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/deliverables")]
[Authorize]
public class DeliverablesController : ControllerBase
{
    private readonly IDeliverableService _service;
    private readonly IProposalDocumentService _documents;

    public DeliverablesController(IDeliverableService service, IProposalDocumentService documents)
    {
        _service = service;
        _documents = documents;
    }

    // ── File sản phẩm + minh chứng thử nghiệm (QĐ543 Điều 13.1) ───────────────
    // Trước đây PI phải TỰ HOST file rồi dán URL — chỗ cuối cùng còn như vậy.

    /// <param name="trialEvidence">true = minh chứng thử nghiệm, false = bản sản phẩm.</param>
    [ProducesResponseType(typeof(ApiResponse<ProposalDocumentDto>), StatusCodes.Status200OK)]
    [HttpPost("{id:int}/documents")]
    public async Task<IActionResult> UploadDocument(int id, IFormFile file, [FromQuery] bool trialEvidence = false)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("Chưa chọn file.");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        var result = await _documents.UploadForDeliverableAsync(
            id, stream, file.FileName, file.ContentType, file.Length, userId, trialEvidence);
        return Ok(ApiResponse<ProposalDocumentDto>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProposalDocumentDto>>), StatusCodes.Status200OK)]
    [HttpGet("{id:int}/documents")]
    public async Task<IActionResult> ListDocuments(int id)
        => Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(await _documents.ListForDeliverableAsync(id)));

    [HttpGet("{id:int}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(int id, Guid documentId)
    {
        var (stream, contentType, fileName) = await _documents.DownloadDeliverableDocAsync(documentId);
        return File(stream, contentType, fileName);
    }

    [ProducesResponseType(typeof(ApiResponse<DeliverableResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitDeliverableRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.SubmitAsync(id, request, userId);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<DeliverableResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:int}/evaluate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Evaluate(int id, [FromBody] EvaluateDeliverableRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.EvaluateAsync(id, request, userId);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result));
    }

    // Staff sửa/xoá sản phẩm nhập nhầm. Sản phẩm đã nghiệm thu ĐẠT, đã nộp minh chứng, hoặc đang
    // là điều kiện của một đợt giải ngân đều KHÔNG xoá được — xem DeliverableService.
    [ProducesResponseType(typeof(ApiResponse<DeliverableResponse>), StatusCodes.Status200OK)]
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateDeliverableRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result, "Đã cập nhật sản phẩm."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Đã xoá sản phẩm."));
    }
}
