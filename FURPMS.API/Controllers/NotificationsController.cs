using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Notifications;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetMyNotificationsAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(result));
    }

    // GET /api/notifications/count
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<NotificationCountDto>.Ok(result));
    }

    // PATCH /api/notifications/{id}/read
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.MarkReadAsync(id, userId);
        return Ok(ApiResponse.Ok("Notification marked as read."));
    }

    // PATCH /api/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.MarkAllReadAsync(userId);
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }
}
