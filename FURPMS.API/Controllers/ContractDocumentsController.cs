using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

// BM05 — hồ sơ hợp đồng (rule tuần 10): bản Word hệ thống sinh (regen qua export-word) + bản ĐÃ KÝ
// upload lên đây làm minh chứng. Document polymorphic EntityType="Contract".
[ApiController]
[Route("api/contracts/{contractId:guid}/documents")]
[Authorize(Roles = "Admin,Staff")]
public class ContractDocumentsController : ControllerBase
{
    private readonly IProposalDocumentService _docs;

    public ContractDocumentsController(IProposalDocumentService docs) => _docs = docs;

    [HttpGet]
    public async Task<IActionResult> List(Guid contractId)
    {
        var result = await _docs.ListForContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(Guid contractId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        var result = await _docs.UploadForContractAsync(
            contractId, stream, file.FileName, file.ContentType, file.Length, callerId);
        return Ok(ApiResponse<ProposalDocumentDto>.Ok(result, "Đã lưu bản hợp đồng đã ký."));
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid contractId, Guid documentId)
    {
        var (stream, contentType, fileName) = await _docs.DownloadContractDocAsync(documentId);
        return File(stream, contentType, fileName);
    }
}
