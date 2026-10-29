using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/teachers")]
[Authorize]
public class TeachersController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTeachers()
    {
        var teachers = await db.Teachers.AsNoTracking()
            .OrderBy(item => item.User.FullName)
            .Select(item => new
            {
                id = item.Id,
                userId = item.UserId,
                fullName = item.User.FullName,
                email = item.User.Email,
                department = item.Department
            })
            .ToListAsync();
        return Ok(teachers);
    }

    [HttpGet("me/schedule")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetMySchedule()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var teacher = await db.Teachers.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new { item.Id, item.User.FullName })
            .SingleOrDefaultAsync();
        if (teacher is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ giảng viên." });
        }

        var schedule = await db.ClassSessions.AsNoTracking()
            .Where(item => item.TeacherId == teacher.Id)
            .OrderBy(item => item.StartsAt)
            .Select(item => new
            {
                id = item.Id,
                courseId = item.CourseId,
                courseCode = item.Course.Code,
                courseName = item.Course.Name,
                startsAt = item.StartsAt,
                endsAt = item.EndsAt,
                room = item.Room
            })
            .ToListAsync();
        return Ok(schedule);
    }
}
