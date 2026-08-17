using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/amendments")]
[Authorize]
public class AmendmentsController : ControllerBase
{
    private readonly IAmendmentService _service;
    private readonly IDocumentExportService _export;
    private readonly IContractRepository _contracts;

    public AmendmentsController(
        IAmendmentService service, IDocumentExportService export, IContractRepository contracts)
    {
        _service = service;
        _export = export;
        _contracts = contracts;
    }

    /// <summary>
    /// Admin/Staff xem mọi phụ lục; **chủ nhiệm chỉ xem phụ lục của hợp đồng do mình đứng tên**.
    /// <para>
    /// Phải kiểm ở ĐÂY: <c>DocumentExportService</c> không kiểm quyền sở hữu dòng nào — nó nhận id
    /// là xuất. Nếu chỉ nới <c>[Authorize]</c> cho chủ nhiệm tải phụ lục của mình thì mọi tài khoản
    /// đăng nhập (kể cả người chấm, kể cả chủ nhiệm đề tài khác) đều tải được phụ lục của người lạ.
    /// </para>
    /// </summary>
    private async Task EnsureCanReadAsync(Guid amendmentId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Staff")) return;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isOwner = await _contracts.Amendments
            .AnyAsync(a => a.Id == amendmentId
                        && a.Contract != null
                        && a.Contract.Project != null
                        && a.Contract.Project.PiUserId == userId);

        if (!isOwner)
            throw new ForbiddenException("Bạn chỉ xem được phụ lục của hợp đồng do mình chủ nhiệm.");
    }

    /// <summary>
    /// F4 — xuất **phụ lục hợp đồng** (.docx) cho đề nghị điều chỉnh đã duyệt, đem ký ngoài rồi
    /// upload bản ký làm minh chứng (giống luồng hợp đồng gốc, rule #21). Hợp đồng đã ký không sửa
    /// đè lên bản gốc: mỗi thay đổi phải có văn bản riêng ghi rõ <em>trước → sau</em>.
    /// Trả **409** nếu đề nghị chưa được duyệt — in bản chờ duyệt ra là tạo giấy tờ khống.
    /// </summary>
    [HttpGet("{id:guid}/export-word")]
    [Authorize]
    public async Task<IActionResult> ExportWord(Guid id)
    {
        await EnsureCanReadAsync(id);
        var (content, fileName) = await _export.ExportAmendmentDocAsync(id);
        return File(content, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewAmendmentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ApproveAsync(id, request, userId);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewAmendmentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.RejectAsync(id, request, userId);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }
}
