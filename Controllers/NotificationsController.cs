using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifications = await db.Notifications.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                id = item.Id,
                title = item.Title,
                message = item.Message,
                isRead = item.IsRead,
                createdAt = item.CreatedAt
            })
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notification = await db.Notifications
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (notification is null)
        {
            return NotFound(new { message = "Không tìm thấy thông báo." });
        }
        notification.IsRead = true;
        await db.SaveChangesAsync();
        return Ok(new { id = notification.Id, isRead = notification.IsRead });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifications = await db.Notifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync();
        return Ok(new { updatedCount = notifications.Count });
    }
}
