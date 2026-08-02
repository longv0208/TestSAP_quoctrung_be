using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/proposals/{proposalId:guid}/documents")]
[Authorize]
public class ProposalDocumentsController : ControllerBase
{
    private readonly IProposalDocumentService _docs;

    public ProposalDocumentsController(IProposalDocumentService docs)
    {
        _docs = docs;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid proposalId)
    {
        var result = await _docs.ListForProposalAsync(proposalId);
        return Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(Guid proposalId, IFormFile file, [FromForm] string? documentType)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        var result = await _docs.UploadAsync(
            proposalId, stream, file.FileName, file.ContentType, file.Length, documentType, callerId);
        return Ok(ApiResponse<ProposalDocumentDto>.Ok(result));
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid proposalId, Guid documentId)
    {
        var (stream, contentType, fileName) = await _docs.DownloadAsync(proposalId, documentId);
        return File(stream, contentType, fileName);
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid proposalId, Guid documentId)
    {
        await _docs.DeleteAsync(proposalId, documentId);
        return Ok(ApiResponse.Ok("Đã xóa tài liệu."));
    }
}
