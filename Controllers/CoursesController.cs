using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom3ConCa.Data;
using Nhom3ConCa.Models;

namespace Nhom3ConCa.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await db.Courses.AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new
            {
                id = item.Id,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                credits = item.Credits,
                teacherId = item.TeacherId,
                teacher = item.Teacher == null ? null : item.Teacher.User.FullName,
                studentCount = item.StudentCourses.Count
            })
            .ToListAsync();
        return Ok(courses);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                id = item.Id,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                credits = item.Credits,
                teacherId = item.TeacherId,
                teacher = item.Teacher == null ? null : item.Teacher.User.FullName,
                studentCount = item.StudentCourses.Count,
                sessions = item.ClassSessions.OrderBy(session => session.StartsAt).Select(session => new
                {
                    id = session.Id,
                    startsAt = session.StartsAt,
                    endsAt = session.EndsAt,
                    room = session.Room
                })
            })
            .SingleOrDefaultAsync();
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost("{id:int}/enroll")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Enroll(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await db.Students.SingleOrDefaultAsync(item => item.UserId == userId);
        if (student is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ sinh viên." });
        }

        if (!await db.Courses.AnyAsync(item => item.Id == id))
        {
            return NotFound(new { message = "Không tìm thấy môn học." });
        }
        if (await db.StudentCourses.AnyAsync(item => item.StudentId == student.Id && item.CourseId == id))
        {
            return Conflict(new { message = "Sinh viên đã đăng ký môn học này." });
        }

        db.StudentCourses.Add(new StudentCourse { StudentId = student.Id, CourseId = id });
        await db.SaveChangesAsync();
        return Ok(new { message = "Đăng ký môn học thành công." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request)
    {
        if (request.TeacherId is not null && !await db.Teachers.AnyAsync(item => item.Id == request.TeacherId))
        {
            return BadRequest(new { message = "Giảng viên không tồn tại." });
        }
        if (await db.Courses.AnyAsync(item => item.Code == request.Code))
        {
            return Conflict(new { message = "Mã môn học đã tồn tại." });
        }

        var course = new Course
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Credits = request.Credits,
            TeacherId = request.TeacherId
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, new { course.Id, course.Code, course.Name });
    }
}

public sealed class CreateCourseRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(30)]
    public string Code { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? Description { get; init; }

    [System.ComponentModel.DataAnnotations.Range(1, 30)]
    public int Credits { get; init; }

    public int? TeacherId { get; init; }
}
