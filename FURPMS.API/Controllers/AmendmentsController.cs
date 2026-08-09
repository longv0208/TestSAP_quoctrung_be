using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Services;
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

    public AmendmentsController(IAmendmentService service, IDocumentExportService export)
    {
        _service = service;
        _export = export;
    }

    /// <summary>
    /// F4 — xuất **phụ lục hợp đồng** (.docx) cho đề nghị điều chỉnh đã duyệt, đem ký ngoài rồi
    /// upload bản ký làm minh chứng (giống luồng hợp đồng gốc, rule #21). Hợp đồng đã ký không sửa
    /// đè lên bản gốc: mỗi thay đổi phải có văn bản riêng ghi rõ <em>trước → sau</em>.
    /// Trả **409** nếu đề nghị chưa được duyệt — in bản chờ duyệt ra là tạo giấy tờ khống.
    /// </summary>
    [HttpGet("{id:guid}/export-word")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ExportWord(Guid id)
    {
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
