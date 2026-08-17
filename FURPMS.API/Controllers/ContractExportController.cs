using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

// BM05 — tự sinh Word hợp đồng (rule tuần 10): bốc dữ liệu điền mẫu → xuất .docx để đem ký ngoài,
// sau đó upload bản đã ký làm minh chứng (dùng /api/contracts/{id}/documents — polymorphic Document).
[ApiController]
[Route("api/contracts/{contractId:guid}")]
// Quyền: Admin/Staff xem mọi hợp đồng; **chủ nhiệm xem hợp đồng CỦA MÌNH**. Trước 17/08 chặn cứng
// theo vai nên PI — người trực tiếp ký bản hợp đồng này — lại không tải nổi bản Word của chính mình,
// phải nhắn chuyên viên gửi hộ.
[Authorize]
public class ContractExportController : ControllerBase
{
    private readonly IDocumentExportService _export;
    private readonly IContractRepository _contracts;

    public ContractExportController(IDocumentExportService export, IContractRepository contracts)
    {
        _export = export;
        _contracts = contracts;
    }

    /// <summary>
    /// Không phải Admin/Staff thì phải đúng là chủ nhiệm của đề tài gắn với hợp đồng.
    /// Kiểm ở đây chứ không dựa vào giao diện: giao diện ẩn nút không ngăn được ai gọi thẳng API.
    /// </summary>
    private async Task EnsureCanReadAsync(Guid contractId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Staff")) return;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isOwner = await _contracts.Query()
            .AnyAsync(c => c.Id == contractId && c.Project != null && c.Project.PiUserId == userId);

        if (!isOwner)
            throw new ForbiddenException("Bạn chỉ xem được hợp đồng của đề tài do mình chủ nhiệm.");
    }

    [HttpGet("export-word")]
    public async Task<IActionResult> ExportWord(Guid contractId)
    {
        await EnsureCanReadAsync(contractId);
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
        await EnsureCanReadAsync(contractId);
        var (content, fileName) = await _export.ExportSettlementDocAsync(contractId);
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }
}
