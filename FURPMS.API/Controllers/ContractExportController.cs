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

    /// <summary>
    /// BM13 — Biên bản nghiệm thu &amp; thanh lý hợp đồng (QĐ543 Điều 13.2). Xuất Word để ký ngoài
    /// rồi tải bản đã ký lên như hợp đồng gốc.
    /// </summary>
    [HttpGet("export-settlement-word")]
    public async Task<IActionResult> ExportSettlementWord(Guid contractId)
    {
        var (content, fileName) = await _export.ExportSettlementDocAsync(contractId);
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }
}
