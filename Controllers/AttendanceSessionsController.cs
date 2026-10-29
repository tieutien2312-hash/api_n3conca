using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/attendance-sessions")]
[Authorize(Roles = "Teacher,Admin")]
public class AttendanceSessionsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly TimeSpan QrLifetime = TimeSpan.FromMinutes(2);

    [HttpPost]
    public async Task<IActionResult> Create(CreateAttendanceSessionRequest request)
    {
        var classSession = await db.ClassSessions
            .Include(item => item.Teacher)
            .SingleOrDefaultAsync(item => item.Id == request.ClassSessionId);
        if (classSession is null)
        {
            return NotFound(new { message = "Không tìm thấy buổi học." });
        }
        if (!await CanManageAsync(classSession.TeacherId))
        {
            return Forbid();
        }
        var session = new AttendanceSession
        {
            ClassSessionId = classSession.Id,
            QrCode = CreateQrCode(),
            QrExpiresAt = DateTimeOffset.UtcNow.Add(QrLifetime)
        };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();
        return Ok(new
        {
            id = session.Id,
            classSessionId = session.ClassSessionId,
            qrCode = session.QrCode,
            qrExpiresAt = session.QrExpiresAt,
            isOpen = session.IsOpen
        });
    }

    [HttpPost("{id:int}/refresh-qr")]
    public async Task<IActionResult> RefreshQr(int id)
    {
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy phiên điểm danh." });
        }
        if (!await CanManageAsync(session.ClassSession.TeacherId))
        {
            return Forbid();
        }
        if (!session.IsOpen)
        {
            return Conflict(new { message = "Phiên điểm danh đã đóng." });
        }

        session.QrCode = CreateQrCode();
        session.QrExpiresAt = DateTimeOffset.UtcNow.Add(QrLifetime);
        await db.SaveChangesAsync();
        return Ok(new { id = session.Id, qrCode = session.QrCode, qrExpiresAt = session.QrExpiresAt });
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id)
    {
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy phiên điểm danh." });
        }
        if (!await CanManageAsync(session.ClassSession.TeacherId))
        {
            return Forbid();
        }
        if (!session.IsOpen)
        {
            return Conflict(new { message = "Phiên điểm danh đã đóng." });
        }

        session.IsOpen = false;
        session.QrCode = null;
        session.QrExpiresAt = null;
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã đóng phiên điểm danh." });
    }

    private async Task<bool> CanManageAsync(int teacherId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.Teachers.AnyAsync(item => item.Id == teacherId && item.UserId == userId);
    }

    private static string CreateQrCode() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

public sealed class CreateAttendanceSessionRequest
{
    [Range(1, int.MaxValue)]
    public int ClassSessionId { get; init; }
}
