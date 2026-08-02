using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _service;

    public SystemSettingsController(ISystemSettingService service)
    {
        _service = service;
    }

    /// <summary>Danh sách cấu hình vận hành (Admin xem/chỉnh).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingResponse>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<SystemSettingResponse>>.Ok(result));
    }

    /// <summary>Giới hạn upload hiện hành — mọi user đăng nhập đọc được để FE validate trước khi gửi file.</summary>
    [HttpGet("upload-policy")]
    public async Task<ActionResult<ApiResponse<UploadPolicyResponse>>> GetUploadPolicy()
    {
        var result = await _service.GetUploadPolicyAsync();
        return Ok(ApiResponse<UploadPolicyResponse>.Ok(result));
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SystemSettingResponse>>> Update(
        string key, [FromBody] UpdateSystemSettingRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.UpdateAsync(key, request.Value, userId);
        return Ok(ApiResponse<SystemSettingResponse>.Ok(result, "Đã cập nhật cấu hình."));
    }
}
