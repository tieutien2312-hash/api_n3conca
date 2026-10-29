using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/class-sessions")]
[Authorize]
public class ClassSessionsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClassSessions(
        [FromQuery] int? courseId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
    {
        if (from is not null && to is not null && from > to)
        {
            return BadRequest(new { message = "'from' phải nhỏ hơn hoặc bằng 'to'." });
        }

        var query = db.ClassSessions.AsNoTracking().AsQueryable();
        if (courseId is not null)
        {
            query = query.Where(item => item.CourseId == courseId);
        }
        if (from is not null)
        {
            query = query.Where(item => item.EndsAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(item => item.StartsAt <= to);
        }

        var sessions = await query.OrderBy(item => item.StartsAt)
            .Select(item => new
            {
                id = item.Id,
                courseId = item.CourseId,
                courseCode = item.Course.Code,
                courseName = item.Course.Name,
                teacherId = item.TeacherId,
                teacher = item.Teacher.User.FullName,
                startsAt = item.StartsAt,
                endsAt = item.EndsAt,
                room = item.Room
            })
            .ToListAsync();
        return Ok(sessions);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> CreateClassSession(CreateClassSessionRequest request)
    {
        if (request.EndsAt <= request.StartsAt)
        {
            return BadRequest(new { message = "Thời điểm kết thúc phải sau thời điểm bắt đầu." });
        }

        var isAdmin = User.IsInRole("Admin");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var teacher = await db.Teachers.SingleOrDefaultAsync(item => item.UserId == userId);
        var teacherId = isAdmin ? request.TeacherId ?? teacher?.Id : teacher?.Id;
        if (teacherId is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ giảng viên." });
        }

        if (!await db.Teachers.AnyAsync(item => item.Id == teacherId))
        {
            return BadRequest(new { message = "Giảng viên không tồn tại." });
        }
        var course = await db.Courses.SingleOrDefaultAsync(item => item.Id == request.CourseId);
        if (course is null)
        {
            return NotFound(new { message = "Không tìm thấy môn học." });
        }
        if (!isAdmin && course.TeacherId != teacherId)
        {
            return Forbid();
        }

        var session = new ClassSession
        {
            CourseId = request.CourseId,
            TeacherId = teacherId.Value,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Room = request.Room.Trim()
        };
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetClassSessions), new { courseId = session.CourseId }, new { session.Id });
    }
}

public sealed class CreateClassSessionRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    public int? TeacherId { get; init; }

    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }

    [Required, StringLength(100)]
    public string Room { get; init; } = string.Empty;
}
