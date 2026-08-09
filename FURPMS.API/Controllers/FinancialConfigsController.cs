using FURPMS.Application.Common;
using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/financial-configs")]
[Authorize]
public class FinancialConfigsController : ControllerBase
{
    private readonly ISystemFinancialConfigService _service;

    public FinancialConfigsController(ISystemFinancialConfigService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemFinancialConfigResponse>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<SystemFinancialConfigResponse>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SystemFinancialConfigResponse>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<SystemFinancialConfigResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SystemFinancialConfigResponse>>> Create(
        [FromBody] UpsertSystemFinancialConfigRequest request)
    {
        var result = await _service.CreateAsync(request);
        return Ok(ApiResponse<SystemFinancialConfigResponse>.Ok(result, "Financial config created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SystemFinancialConfigResponse>>> Update(
        int id,
        [FromBody] UpsertSystemFinancialConfigRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<SystemFinancialConfigResponse>.Ok(result, "Financial config updated."));
    }

    // Xoá vĩnh viễn chỉ khi không ai tham chiếu; còn dùng thì vô hiệu hoá (xem service).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Đã xoá vĩnh viễn cấu hình tài chính."));
    }
}
