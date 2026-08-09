using FURPMS.Application.Common;
using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/personnel-role-types")]
[Authorize]
public class PersonnelRoleTypesController : ControllerBase
{
    private readonly IPersonnelRoleTypeService _service;

    public PersonnelRoleTypesController(IPersonnelRoleTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PersonnelRoleTypeResponse>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PersonnelRoleTypeResponse>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PersonnelRoleTypeResponse>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<PersonnelRoleTypeResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PersonnelRoleTypeResponse>>> Create(
        [FromBody] UpsertPersonnelRoleTypeRequest request)
    {
        var result = await _service.CreateAsync(request);
        return Ok(ApiResponse<PersonnelRoleTypeResponse>.Ok(result, "PersonnelRoleType created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PersonnelRoleTypeResponse>>> Update(
        int id,
        [FromBody] UpsertPersonnelRoleTypeRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<PersonnelRoleTypeResponse>.Ok(result, "PersonnelRoleType updated."));
    }

    // Xoá vĩnh viễn chỉ khi không ai tham chiếu; còn dùng thì vô hiệu hoá (xem service).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Đã xoá vĩnh viễn vai trò nhân sự."));
    }
}
