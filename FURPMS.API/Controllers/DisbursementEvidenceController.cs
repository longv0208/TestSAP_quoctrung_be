using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

// Minh chứng giải ngân (rule tuần 10 — thầy Đức): hệ thống KHÔNG chuyển tiền, chỉ lưu file
// hợp đồng/chứng từ do Staff upload làm minh chứng cho từng đợt giải ngân.
[ApiController]
[Route("api/disbursements/{disbursementId:int}/evidence")]
[Authorize(Roles = "Admin,Staff")]
public class DisbursementEvidenceController : ControllerBase
{
    private readonly IProposalDocumentService _docs;

    public DisbursementEvidenceController(IProposalDocumentService docs) => _docs = docs;

    [HttpGet]
    public async Task<IActionResult> List(int disbursementId)
    {
        var result = await _docs.ListForDisbursementAsync(disbursementId);
        return Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int disbursementId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        var result = await _docs.UploadForDisbursementAsync(
            disbursementId, stream, file.FileName, file.ContentType, file.Length, callerId);
        return Ok(ApiResponse<ProposalDocumentDto>.Ok(result, "Đã lưu minh chứng."));
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(int disbursementId, Guid documentId)
    {
        var (stream, contentType, fileName) = await _docs.DownloadEvidenceAsync(documentId);
        return File(stream, contentType, fileName);
    }
}
