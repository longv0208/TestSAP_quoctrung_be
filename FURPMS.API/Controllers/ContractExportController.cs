using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

// BM05 — tự sinh Word hợp đồng (rule tuần 10): bốc dữ liệu điền mẫu → xuất .docx để đem ký ngoài,
// sau đó upload bản đã ký làm minh chứng (dùng /api/contracts/{id}/documents — polymorphic Document).
[ApiController]
[Route("api/contracts/{contractId:guid}")]
[Authorize(Roles = "Admin,Staff")]
public class ContractExportController : ControllerBase
{
    private readonly IDocumentExportService _export;

    public ContractExportController(IDocumentExportService export) => _export = export;

    [HttpGet("export-word")]
    public async Task<IActionResult> ExportWord(Guid contractId)
    {
        var (content, fileName) = await _export.ExportContractDocAsync(contractId);
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }
}
