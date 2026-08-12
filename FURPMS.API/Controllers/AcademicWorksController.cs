using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Users;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Công trình khoa học &amp; đề tài đã tham gia trong lý lịch khoa học (QĐ543 — Biểu mẫu 02).
/// <para>
/// Thay cho các ô <i>đếm số</i> tự khai trước đây. BM02 đòi <b>cả hai</b>: số lượng (mục
/// 14.1–14.5, 15, 19.1/19.3) <i>và</i> danh sách chi tiết (14.6, 16.3, 17, 19.4) — hệ thống cũ
/// chỉ làm phần số nên hồ sơ thiếu so với biểu mẫu, và hội đồng xét năng lực chủ nhiệm theo
/// <b>Điều 7</b> không tra được nguồn.
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/academic-works")]
public class AcademicWorksController : ControllerBase
{
    private readonly IAcademicWorkService _service;

    public AcademicWorksController(IAcademicWorkService service) => _service = service;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdminOrStaff => User.IsInRole("Admin") || User.IsInRole("Staff");

    /// <summary>Toàn bộ công trình của một người, gom theo mục của biểu mẫu.</summary>
    [HttpGet]
    public async Task<IActionResult> List(Guid userId)
    {
        var works = await _service.ListAsync(userId, CurrentUserId, IsAdminOrStaff);
        return Ok(ApiResponse<List<AcademicWorkResponse>>.Ok(works));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, [FromBody] AcademicWorkRequest request)
    {
        var work = await _service.CreateAsync(userId, CurrentUserId, request);
        return Ok(ApiResponse<AcademicWorkResponse>.Ok(work));
    }

    [HttpPut("{workId:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid workId, [FromBody] AcademicWorkRequest request)
    {
        var work = await _service.UpdateAsync(userId, CurrentUserId, workId, request);
        return Ok(ApiResponse<AcademicWorkResponse>.Ok(work));
    }

    [HttpDelete("{workId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid workId)
    {
        await _service.DeleteAsync(userId, CurrentUserId, workId);
        return Ok(ApiResponse.Ok("Đã xoá công trình khỏi lý lịch khoa học."));
    }
}
