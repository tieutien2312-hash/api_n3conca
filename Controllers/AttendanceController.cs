using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("scan-qr")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ScanQr(ScanQrRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await db.Students.SingleOrDefaultAsync(item => item.UserId == userId);
        if (student is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });
        }

        var now = DateTimeOffset.UtcNow;
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .ThenInclude(item => item.Course)
            .SingleOrDefaultAsync(item =>
                item.QrCode == request.QrCode &&
                item.IsOpen &&
                item.QrExpiresAt > now);
        if (session is null)
        {
            return BadRequest(new { message = "Mã QR không hợp lệ hoặc đã hết hạn." });
        }

        if (!await db.StudentCourses.AnyAsync(item =>
                item.StudentId == student.Id &&
                item.CourseId == session.ClassSession.CourseId))
        {
            return Forbid();
        }
        if (await db.AttendanceRecords.AnyAsync(item =>
                item.AttendanceSessionId == session.Id &&
                item.StudentId == student.Id))
        {
            return Conflict(new { message = "Sinh viên đã điểm danh phiên này." });
        }

        var record = new AttendanceRecord
        {
            AttendanceSessionId = session.Id,
            StudentId = student.Id,
            MarkedAt = now,
            Status = "Present"
        };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();
        return Ok(new
        {
            id = record.Id,
            attendanceSessionId = record.AttendanceSessionId,
            status = record.Status,
            markedAt = record.MarkedAt,
            courseCode = session.ClassSession.Course.Code,
            courseName = session.ClassSession.Course.Name
        });
    }

    [HttpGet("session/{id:int}")]
    public async Task<IActionResult> GetSessionAttendance(int id)
    {
        var session = await db.AttendanceSessions
            .Include(item => item.ClassSession)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy phiên điểm danh." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Student"))
        {
            var studentId = await db.Students
                .Where(item => item.UserId == userId)
                .Select(item => (int?)item.Id)
                .SingleOrDefaultAsync();
            if (studentId is null)
            {
                return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });
            }

            var ownRecord = await db.AttendanceRecords.AsNoTracking()
                .Where(item => item.AttendanceSessionId == id && item.StudentId == studentId)
                .Select(item => new { id = item.Id, status = item.Status, markedAt = item.MarkedAt })
                .SingleOrDefaultAsync();
            return Ok(new { attendanceSessionId = id, attended = ownRecord is not null, record = ownRecord });
        }

        var mayManage = User.IsInRole("Admin") ||
                        await db.Teachers.AnyAsync(item => item.Id == session.ClassSession.TeacherId && item.UserId == userId);
        if (!mayManage)
        {
            return Forbid();
        }

        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(item => item.AttendanceSessionId == id)
            .OrderBy(item => item.Student.User.FullName)
            .Select(item => new
            {
                id = item.Id,
                studentId = item.StudentId,
                studentCode = item.Student.StudentCode,
                fullName = item.Student.User.FullName,
                email = item.Student.User.Email,
                status = item.Status,
                markedAt = item.MarkedAt
            })
            .ToListAsync();
        return Ok(records);
    }
}

public sealed class ScanQrRequest
{
    [Required, StringLength(100)]
    public string QrCode { get; init; } = string.Empty;
}
