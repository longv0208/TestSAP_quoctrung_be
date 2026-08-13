using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Users;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _users.GetUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();

        if (!roles.Contains("Admin") && !roles.Contains("Staff") && callerId != id)
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này.");

        var result = await _users.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _users.CreateUserAsync(request, createdBy);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _users.UpdateUserAsync(id, request);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _users.ToggleActiveAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        await _users.ResetPasswordAsync(id);
        return Ok(ApiResponse.Ok("Đã reset mật khẩu về mặc định Furpms@123456."));
    }
}
